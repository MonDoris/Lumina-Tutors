import React, { useRef, useEffect } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet,
  Animated, Dimensions, StatusBar,
} from 'react-native';

const { width, height } = Dimensions.get('window');

interface RoleCard {
  roleCode: string;
  label:    string;
  sub:      string;
  icon:     string;
  grad:     [string, string];
}

const ROLES: RoleCard[] = [
  {
    roleCode: 'STUDENT',
    label:    'Học sinh',
    sub:      'Điểm số · Lịch học · Thông báo',
    icon:     '🎓',
    grad:     ['#1d4ed8', '#3b82f6'],
  },
  {
    roleCode: 'PARENT',
    label:    'Phụ huynh',
    sub:      'Theo dõi con · Học phí · Liên hệ',
    icon:     '👨‍👩‍👧',
    grad:     ['#059669', '#34d399'],
  },
  {
    roleCode: 'TEACHER',
    label:    'Giáo viên',
    sub:      'Lớp học · Điểm danh · Bài tập',
    icon:     '📖',
    grad:     ['#b45309', '#f59e0b'],
  },
  {
    roleCode: 'SUPERVISOR',
    label:    'Giám thị',
    sub:      'Kỷ luật · Báo cáo · Giám sát',
    icon:     '🛡️',
    grad:     ['#6d28d9', '#a78bfa'],
  },
];

interface Props {
  onSelect: (roleCode: string) => void;
}

export default function RoleSelectScreen({ onSelect }: Props) {
  const headerY   = useRef(new Animated.Value(-30)).current;
  const headerOp  = useRef(new Animated.Value(0)).current;
  const cardAnims = useRef(ROLES.map(() => ({
    y:  new Animated.Value(40),
    op: new Animated.Value(0),
  }))).current;
  const scales    = useRef(ROLES.map(() => new Animated.Value(1))).current;

  useEffect(() => {
    Animated.parallel([
      Animated.timing(headerY,  { toValue: 0, duration: 600, useNativeDriver: true }),
      Animated.timing(headerOp, { toValue: 1, duration: 600, useNativeDriver: true }),
      Animated.stagger(90, cardAnims.map(a =>
        Animated.parallel([
          Animated.timing(a.y,  { toValue: 0, duration: 500, useNativeDriver: true }),
          Animated.timing(a.op, { toValue: 1, duration: 500, useNativeDriver: true }),
        ])
      )),
    ]).start();
  }, []);

  const pressIn  = (i: number) =>
    Animated.spring(scales[i], { toValue: 0.95, useNativeDriver: true, damping: 15 }).start();
  const pressOut = (i: number) =>
    Animated.spring(scales[i], { toValue: 1, useNativeDriver: true, damping: 12 }).start();

  return (
    <View style={s.root}>
      <StatusBar barStyle="light-content" backgroundColor="transparent" translucent />

      {/* ── Decorative circles ─────────────────────────── */}
      <View style={s.circle1} />
      <View style={s.circle2} />
      <View style={s.circle3} />

      {/* ── Header ─────────────────────────────────────── */}
      <Animated.View style={[s.header, { opacity: headerOp, transform: [{ translateY: headerY }] }]}>
        <View style={s.logoOuter}>
          <View style={s.logoInner}>
            <Text style={s.logoStar}>✦</Text>
          </View>
        </View>
        <Text style={s.appName}>Lumina Tutors</Text>
        <Text style={s.appSub}>Hệ thống quản lý giáo dục</Text>
        <View style={s.divider} />
        <Text style={s.prompt}>Chọn vai trò của bạn</Text>
      </Animated.View>

      {/* ── Role list ──────────────────────────────────── */}
      <View style={s.list}>
        {ROLES.map((role, i) => (
          <Animated.View
            key={role.roleCode}
            style={{
              opacity:   cardAnims[i].op,
              transform: [
                { translateY: cardAnims[i].y },
                { scale: scales[i] },
              ],
            }}
          >
            <TouchableOpacity
              activeOpacity={1}
              onPressIn={() => pressIn(i)}
              onPressOut={() => pressOut(i)}
              onPress={() => onSelect(role.roleCode)}
              style={s.card}
            >
              {/* Left accent line */}
              <View style={[s.cardAccent, { backgroundColor: role.grad[0] }]} />

              {/* Icon bubble */}
              <View style={[s.iconBubble, { backgroundColor: role.grad[0] + '18' }]}>
                <Text style={s.iconText}>{role.icon}</Text>
              </View>

              {/* Text */}
              <View style={s.cardText}>
                <Text style={s.cardLabel}>{role.label}</Text>
                <Text style={s.cardSub}>{role.sub}</Text>
              </View>

              {/* Arrow */}
              <View style={[s.arrow, { backgroundColor: role.grad[0] }]}>
                <Text style={s.arrowText}>›</Text>
              </View>
            </TouchableOpacity>
          </Animated.View>
        ))}
      </View>

      {/* ── Footer ─────────────────────────────────────── */}
      <Animated.View style={[s.footer, { opacity: headerOp }]}>
        <Text style={s.footerText}>© 2025 Lumina Education · Phiên bản 1.0</Text>
      </Animated.View>
    </View>
  );
}

