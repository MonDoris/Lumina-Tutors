import React, { useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
  ActivityIndicator, RefreshControl,
} from 'react-native';
import { useAuth } from '../../context/AuthContext';
import { supervisorApi } from '../../api/client';
import { colors, spacing, radius, typography } from '../../theme';

export default function SupervisorHomeScreen({ navigation }: any) {
  const { user, logout } = useAuth();
  const [report,    setReport]    = useState<any>(null);
  const [records,   setRecords]   = useState<any[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [refreshing,setRefreshing]= useState(false);

  const loadData = async () => {
    try {
      const [reportRes, recordsRes] = await Promise.all([
        supervisorApi.dailyReport(),
        supervisorApi.discipline(),
      ]);
      setReport(reportRes.data);
      setRecords(recordsRes.data?.items ?? []);
    } catch {}
    finally { setLoading(false); setRefreshing(false); }
  };

  useEffect(() => { loadData(); }, []);
  const onRefresh = () => { setRefreshing(true); loadData(); };

  if (loading) return <View style={s.center}><ActivityIndicator color={colors.navy} size="large" /></View>;

  const today = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: 'numeric', month: 'long' });

  return (
    <ScrollView style={s.page} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}>

      <View style={s.header}>
        <View>
          <Text style={s.greeting}>Giám thị {user?.fullName.split(' ').pop()} 👋</Text>
          <Text style={s.sub}>{today}</Text>
        </View>
        <TouchableOpacity style={s.avatar} onPress={logout}>
          <Text style={s.avatarText}>{user?.fullName[0]}</Text>
        </TouchableOpacity>
      </View>

      {/* Daily stats */}
      {report && (
        <View style={s.statsRow}>
          <StatBox icon="⚠️" label="Vi phạm hôm nay" value={report.totalViolations ?? 0} color="#fef3c7" />
          <StatBox icon="📋" label="Báo cáo mới" value={report.newReports ?? 0} color="#ede9fe" />
        </View>
      )}

      <Text style={s.sectionTitle}>Vi phạm gần đây</Text>

      {records.length === 0 ? (
        <View style={s.empty}>
          <Text style={s.emptyIcon}>✅</Text>
          <Text style={s.emptyText}>Không có vi phạm nào</Text>
        </View>
      ) : (
        records.map((r: any, i: number) => (
          <View key={i} style={s.recordCard}>
            <View style={s.recordHeader}>
              <Text style={s.studentName}>{r.studentName}</Text>
              <View style={[s.badge, { backgroundColor: r.severity === 'High' ? 'rgba(220,38,38,0.1)' : 'rgba(217,119,6,0.1)' }]}>
                <Text style={{ fontSize: 11, fontWeight: '700', color: r.severity === 'High' ? colors.danger : colors.warning }}>
                  {r.severity === 'High' ? 'Nghiêm trọng' : 'Nhẹ'}
                </Text>
              </View>
            </View>
            <Text style={s.recordDesc} numberOfLines={2}>{r.description}</Text>
            <Text style={s.recordDate}>{r.occurredDate}</Text>
          </View>
        ))
      )}

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

function StatBox({ icon, label, value, color }: any) {
  return (
    <View style={[s.statBox, { backgroundColor: color }]}>
      <Text style={s.statIcon}>{icon}</Text>
      <Text style={s.statValue}>{value}</Text>
      <Text style={s.statLabel}>{label}</Text>
    </View>
  );
}

const s = StyleSheet.create({
  page:         { flex: 1, backgroundColor: colors.ivory },
  center:       { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.ivory },
  header:       {
    backgroundColor: colors.navy, paddingTop: 56, paddingBottom: spacing.lg,
    paddingHorizontal: spacing.lg, flexDirection: 'row',
    alignItems: 'center', justifyContent: 'space-between',
  },
  greeting:     { fontSize: 20, fontWeight: '700', color: colors.ivory },
  sub:          { fontSize: 12, color: 'rgba(250,248,244,0.5)', marginTop: 2 },
  avatar:       { width: 40, height: 40, borderRadius: 20, backgroundColor: colors.gold, alignItems: 'center', justifyContent: 'center' },
  avatarText:   { fontSize: 16, fontWeight: '700', color: colors.navy },

  statsRow:     { flexDirection: 'row', padding: spacing.md, gap: spacing.sm },
  statBox:      {
    flex: 1, borderRadius: radius.md, padding: spacing.md, alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 6, elevation: 2,
  },
  statIcon:     { fontSize: 28, marginBottom: 4 },
  statValue:    { fontSize: 28, fontWeight: '800', color: colors.navy },
  statLabel:    { fontSize: 11, color: colors.textMuted, textAlign: 'center', marginTop: 2 },

  sectionTitle: { ...typography.label, marginHorizontal: spacing.md, marginBottom: spacing.sm },
  recordCard:   {
    backgroundColor: colors.white, marginHorizontal: spacing.md, marginBottom: spacing.sm,
    borderRadius: radius.sm, padding: spacing.md,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 4, elevation: 1,
  },
  recordHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  studentName:  { fontSize: 14, fontWeight: '700', color: colors.textPrimary },
  badge:        { borderRadius: radius.full, paddingHorizontal: 8, paddingVertical: 3 },
  recordDesc:   { fontSize: 13, color: colors.textMuted, lineHeight: 18, marginBottom: 6 },
  recordDate:   { fontSize: 11, color: colors.textMuted },

  empty:        { margin: spacing.lg, padding: spacing.xl, alignItems: 'center' },
  emptyIcon:    { fontSize: 48, marginBottom: spacing.sm },
  emptyText:    { fontSize: 15, color: colors.textMuted },
});
