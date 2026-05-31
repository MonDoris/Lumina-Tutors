import React, { useEffect, useState } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
  ActivityIndicator, RefreshControl,
} from 'react-native';
import { useAuth } from '../../context/AuthContext';
import { commonApi, parentApi } from '../../api/client';
import { colors, spacing, radius, typography } from '../../theme';

export default function ParentHomeScreen({ navigation }: any) {
  const { user, logout } = useAuth();
  const [semesters,  setSemesters]  = useState<any[]>([]);
  const [activeSem,  setActiveSem]  = useState<any>(null);
  const [grades,     setGrades]     = useState<any>(null);
  const [attendance, setAttendance] = useState<any>(null);
  const [loading,    setLoading]    = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  // Demo: dùng userId của parent làm studentId (thực tế cần API get-children)
  const studentId = user?.userId ?? 0;

  const loadData = async () => {
    try {
      const semRes  = await commonApi.semesters();
      const sems    = semRes.data ?? [];
      setSemesters(sems);
      const sem     = sems.find((s: any) => s.isActive) ?? sems[0];
      if (sem) {
        setActiveSem(sem);
        const [gradeRes, attRes] = await Promise.all([
          parentApi.childGrades(studentId, sem.semesterId),
          parentApi.childAttendance(studentId, sem.semesterId),
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

  return (
    <ScrollView style={s.page} refreshControl={<RefreshControl refreshing={refreshing} onRefresh={onRefresh} />}>

      <View style={s.header}>
        <View>
          <Text style={s.greeting}>Xin chào phụ huynh 👋</Text>
          <Text style={s.sub}>{user?.fullName} · {user?.schoolName}</Text>
        </View>
        <TouchableOpacity style={s.avatar} onPress={logout}>
          <Text style={s.avatarText}>{user?.fullName[0]}</Text>
        </TouchableOpacity>
      </View>

      {/* Summary */}
      {grades && (
        <View style={s.summaryCard}>
          <Text style={s.summaryLabel}>Kết quả học tập — {activeSem?.semesterName}</Text>
          <View style={s.summaryRow}>
            <SummaryItem label="ĐTB học kỳ" value={grades.semesterGpa?.toFixed(1) ?? '—'} highlight />
            <SummaryItem label="Số môn" value={grades.subjectAverages?.length ?? 0} />
            <SummaryItem label="Vắng mặt" value={attendance?.absenceCount ?? 0} danger={attendance?.absenceCount > 5} />
          </View>
        </View>
      )}

      {/* Subjects */}
      {grades?.subjectAverages?.length > 0 && (
        <>
          <Text style={s.sectionTitle}>Điểm từng môn</Text>
          {grades.subjectAverages.map((sub: any, i: number) => (
            <View key={i} style={s.subjectRow}>
              <Text style={s.subjectName}>{sub.subjectName}</Text>
              <View style={[s.scorePill, getScoreStyle(sub.averageScore)]}>
                <Text style={s.scoreText}>
                  {sub.averageScore != null ? sub.averageScore.toFixed(1) : '—'}
                </Text>
              </View>
            </View>
          ))}
        </>
      )}

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

function SummaryItem({ label, value, highlight, danger }: any) {
  return (
    <View style={{ alignItems: 'center', flex: 1 }}>
      <Text style={{ fontSize: highlight ? 28 : 22, fontWeight: '800',
        color: danger ? colors.danger : highlight ? colors.gold : colors.navy }}>
        {value}
      </Text>
      <Text style={{ fontSize: 11, color: colors.textMuted, marginTop: 2, textAlign: 'center' }}>{label}</Text>
    </View>
  );
}

function getScoreStyle(score: number | null) {
  if (score == null) return { backgroundColor: colors.bg };
  if (score >= 8)  return { backgroundColor: 'rgba(22,163,74,0.12)' };
  if (score >= 5)  return { backgroundColor: 'rgba(217,119,6,0.12)' };
  return { backgroundColor: 'rgba(220,38,38,0.1)' };
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

  summaryCard:  {
    margin: spacing.md, backgroundColor: colors.white, borderRadius: radius.lg,
    padding: spacing.lg, shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.08, shadowRadius: 12, elevation: 4,
  },
  summaryLabel: { fontSize: 12, color: colors.textMuted, marginBottom: spacing.md, fontWeight: '600' },
  summaryRow:   { flexDirection: 'row', justifyContent: 'space-around' },

  sectionTitle: { ...typography.label, marginHorizontal: spacing.md, marginBottom: spacing.sm, marginTop: 4 },
  subjectRow:   {
    flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
    backgroundColor: colors.white, marginHorizontal: spacing.md, marginBottom: spacing.sm,
    borderRadius: radius.sm, paddingVertical: 12, paddingHorizontal: spacing.md,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 4, elevation: 1,
  },
  subjectName:  { fontSize: 14, fontWeight: '600', color: colors.textPrimary, flex: 1 },
  scorePill:    { borderRadius: radius.full, paddingHorizontal: 12, paddingVertical: 4 },
  scoreText:    { fontSize: 14, fontWeight: '700', color: colors.textPrimary },
});
