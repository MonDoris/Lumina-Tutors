import React from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity,
  ActivityIndicator, ScrollView, RefreshControl,
} from 'react-native';

/* ── Score color helper ─────────────────────────────────────── */
export function scoreColor(s: number | null): string {
  if (s == null) return '#94a3b8';
  if (s >= 8)   return '#16a34a';
  if (s >= 5)   return '#d97706';
  return '#dc2626';
}
export function scoreBg(s: number | null): string {
  if (s == null) return '#f1f5f9';
  if (s >= 8)   return '#f0fdf4';
  if (s >= 5)   return '#fffbeb';
  return '#fef2f2';
}

/* ── Loading / Empty ────────────────────────────────────────── */
export function LoadingView() {
  return (
    <View style={u.center}>
      <ActivityIndicator size="large" color="#c9a84c" />
    </View>
  );
}

export function EmptyView({ icon = '📭', text = 'Không có dữ liệu' }: { icon?: string; text?: string }) {
  return (
    <View style={u.empty}>
      <Text style={u.emptyIcon}>{icon}</Text>
      <Text style={u.emptyText}>{text}</Text>
    </View>
  );
}

/* ── Section title ──────────────────────────────────────────── */
export function SectionTitle({ children }: { children: React.ReactNode }) {
  return <Text style={u.sectionTitle}>{children}</Text>;
}

/* ── Stat card (hero number) ────────────────────────────────── */
export function StatCard({
  label, value, sub, accent = '#0b1628',
}: { label: string; value: string | number; sub?: string; accent?: string }) {
  return (
    <View style={[u.statCard, { borderTopColor: accent }]}>
      <Text style={u.statLabel}>{label}</Text>
      <Text style={[u.statValue, { color: accent }]}>{value}</Text>
      {sub ? <Text style={u.statSub}>{sub}</Text> : null}
    </View>
  );
}

/* ── Info row ───────────────────────────────────────────────── */
export function InfoRow({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <View style={u.infoRow}>
      <Text style={u.infoLabel}>{label}</Text>
      <Text style={u.infoValue}>{value ?? '—'}</Text>
    </View>
  );
}

/* ── Card wrapper ───────────────────────────────────────────── */
export function Card({ children, style }: { children: React.ReactNode; style?: object }) {
  return <View style={[u.card, style]}>{children}</View>;
}

/* ── Score pill ─────────────────────────────────────────────── */
export function ScorePill({ score }: { score: number | null }) {
  return (
    <View style={[u.pill, { backgroundColor: scoreBg(score) }]}>
      <Text style={[u.pillText, { color: scoreColor(score) }]}>
        {score != null ? score.toFixed(1) : '—'}
      </Text>
    </View>
  );
}

/* ── Badge ──────────────────────────────────────────────────── */
export function Badge({
  text, color = '#64748b', bg = '#f1f5f9',
}: { text: string; color?: string; bg?: string }) {
  return (
    <View style={[u.badge, { backgroundColor: bg }]}>
      <Text style={[u.badgeText, { color }]}>{text}</Text>
    </View>
  );
}

/* ── Tab page wrapper ───────────────────────────────────────── */
export function TabPage({
  children, refreshing, onRefresh,
}: { children: React.ReactNode; refreshing?: boolean; onRefresh?: () => void }) {
  return (
    <ScrollView
      style={u.page}
      contentContainerStyle={u.pageContent}
      showsVerticalScrollIndicator={false}
      refreshControl={
        onRefresh
          ? <RefreshControl refreshing={refreshing ?? false} onRefresh={onRefresh} colors={['#c9a84c']} tintColor="#c9a84c" />
          : undefined
      }
    >
      {children}
      <View style={{ height: 24 }} />
    </ScrollView>
  );
}

/* ── Quick action button ────────────────────────────────────── */
export function QuickBtn({
  icon, label, color, onPress,
}: { icon: string; label: string; color: string; onPress: () => void }) {
  return (
    <TouchableOpacity style={[u.quickBtn, { backgroundColor: color + '18', borderColor: color + '30' }]} onPress={onPress} activeOpacity={0.75}>
      <Text style={u.quickIcon}>{icon}</Text>
      <Text style={[u.quickLabel, { color }]}>{label}</Text>
    </TouchableOpacity>
  );
}

