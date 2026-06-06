/**
 * ClassroomWebViewScreen — Zoom/Meet style full-screen classroom
 * • No native header bar (web page controls its own UI)
 * • Floating exit button always visible top-right
 * • Room code pill top-left (auto-hides after 4s, tap to show)
 * • Rendered inside Modal → truly covers AppShell header + tab bar
 */
import React, { useRef, useState, useEffect, useCallback } from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity,
  ActivityIndicator, Platform, StatusBar, Animated,
} from 'react-native';
import { WebView } from 'react-native-webview';
import { BASE_URL, onlineApi } from '../../api/client';

interface Props {
  roomCode: string;
  onClose: () => void;
}

export default function ClassroomWebViewScreen({ roomCode, onClose }: Props) {
  const webViewRef = useRef<WebView>(null);
  const [loading,  setLoading]  = useState(true);
  const [error,    setError]    = useState<string | null>(null);
  const [url,      setUrl]      = useState<string | null>(null);
  const [infoVisible, setInfoVisible] = useState(true);

  const infoOpacity = useRef(new Animated.Value(1)).current;
  const hideTimer   = useRef<ReturnType<typeof setTimeout> | null>(null);

  /* ── Auto-hide info overlay after 4s ───────────────────────────── */
  const flashInfo = useCallback(() => {
    if (hideTimer.current) clearTimeout(hideTimer.current);
    Animated.timing(infoOpacity, { toValue: 1, duration: 150, useNativeDriver: true }).start();
    setInfoVisible(true);
    hideTimer.current = setTimeout(() => {
      Animated.timing(infoOpacity, { toValue: 0, duration: 500, useNativeDriver: true })
        .start(() => setInfoVisible(false));
    }, 4000);
  }, [infoOpacity]);

  useEffect(() => {
    flashInfo();
    return () => { if (hideTimer.current) clearTimeout(hideTimer.current); };
  }, [flashInfo]);

  /* ── Get bridge token + build entry URL ─────────────────────────── */
  useEffect(() => {
    (async () => {
      try {
        const res  = await onlineApi.webviewToken();
        const code = res.data?.code;
        if (!code) { setError('Không lấy được mã xác thực. Thử lại sau.'); return; }
        setUrl(`${BASE_URL}/OnlineClassroom/MobileEntry?roomCode=${encodeURIComponent(roomCode)}&code=${code}`);
      } catch (e: any) {
        setError(`Lỗi kết nối: ${e?.message ?? 'Không xác định'}`);
      }
    })();
  }, [roomCode]);

  /* ── Error state ────────────────────────────────────────────────── */
  if (error) {
    return (
      <View style={s.screen}>
        <StatusBar barStyle="light-content" backgroundColor="#000" />
        <View style={s.center}>
          <Text style={s.errIcon}>⚠️</Text>
          <Text style={s.errText}>{error}</Text>
          <TouchableOpacity style={s.goBackBtn} onPress={onClose}>
            <Text style={s.goBackText}>← Quay lại</Text>
          </TouchableOpacity>
        </View>
        {/* Exit always reachable */}
        <TouchableOpacity style={s.exitBtnFixed} onPress={onClose} hitSlop={HIT_SLOP}>
          <Text style={s.exitX}>✕</Text>
        </TouchableOpacity>
      </View>
    );
  }

  /* ── Loading URL state ──────────────────────────────────────────── */
  if (!url) {
    return (
      <View style={s.screen}>
        <StatusBar barStyle="light-content" backgroundColor="#000" />
        <View style={s.center}>
          <ActivityIndicator size="large" color="#c9a84c" />
          <Text style={s.loadHint}>Đang kết nối phòng học…</Text>
        </View>
        <TouchableOpacity style={s.exitBtnFixed} onPress={onClose} hitSlop={HIT_SLOP}>
          <Text style={s.exitX}>✕</Text>
        </TouchableOpacity>
      </View>
    );
  }

  /* ── Main classroom ─────────────────────────────────────────────── */
  return (
    <View style={s.screen}>
      <StatusBar barStyle="light-content" backgroundColor="#000" hidden={Platform.OS === 'ios'} />

      {/* WebView — absolutely fills the screen */}
      <WebView
        ref={webViewRef}
        source={{ uri: url }}
        style={StyleSheet.absoluteFill}
        javaScriptEnabled
        domStorageEnabled
        allowsInlineMediaPlayback
        mediaPlaybackRequiresUserAction={false}
        mediaCapturePermissionGrantType="grant"
        allowsFullscreenVideo
        onLoadStart={() => setLoading(true)}
        onLoadEnd={() => { setLoading(false); flashInfo(); }}
        onError={(e) => {
          setLoading(false);
          setError(`Không thể tải phòng học:\n${e.nativeEvent.description}`);
        }}
        onHttpError={(e) => {
          if (e.nativeEvent.statusCode >= 500)
            setError(`Lỗi máy chủ (${e.nativeEvent.statusCode}). Thử lại sau.`);
        }}
        injectedJavaScript={INJECTED_JS}
        onMessage={() => {}}
      />

      {/* Initial loading spinner over WebView */}
      {loading && (
        <View style={s.spinnerOverlay} pointerEvents="none">
          <ActivityIndicator size="large" color="#c9a84c" />
          <Text style={s.spinnerText}>Đang vào phòng học…</Text>
        </View>
      )}

      {/* ── Floating info strip (top-left, auto-hides) ── */}
      <Animated.View
        style={[s.infoStrip, { opacity: infoOpacity }]}
        pointerEvents={infoVisible ? 'none' : 'none'}
      >
        <View style={s.liveDot} />
        <Text style={s.infoText}>Phòng {roomCode}</Text>
      </Animated.View>

      {/* ── Always-visible exit button (top-right) ── */}
      <TouchableOpacity style={s.exitBtnFixed} onPress={onClose} hitSlop={HIT_SLOP} activeOpacity={0.75}>
        <Text style={s.exitX}>✕</Text>
      </TouchableOpacity>

      {/* Tap zone — tapping shows info strip again */}
      <TouchableOpacity
        style={s.tapZone}
        activeOpacity={1}
        onPress={flashInfo}
      />
    </View>
  );
}