const s = StyleSheet.create({
  root: {
    flex: 1,
    backgroundColor: '#0b1628',
    paddingHorizontal: 20,
    paddingTop: 0,
  },

  /* ── Decorative ── */
  circle1: {
    position: 'absolute', width: 320, height: 320, borderRadius: 160,
    backgroundColor: '#c9a84c12',
    top: -80, right: -100,
  },
  circle2: {
    position: 'absolute', width: 200, height: 200, borderRadius: 100,
    backgroundColor: '#ffffff06',
    top: 120, left: -60,
  },
  circle3: {
    position: 'absolute', width: 150, height: 150, borderRadius: 75,
    backgroundColor: '#c9a84c08',
    bottom: 60, right: -30,
  },

  /* ── Header ── */
  header: {
    alignItems: 'center',
    paddingTop: 64,
    paddingBottom: 28,
  },
  logoOuter: {
    width: 80, height: 80, borderRadius: 24,
    backgroundColor: '#c9a84c22',
    alignItems: 'center', justifyContent: 'center',
    marginBottom: 16,
    borderWidth: 1, borderColor: '#c9a84c40',
  },
  logoInner: {
    width: 60, height: 60, borderRadius: 18,
    backgroundColor: '#c9a84c',
    alignItems: 'center', justifyContent: 'center',
    shadowColor: '#c9a84c', shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.6, shadowRadius: 20, elevation: 16,
  },
  logoStar:  { fontSize: 28, color: '#0b1628' },
  appName:   { fontSize: 26, fontWeight: '800', color: '#faf8f4', letterSpacing: 0.3, marginBottom: 4 },
  appSub:    { fontSize: 12, color: 'rgba(250,248,244,0.4)', letterSpacing: 0.8, textTransform: 'uppercase' },
  divider: {
    width: 36, height: 2, borderRadius: 1,
    backgroundColor: '#c9a84c50',
    marginVertical: 18,
  },
  prompt:    { fontSize: 17, fontWeight: '700', color: 'rgba(250,248,244,0.85)', letterSpacing: 0.2 },

  /* ── Cards ── */
  list: { gap: 12 },

  card: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#ffffff0d',
    borderRadius: 18,
    paddingVertical: 18,
    paddingRight: 16,
    paddingLeft: 0,
    borderWidth: 1,
    borderColor: '#ffffff12',
    overflow: 'hidden',
    gap: 14,
  },
  cardAccent: {
    width: 4, alignSelf: 'stretch',
    borderTopRightRadius: 2, borderBottomRightRadius: 2,
    marginLeft: 0,
  },
  iconBubble: {
    width: 52, height: 52, borderRadius: 16,
    alignItems: 'center', justifyContent: 'center',
    flexShrink: 0,
  },
  iconText:  { fontSize: 26 },
  cardText:  { flex: 1 },
  cardLabel: {
    fontSize: 16, fontWeight: '700',
    color: '#faf8f4', marginBottom: 3, letterSpacing: 0.1,
  },
  cardSub:   {
    fontSize: 12, color: 'rgba(250,248,244,0.45)',
    letterSpacing: 0.2,
  },
  arrow: {
    width: 34, height: 34, borderRadius: 10,
    alignItems: 'center', justifyContent: 'center',
    flexShrink: 0,
  },
  arrowText: { fontSize: 20, color: '#fff', fontWeight: '700', lineHeight: 22 },

  /* ── Footer ── */
  footer:    { paddingVertical: 20, alignItems: 'center' },
  footerText:{ fontSize: 11, color: 'rgba(250,248,244,0.2)', letterSpacing: 0.4 },
});
