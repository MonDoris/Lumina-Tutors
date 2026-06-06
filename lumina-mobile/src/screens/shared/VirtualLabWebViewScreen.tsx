/**
 * VirtualLabWebViewScreen — Full-screen 3D Lab viewer
 * • No native header bar (lab controls its own UI)
 * • Floating exit button always visible top-right
 * • Lab name pill top-left (auto-hides after 4s)
 * • Rendered inside Modal → truly covers AppShell
 */
import React, { useRef, useState, useEffect, useCallback } from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity,
  ActivityIndicator, Platform, StatusBar, Animated,
} from 'react-native';
import { WebView } from 'react-native-webview';
import { BASE_URL, onlineApi } from '../../api/client';

interface Props {
  sessionCode: string;
  labName?: string;
  onClose: () => void;
}

export default function VirtualLabWebViewScreen({ sessionCode, labName, onClose }: Props) {
  const webViewRef  = useRef<WebView>(null);
  const [loading,   setLoading]   = useState(true);
  const [error,     setError]     = useState<string | null>(null);
  const [url,       setUrl]       = useState<string | null>(null);

  const infoOpacity = useRef(new Animated.Value(1)).current;
  const hideTimer   = useRef<ReturnType<typeof setTimeout> | null>(null);

  /* ── Auto-hide info overlay after 4s (fades to 15% so exit btn stays visible) */
  const flashInfo = useCallback(() => {
    if (hideTimer.current) clearTimeout(hideTimer.current);
    Animated.timing(infoOpacity, { toValue: 1, duration: 150, useNativeDriver: true }).start();
    hideTimer.current = setTimeout(() => {
      Animated.timing(infoOpacity, { toValue: 0.15, duration: 600, useNativeDriver: true }).start();
    }, 4000);
  }, [infoOpacity]);

  useEffect(() => {
    flashInfo();
    return () => { if (hideTimer.current) clearTimeout(hideTimer.current); };
  }, [flashInfo]);

  /* ── Get bridge token ───────────────────────────────────────────── */
  useEffect(() => {
    (async () => {
      try {
        const res  = await onlineApi.webviewToken();
        const code = res.data?.code;
        if (!code) { setError('Không lấy được mã xác thực. Thử lại sau.'); return; }
        setUrl(`${BASE_URL}/VirtualLab/MobileEntry?sessionCode=${encodeURIComponent(sessionCode)}&code=${code}`);
      } catch (e: any) {
        setError(`Lỗi kết nối: ${e?.message ?? 'Không xác định'}`);
      }
    })();
  }, [sessionCode]);

  /* ── Error state ────────────────────────────────────────────────── */
  if (error) {
    return (
      <View style={s.screen}>
        <StatusBar barStyle="light-content" backgroundColor="#000" />
        <View style={s.center}>
          <Text style={s.errIcon}>🔬</Text>
          <Text style={s.errText}>{error}</Text>
          <TouchableOpacity style={s.goBackBtn} onPress={onClose}>
            <Text style={s.goBackText}>← Quay lại</Text>
          </TouchableOpacity>
        </View>
        <TouchableOpacity style={s.exitBtnFixed} onPress={onClose} hitSlop={HIT_SLOP}>
          <Text style={s.exitX}>✕</Text>
        </TouchableOpacity>
      </View>
    );
  }

  /* ── Loading URL ────────────────────────────────────────────────── */
  if (!url) {
    return (
      <View style={s.screen}>
        <StatusBar barStyle="light-content" backgroundColor="#000" />
        <View style={s.center}>
          <ActivityIndicator size="large" color="#10b981" />
          <Text style={s.loadHint}>Đang khởi tạo phòng lab…</Text>
          <Text style={s.loadHint2}>Mô hình 3D có thể mất vài giây</Text>
        </View>
        <TouchableOpacity style={s.exitBtnFixed} onPress={onClose} hitSlop={HIT_SLOP}>
          <Text style={s.exitX}>✕</Text>
        </TouchableOpacity>
      </View>
    );
  }

  /* ── Main lab ───────────────────────────────────────────────────── */
  return (
    <View style={s.screen}>
      <StatusBar barStyle="light-content" backgroundColor="#000" hidden={Platform.OS === 'ios'} />

      {/* WebView absolutely fills the screen */}
      <WebView
        ref={webViewRef}
        source={{ uri: url }}
        style={StyleSheet.absoluteFill}
        javaScriptEnabled
        domStorageEnabled
        allowsInlineMediaPlayback
        allowsFullscreenVideo
        onLoadStart={() => setLoading(true)}
        onLoadEnd={() => { setLoading(false); flashInfo(); }}
        onError={(e) => {
          setLoading(false);
          setError(`Không thể tải phòng lab:\n${e.nativeEvent.description}`);
        }}
        injectedJavaScript={INJECTED_JS}
        onMessage={() => {}}
      />

      {/* Loading spinner */}
      {loading && (
        <View style={s.spinnerOverlay} pointerEvents="none">
          <ActivityIndicator size="large" color="#10b981" />
          <Text style={s.spinnerText}>Đang tải phòng lab 3D…</Text>
        </View>
      )}

      {/* ── Floating overlay: box-none → touches pass through to WebView ── */}
      <Animated.View
        style={[StyleSheet.absoluteFill, { opacity: infoOpacity }]}
        pointerEvents="box-none"
      >
        {/* Lab name pill — non-interactive */}
        <View style={s.infoStrip} pointerEvents="none">
          <Text style={s.infoIcon}>🔬</Text>
          <Text style={s.infoText} numberOfLines={1}>
            {labName ?? 'Phòng Lab 3D'}
          </Text>
        </View>

        {/* Exit button — stays tappable */}
        <TouchableOpacity
          style={s.exitBtnFixed}
          onPress={onClose}
          hitSlop={HIT_SLOP}
          activeOpacity={0.75}
        >
          <Text style={s.exitX}>✕</Text>
        </TouchableOpacity>
      </Animated.View>
    </View>
  );
}

