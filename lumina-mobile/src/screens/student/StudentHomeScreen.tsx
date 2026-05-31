import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, ActivityIndicator, RefreshControl } from 'react-native';
import { useAuth } from '../../context/AuthContext';
import { commonApi, studentApi } from '../../api/client';
import { colors, spacing, radius, typography } from '../../theme';

export default function StudentHomeScreen({ navigation }: any) {
  const { user, logout } = useAuth();
  const [semesters,    setSemesters]    = useState<any[]>([]);
  const [activeSem,    setActiveSem]    = useState<any>(null);
  const [grades,       setGrades]       = useState<any>(null);
  const [attendance,   setAttendance]   = useState<any>(null);
  const [notifications,setNotifications]= useState<any[]>([]);
  const [loading,      setLoading]      = useState(true);
  const [refreshing,   setRefreshing]   = useState(false);

  const loadData = async () => {
    try {
      const [semRes, notifRes] = await Promise.all([
        commonApi.semesters(),
        commonApi.notifications(),
      ]);
      const sems = semRes.data ?? [];
      setSemesters(sems);
      setNotifications(notifRes.data?.items ?? []);

      const sem = sems.find((s: any) => s.isActive) ?? sems[0];
      if (sem) {
        setActiveSem(sem);
        const [gradeRes, attRes] = await Promise.all([
          studentApi.grades(sem.semesterId),
          studentApi.attendance(sem.semesterId),
        ]);
        setGrades(gradeRes.data);
        setAttendance(attRes.data);
      }
    } catch {}
    finally { setLoading(false); setRefreshing(false); }
  };

  useEffect(() => { loadData(); }, []);

  const onRefresh = () => { setRefreshing(true); loadData(); };

  if (loading) return <View style={s.center}><ActivityIndicator color={colors.navy} size="large" /></View>;

  const firstName = user?.fullName.split(' ').pop() ?? user?.fullName ?? '';

  return (
    <ScrollView style={s.page} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}>

      {/* Header */}
      <View style={s.header}>
        <View>
          <Text style={s.greeting}>Xin chào, {firstName} 👋</Text>
          <Text style={s.schoolName}>{user?.schoolName}</Text>
        </View>
        <TouchableOpacity style={s.avatar} onPress={logout}>
          <Text style={s.avatarText}>{user?.fullName[0]}</Text>
        </TouchableOpacity>
      </View>

      {/* GPA card */}
      {grades && (
        <View style={s.gpaCard}>
          <View style={s.gpaLeft}>
            <Text style={s.gpaLabel}>ĐTB học kỳ</Text>
            <Text style={s.gpaValue}>
              {grades.semesterGpa?.toFixed(1) ?? '—'}
            </Text>
            <Text style={s.gpaSem}>{activeSem?.semesterName}</Text>
          </View>
          <View style={s.gpaDivider} />
          <View style={s.gpaRight}>
            <StatPill label="Môn học" value={grades.subjectAverages?.length ?? 0} />
            <StatPill label="Vắng mặt" value={attendance?.absenceCount ?? 0} danger={attendance?.absenceCount > 5} />
          </View>
        </View>
      )}

      {/* Quick actions */}
      <Text style={s.sectionTitle}>Chức năng</Text>
      <View style={s.grid}>
        <QuickAction icon="📊" label="Xem điểm"    onPress={() => navigation.navigate('Grades')} />
        <QuickAction icon="📅" label="Điểm danh"   onPress={() => navigation.navigate('Attendance')} />
        <QuickAction icon="🔔" label="Thông báo"   onPress={() => navigation.navigate('Notifications')} />
        <QuickAction icon="🧠" label="Gia sư AI"   onPress={() => {}} />
      </View>

      {/* Recent notifications */}
      {notifications.length > 0 && (
        <>
          <Text style={s.sectionTitle}>Thông báo gần đây</Text>
          {notifications.slice(0, 3).map((n: any, i: number) => (
            <View key={i} style={s.notifCard}>
              <Text style={s.notifTitle} numberOfLines={1}>{n.title}</Text>
              <Text style={s.notifBody} numberOfLines={2}>{n.body}</Text>
            </View>
          ))}
        </>
      )}

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

function StatPill({ label, value, danger }: { label: string; value: number; danger?: boolean }) {
  return (
    <View style={{ alignItems: 'center', paddingHorizontal: 12 }}>
      <Text style={{ fontSize: 22, fontWeight: '800', color: danger ? colors.danger : colors.navy }}>{value}</Text>
      <Text style={{ fontSize: 11, color: colors.textMuted }}>{label}</Text>
    </View>
  );
}

function QuickAction({ icon, label, onPress }: { icon: string; label: string; onPress: () => void }) {
  return (
    <TouchableOpacity style={s.quickBtn} onPress={onPress} activeOpacity={0.8}>
      <Text style={s.quickIcon}>{icon}</Text>
      <Text style={s.quickLabel}>{label}</Text>
    </TouchableOpacity>
  );
}

const s = StyleSheet.create({
  page:       { flex: 1, backgroundColor: colors.ivory },
  center:     { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.ivory },

  header:     {
    backgroundColor: colors.navy, paddingTop: 56, paddingBottom: spacing.lg,
    paddingHorizontal: spacing.lg, flexDirection: 'row',
    alignItems: 'center', justifyContent: 'space-between',
  },
  greeting:   { fontSize: 20, fontWeight: '700', color: colors.ivory },
  schoolName: { fontSize: 12, color: 'rgba(250,248,244,0.5)', marginTop: 2 },
  avatar:     {
    width: 40, height: 40, borderRadius: 20,
    backgroundColor: colors.gold, alignItems: 'center', justifyContent: 'center',
  },
  avatarText: { fontSize: 16, fontWeight: '700', color: colors.navy },

  gpaCard:    {
    margin: spacing.md, borderRadius: radius.lg,
    backgroundColor: colors.white, padding: spacing.lg,
    flexDirection: 'row', alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08, shadowRadius: 12, elevation: 4,
  },
  gpaLeft:    { flex: 1 },
  gpaLabel:   { fontSize: 11, color: colors.textMuted, fontWeight: '600', textTransform: 'uppercase', letterSpacing: 0.6 },
  gpaValue:   { fontSize: 40, fontWeight: '800', color: colors.navy, lineHeight: 48 },
  gpaSem:     { fontSize: 12, color: colors.textMuted, marginTop: 2 },
  gpaDivider: { width: 1, height: 60, backgroundColor: colors.border, marginHorizontal: spacing.md },
  gpaRight:   { flexDirection: 'row' },

  sectionTitle: { ...typography.label, marginHorizontal: spacing.md, marginTop: spacing.lg, marginBottom: spacing.sm },
  grid:       { flexDirection: 'row', flexWrap: 'wrap', paddingHorizontal: spacing.sm },
  quickBtn:   {
    width: '46%', margin: '2%',
    backgroundColor: colors.white, borderRadius: radius.md,
    padding: spacing.md, alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 6, elevation: 2,
  },
  quickIcon:  { fontSize: 28, marginBottom: 6 },
  quickLabel: { fontSize: 13, fontWeight: '600', color: colors.textPrimary },

  notifCard:  {
    marginHorizontal: spacing.md, marginBottom: spacing.sm,
    backgroundColor: colors.white, borderRadius: radius.sm,
    padding: spacing.md, borderLeftWidth: 3, borderLeftColor: colors.gold,
  },
  notifTitle: { fontSize: 14, fontWeight: '600', color: colors.textPrimary, marginBottom: 3 },
  notifBody:  { fontSize: 13, color: colors.textMuted, lineHeight: 18 },
});