/* ── Notification item ──────────────────────────────────────── */
export function NotifItem({ item }: { item: any }) {
  return (
    <View style={[u.notifItem, !item.isRead && u.notifUnread]}>
      <View style={u.notifDot}>
        {!item.isRead && <View style={u.dot} />}
      </View>
      <View style={{ flex: 1 }}>
        <Text style={u.notifTitle} numberOfLines={1}>{item.title}</Text>
        <Text style={u.notifBody} numberOfLines={2}>{item.body ?? item.content}</Text>
        <Text style={u.notifTime}>{formatTime(item.createdAt)}</Text>
      </View>
    </View>
  );
}

function formatTime(iso?: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' });
}

const u = StyleSheet.create({
  center:       { flex: 1, alignItems: 'center', justifyContent: 'center' },
  page:         { flex: 1, backgroundColor: '#f0f4f8' },
  pageContent:  { padding: 16 },

  empty:        { alignItems: 'center', paddingVertical: 48 },
  emptyIcon:    { fontSize: 40, marginBottom: 10 },
  emptyText:    { fontSize: 14, color: '#94a3b8', fontWeight: '500' },

  sectionTitle: {
    fontSize: 11, fontWeight: '800', color: '#64748b',
    letterSpacing: 0.8, textTransform: 'uppercase',
    marginBottom: 10, marginTop: 6,
  },

  statCard: {
    flex: 1,
    backgroundColor: '#fff',
    borderRadius: 14,
    padding: 16,
    borderTopWidth: 3,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.06, shadowRadius: 8, elevation: 3,
  },
  statLabel: { fontSize: 11, color: '#94a3b8', fontWeight: '600', marginBottom: 6, textTransform: 'uppercase', letterSpacing: 0.5 },
  statValue: { fontSize: 30, fontWeight: '800', lineHeight: 36 },
  statSub:   { fontSize: 11, color: '#94a3b8', marginTop: 4 },

  infoRow:   { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 10, borderBottomWidth: 1, borderBottomColor: '#f1f5f9' },
  infoLabel: { fontSize: 13, color: '#94a3b8', fontWeight: '500' },
  infoValue: { fontSize: 13, color: '#1e293b', fontWeight: '600' },

  card: {
    backgroundColor: '#fff', borderRadius: 16,
    padding: 16, marginBottom: 12,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.06, shadowRadius: 8, elevation: 3,
  },

  pill:     { borderRadius: 20, paddingHorizontal: 12, paddingVertical: 5, minWidth: 48, alignItems: 'center' },
  pillText: { fontSize: 13, fontWeight: '800' },

  badge:     { borderRadius: 6, paddingHorizontal: 8, paddingVertical: 3 },
  badgeText: { fontSize: 11, fontWeight: '700' },

  quickBtn: {
    flex: 1, alignItems: 'center', paddingVertical: 14,
    borderRadius: 14, borderWidth: 1, gap: 6,
  },
  quickIcon:  { fontSize: 24 },
  quickLabel: { fontSize: 11, fontWeight: '700', textAlign: 'center' },

  notifItem: {
    flexDirection: 'row', alignItems: 'flex-start',
    backgroundColor: '#fff', borderRadius: 14,
    padding: 14, marginBottom: 10, gap: 10,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.04, shadowRadius: 4, elevation: 2,
  },
  notifUnread: { borderLeftWidth: 3, borderLeftColor: '#c9a84c' },
  notifDot:    { width: 20, paddingTop: 3, alignItems: 'center' },
  dot:         { width: 8, height: 8, borderRadius: 4, backgroundColor: '#c9a84c' },
  notifTitle:  { fontSize: 13, fontWeight: '700', color: '#1e293b', marginBottom: 3 },
  notifBody:   { fontSize: 12, color: '#64748b', lineHeight: 17 },
  notifTime:   { fontSize: 10, color: '#94a3b8', marginTop: 5 },
});