/* ── Injected JS ─────────────────────────────────────────────────── */
const INJECTED_JS = `
(function() {
  var meta = document.querySelector('meta[name="viewport"]');
  if (!meta) { meta = document.createElement('meta'); meta.name = 'viewport'; document.head.appendChild(meta); }
  meta.content = 'width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no';
  document.body.style.overscrollBehavior = 'none';
  document.documentElement.classList.add('in-webview');
})();
true;
`;

const HIT_SLOP   = { top: 12, bottom: 12, left: 12, right: 12 };
const TOP_OFFSET = Platform.OS === 'android' ? (StatusBar.currentHeight ?? 0) + 8 : 52;

const s = StyleSheet.create({
  screen: { flex: 1, backgroundColor: '#000' },

  center: {
    flex: 1, alignItems: 'center', justifyContent: 'center', gap: 10, paddingHorizontal: 32,
  },

  /* Info strip ─────────────────────────────────────────────── */
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
    maxWidth: '70%',
  },
  infoIcon: { fontSize: 16 },
  infoText: { color: '#fff', fontSize: 13, fontWeight: '700' },

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

  /* Spinner ─────────────────────────────────────────────────── */
  spinnerOverlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0,0,0,0.78)',
    alignItems: 'center', justifyContent: 'center', gap: 12,
  },
  spinnerText: { color: '#e2e8f0', fontSize: 14, fontWeight: '600' },

  /* Loading text ────────────────────────────────────────────── */
  loadHint:  { color: '#94a3b8', fontSize: 14, marginTop: 8 },
  loadHint2: { color: '#64748b', fontSize: 12 },

  /* Error ───────────────────────────────────────────────────── */
  errIcon: { fontSize: 44 },
  errText: { color: '#f87171', fontSize: 14, textAlign: 'center', lineHeight: 22 },
  goBackBtn: {
    backgroundColor: '#10b981', borderRadius: 10,
    paddingHorizontal: 24, paddingVertical: 11, marginTop: 8,
  },
  goBackText: { color: '#fff', fontWeight: '700', fontSize: 14 },
});