/* ── Injected JS: mobile-friendly viewport + notify touch ───────────── */
const INJECTED_JS = `
(function() {
  var meta = document.querySelector('meta[name="viewport"]');
  if (!meta) { meta = document.createElement('meta'); meta.name = 'viewport'; document.head.appendChild(meta); }
  meta.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';
  // Mark as mobile WebView so Room.cshtml can apply mobile CSS
  document.documentElement.classList.add('in-webview');
})();
true;
`;

const HIT_SLOP = { top: 12, bottom: 12, left: 12, right: 12 };
const TOP_OFFSET = Platform.OS === 'android' ? (StatusBar.currentHeight ?? 0) + 8 : 52;

const s = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#000' },

  center: {
    flex: 1, alignItems: 'center', justifyContent: 'center', gap: 14, paddingHorizontal: 32,
  },

  /* Floating info strip ─────────────────────────────────────── */
  infoStrip: {
    position: 'absolute',
    top: TOP_OFFSET,
    left: 14,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
    backgroundColor: 'rgba(0,0,0,0.55)',
    borderRadius: 20,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  liveDot: {
    width: 8, height: 8, borderRadius: 4,
    backgroundColor: '#ef4444',
  },
  infoText: { color: '#fff', fontSize: 13, fontWeight: '700', letterSpacing: 0.5 },

  /* Exit button ─────────────────────────────────────────────── */
  exitBtnFixed: {
    position: 'absolute',
    top: TOP_OFFSET,
    right: 14,
    width: 36, height: 36, borderRadius: 18,
    backgroundColor: 'rgba(0,0,0,0.55)',
    alignItems: 'center', justifyContent: 'center',
  },
  exitX: { color: '#fff', fontSize: 16, fontWeight: '700' },

  /* Tap zone — covers center of screen, won't block edge buttons */
  tapZone: {
    position: 'absolute',
    top: TOP_OFFSET + 50,
    left: 60,
    right: 60,
    bottom: 100,
  },

  /* Loading / spinner ───────────────────────────────────────── */
  spinnerOverlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0,0,0,0.75)',
    alignItems: 'center', justifyContent: 'center', gap: 12,
  },
  spinnerText: { color: '#94a3b8', fontSize: 14 },

  loadHint: { color: '#94a3b8', fontSize: 14, marginTop: 12 },

  /* Error ───────────────────────────────────────────────────── */
  errIcon: { fontSize: 44 },
  errText: {
    color: '#f87171', fontSize: 14, textAlign: 'center', lineHeight: 22,
  },
  goBackBtn: {
    backgroundColor: '#c9a84c', borderRadius: 10,
    paddingHorizontal: 24, paddingVertical: 11, marginTop: 8,
  },
  goBackText: { color: '#fff', fontWeight: '700', fontSize: 14 },
});
