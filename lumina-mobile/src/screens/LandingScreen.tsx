import React, { useRef, useEffect } from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Animated, StatusBar } from 'react-native';

interface Props {
  onStart: () => void;
}

const FEATURES = [
  { icon: '🎓', title: 'Học tập mọi lúc',           sub: 'Điểm số, lịch học, bài tập và thông báo trong tầm tay.' },
  { icon: '🧪', title: 'Phòng học 3D & Gia Sư AI',  sub: 'Thí nghiệm tương tác và trợ giảng thông minh 24/7.' },
  { icon: '📊', title: 'Theo dõi toàn diện',        sub: 'Điểm danh, học phí, kết nối nhà trường và gia đình.' },
];

export default function LandingScreen({ onStart }: Props) {
  const fade      = useRef(new Animated.Value(0)).current;
  const lift      = useRef(new Animated.Value(24)).current;
  const featAnims = useRef(FEATURES.map(() => new Animated.Value(0))).current;
  const btnScale  = useRef(new Animated.Value(1)).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(fade, { toValue: 1, duration: 600, useNativeDriver: true }),
      Animated.timing(lift, { toValue: 0, duration: 600, useNativeDriver: true }),
      Animated.stagger(110, featAnims.map(a =>
        Animated.timing(a, { toValue: 1, duration: 500, useNativeDriver: true })
      )),
    ]).start();
  }, []);

  return (
    <View style={s.root}>
      <StatusBar barStyle="light-content" backgroundColor="transparent" translucent />

      {/* Decorative circles */}
      <View style={s.circle1} />
      <View style={s.circle2} />
      <View style={s.circle3} />

      {/* Hero */}
      <Animated.View style={[s.hero, { opacity: fade, transform: [{ translateY: lift }] }]}>
        <View style={s.logoOuter}>
          <View style={s.logoInner}><Text style={s.logoStar}>✦</Text></View>
        </View>
        <Text style={s.appName}>Lumina Tutors</Text>
        <Text style={s.appSub}>Hệ thống quản lý giáo dục</Text>
        <Text style={s.headline}>Trường học thông minh{'\n'}trong lòng bàn tay</Text>
        <Text style={s.lead}>Kết nối học sinh, giáo viên và phụ huynh trên một nền tảng duy nhất.</Text>
      </Animated.View>

      {/* Features */}
      <View style={s.features}>
        {FEATURES.map((f, i) => (
          <Animated.View
            key={i}
            style={[
              s.feat,
              {
                opacity: featAnims[i],
                transform: [{ translateY: featAnims[i].interpolate({ inputRange: [0, 1], outputRange: [20, 0] }) }],
              },
            ]}
          >
            <View style={s.featIc}><Text style={s.featIcTxt}>{f.icon}</Text></View>
            <View style={s.featText}>
              <Text style={s.featTitle}>{f.title}</Text>
              <Text style={s.featSub}>{f.sub}</Text>
            </View>
          </Animated.View>
        ))}
      </View>

      {/* CTA */}
      <Animated.View style={{ opacity: fade, transform: [{ scale: btnScale }] }}>
        <TouchableOpacity
          activeOpacity={0.9}
          onPressIn={() => Animated.spring(btnScale, { toValue: 0.96, useNativeDriver: true, damping: 15 }).start()}
          onPressOut={() => Animated.spring(btnScale, { toValue: 1, useNativeDriver: true, damping: 12 }).start()}
          onPress={onStart}
          style={s.cta}
        >
          <Text style={s.ctaText}>Bắt đầu</Text>
          <Text style={s.ctaArrow}>→</Text>
        </TouchableOpacity>
      </Animated.View>

      <Animated.Text style={[s.footer, { opacity: fade }]}>© 2025 Lumina Education · Phiên bản 1.0</Animated.Text>
    </View>
  );
}

const s = StyleSheet.create({
  root: { flex: 1, backgroundColor: '#0b1628', paddingHorizontal: 24, paddingTop: 72, paddingBottom: 30 },

  circle1: { position: 'absolute', width: 320, height: 320, borderRadius: 160, backgroundColor: '#c9a84c12', top: -90, right: -110 },
  circle2: { position: 'absolute', width: 220, height: 220, borderRadius: 110, backgroundColor: '#ffffff06', top: 160, left: -80 },
  circle3: { position: 'absolute', width: 160, height: 160, borderRadius: 80, backgroundColor: '#c9a84c08', bottom: 80, right: -40 },

  hero: { alignItems: 'center' },
  logoOuter: {
    width: 84, height: 84, borderRadius: 26, backgroundColor: '#c9a84c22',
    alignItems: 'center', justifyContent: 'center', marginBottom: 16,
    borderWidth: 1, borderColor: '#c9a84c40',
  },
  logoInner: {
    width: 62, height: 62, borderRadius: 19, backgroundColor: '#c9a84c',
    alignItems: 'center', justifyContent: 'center',
    shadowColor: '#c9a84c', shadowOffset: { width: 0, height: 8 }, shadowOpacity: 0.6, shadowRadius: 20, elevation: 16,
  },
  logoStar: { fontSize: 30, color: '#0b1628' },
  appName:  { fontSize: 26, fontWeight: '800', color: '#faf8f4', letterSpacing: 0.3, marginBottom: 4 },
  appSub:   { fontSize: 12, color: 'rgba(250,248,244,0.4)', letterSpacing: 0.8, textTransform: 'uppercase', marginBottom: 24 },
  headline: { fontSize: 24, fontWeight: '800', color: '#faf8f4', textAlign: 'center', lineHeight: 32, marginBottom: 10 },
  lead:     { fontSize: 14, color: 'rgba(250,248,244,0.55)', textAlign: 'center', lineHeight: 21, paddingHorizontal: 8 },

  features: { flex: 1, justifyContent: 'center', gap: 14, marginTop: 8 },
  feat: {
    flexDirection: 'row', alignItems: 'center', gap: 14,
    backgroundColor: '#ffffff0d', borderWidth: 1, borderColor: '#ffffff12', borderRadius: 16, padding: 14,
  },
  featIc:    { width: 46, height: 46, borderRadius: 14, backgroundColor: '#c9a84c1f', alignItems: 'center', justifyContent: 'center' },
  featIcTxt: { fontSize: 22 },
  featText:  { flex: 1 },
  featTitle: { fontSize: 15, fontWeight: '700', color: '#faf8f4', marginBottom: 2 },
  featSub:   { fontSize: 12, color: 'rgba(250,248,244,0.5)', lineHeight: 17 },

  cta: {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 10,
    backgroundColor: '#c9a84c', borderRadius: 16, paddingVertical: 16,
    shadowColor: '#c9a84c', shadowOffset: { width: 0, height: 8 }, shadowOpacity: 0.4, shadowRadius: 18, elevation: 12,
  },
  ctaText:  { fontSize: 16, fontWeight: '800', color: '#0b1628', letterSpacing: 0.3 },
  ctaArrow: { fontSize: 18, fontWeight: '800', color: '#0b1628' },

  footer: { textAlign: 'center', fontSize: 11, color: 'rgba(250,248,244,0.22)', letterSpacing: 0.4, marginTop: 18 },
});
