import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
} from 'react-native';
import AppShell from '../../components/AppShell';
import {
  LoadingView, EmptyView, SectionTitle, StatCard,
  TabPage, QuickBtn, ScorePill, Card, NotifItem, Badge, scoreColor,
} from '../../components/ui';
import { studentApi, commonApi } from '../../api/client';

/* ═══════════════════════════════════════════════════════════ */
/*  STUDENT — 4 tabs: Tổng quan | Điểm số | Điểm danh | TB  */
/* ═══════════════════════════════════════════════════════════ */

export default function StudentHomeScreen() {
  const [semesters,     setSemesters]     = useState<any[]>([]);
  const [activeSem,     setActiveSem]     = useState<any>(null);
  const [grades,        setGrades]        = useState<any>(null);
  const [attendance,    setAttendance]    = useState<any>(null);
  const [notifications, setNotifications] = useState<any[]>([]);
  const [loading,       setLoading]       = useState(true);
  const [refreshing,    setRefreshing]    = useState(false);

  const fetchGrades = async (semId: number) => {
    const r = await studentApi.grades(semId); return r.data;
  };
  const fetchAttendance = async (semId: number) => {
    const r = await studentApi.attendance(semId); return r.data;
  };

  const loadAll = useCallback(async () => {
    try {
      const [semRes, notifRes] = await Promise.all([
        commonApi.semesters(),
        commonApi.notifications(),
      ]);
      const sems  = semRes.data ?? [];
      const notif = notifRes.data?.items ?? notifRes.data ?? [];
      setSemesters(sems);
      setNotifications(notif);

      const sem = sems.find((s: any) => s.isActive) ?? sems[0];
      if (sem) {
        setActiveSem(sem);
        const [g, a] = await Promise.all([fetchGrades(sem.semesterId), fetchAttendance(sem.semesterId)]);
        setGrades(g); setAttendance(a);
      }
    } catch {}
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { loadAll(); }, [loadAll]);
  const onRefresh = () => { setRefreshing(true); loadAll(); };

  if (loading) return <LoadingView />;

  const shared = { semesters, activeSem, setActiveSem, grades, setGrades, attendance, setAttendance };

  return (
    <AppShell accentColor="#2563eb" roleLabel="Học sinh" tabs={[
      { key: 'home',   label: 'Tổng quan', icon: '🏠',
        content: <DashboardTab grades={grades} attendance={attendance} activeSem={activeSem} refreshing={refreshing} onRefresh={onRefresh} /> },
      { key: 'grades', label: 'Điểm số',   icon: '📊',
        content: <GradesTab {...shared} /> },
      { key: 'att',    label: 'Điểm danh', icon: '📅',
        content: <AttendanceTab {...shared} /> },
      { key: 'notif',  label: 'Thông báo', icon: '🔔',
        content: <NotifTab notifications={notifications} refreshing={refreshing} onRefresh={onRefresh} /> },
    ]} />
  );
}

/* ── Dashboard ─────────────────────────────────────────────── */
function DashboardTab({ grades, attendance, activeSem, refreshing, onRefresh }: any) {
  const rate = attendance?.attendanceRate ?? 0;
  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      <View style={s.hero}>
        <View style={s.heroLeft}>
          <Text style={s.heroLabel}>ĐTB học kỳ</Text>
          <Text style={[s.heroGpa, { color: scoreColor(grades?.semesterGpa) }]}>
            {grades?.semesterGpa != null ? grades.semesterGpa.toFixed(2) : '—'}
          </Text>
          <Text style={s.heroSem}>{activeSem?.semesterName}</Text>
        </View>
        <View style={s.heroDivider} />
        <View style={s.heroRight}>
          <View style={s.heroStat}>
            <Text style={s.heroStatVal}>{grades?.subjectAverages?.length ?? 0}</Text>
            <Text style={s.heroStatLabel}>Môn học</Text>
          </View>
          <View style={s.heroStat}>
            <Text style={[s.heroStatVal, { color: (attendance?.absentCount ?? 0) > 5 ? '#dc2626' : '#16a34a' }]}>
              {attendance?.absentCount ?? 0}
            </Text>
            <Text style={s.heroStatLabel}>Buổi vắng</Text>
          </View>
          <View style={s.heroStat}>
            <Text style={[s.heroStatVal, { color: rate >= 80 ? '#16a34a' : '#dc2626' }]}>
              {rate.toFixed(0)}%
            </Text>
            <Text style={s.heroStatLabel}>Chuyên cần</Text>
          </View>
        </View>
      </View>

      {grades?.semesterRemark ? (
        <Card style={{ marginBottom: 12 }}>
          <Text style={{ fontSize: 11, fontWeight: '700', color: '#94a3b8', marginBottom: 4, textTransform: 'uppercase', letterSpacing: 0.5 }}>Nhận xét</Text>
          <Text style={{ fontSize: 14, color: '#1e293b', lineHeight: 20 }}>{grades.semesterRemark}</Text>
        </Card>
      ) : null}

      <SectionTitle>Chức năng nhanh</SectionTitle>
      <View style={s.grid}>
        <QuickBtn icon="📊" label="Điểm số"   color="#2563eb" onPress={() => {}} />
        <QuickBtn icon="📅" label="Điểm danh" color="#16a34a" onPress={() => {}} />
        <QuickBtn icon="📖" label="Bài tập"   color="#d97706" onPress={() => {}} />
        <QuickBtn icon="🤖" label="Gia sư AI" color="#7c3aed" onPress={() => {}} />
      </View>

      {grades?.subjectAverages?.length > 0 && (
        <>
          <SectionTitle>Điểm các môn — {activeSem?.semesterName}</SectionTitle>
          {grades.subjectAverages.slice(0, 4).map((sub: any, i: number) => (
            <View key={i} style={s.subRow}>
              <Text style={s.subName} numberOfLines={1}>{sub.subjectName}</Text>
              <ScorePill score={sub.averageScore} />
            </View>
          ))}
        </>
      )}
    </TabPage>
  );
}

/* ── Grades ─────────────────────────────────────────────────── */
function GradesTab({ semesters, activeSem, setActiveSem, grades, setGrades }: any) {
  const [loading, setLoading] = useState(false);

  const switchSem = async (sem: any) => {
    setActiveSem(sem); setLoading(true);
    try { const r = await studentApi.grades(sem.semesterId); setGrades(r.data); }
    catch {} finally { setLoading(false); }
  };

  if (loading) return <LoadingView />;

  return (
    <TabPage>
      <SectionTitle>Chọn học kỳ</SectionTitle>
      <SemPicker semesters={semesters} active={activeSem} onSelect={switchSem} accent="#2563eb" />

      {grades ? (
        <>
          <Card style={{ marginBottom: 14 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
              <View>
                <Text style={s.cardSmLabel}>Điểm trung bình học kỳ</Text>
                <Text style={[s.bigScore, { color: scoreColor(grades.semesterGpa) }]}>
                  {grades.semesterGpa != null ? grades.semesterGpa.toFixed(2) : '—'}
                </Text>
              </View>
              {grades.semesterRemark ? (
                <View style={s.remarkBadge}>
                  <Text style={s.remarkText}>{grades.semesterRemark}</Text>
                </View>
              ) : null}
            </View>
          </Card>

          <SectionTitle>Điểm từng môn ({grades.subjectAverages?.length ?? 0} môn)</SectionTitle>
          {!grades.subjectAverages?.length
            ? <EmptyView text="Chưa có điểm nào" />
            : grades.subjectAverages.map((sub: any, i: number) => (
              <View key={i} style={s.subjectCard}>
                <View style={{ flex: 1 }}>
                  <Text style={s.subjectName}>{sub.subjectName}</Text>
                  {sub.remark ? <Text style={s.subjectRemark} numberOfLines={1}>{sub.remark}</Text> : null}
                </View>
                <ScorePill score={sub.averageScore} />
              </View>
            ))
          }
        </>
      ) : <EmptyView icon="📊" text="Chưa có dữ liệu điểm" />}
    </TabPage>
  );
}

/* ── Attendance ─────────────────────────────────────────────── */
function AttendanceTab({ semesters, activeSem, setActiveSem, attendance, setAttendance }: any) {
  const [loading, setLoading] = useState(false);

  const switchSem = async (sem: any) => {
    setActiveSem(sem); setLoading(true);
    try { const r = await studentApi.attendance(sem.semesterId); setAttendance(r.data); }
    catch {} finally { setLoading(false); }
  };

  if (loading) return <LoadingView />;
  const rate = attendance?.attendanceRate ?? 0;

  return (
    <TabPage>
      <SectionTitle>Chọn học kỳ</SectionTitle>
      <SemPicker semesters={semesters} active={activeSem} onSelect={switchSem} accent="#2563eb" />

      {attendance ? (
        <>
          <View style={s.statsRow}>
            <StatCard label="Tổng buổi"  value={attendance.totalSessions} accent="#2563eb" />
            <View style={{ width: 8 }} />
            <StatCard label="Có mặt"     value={attendance.presentCount}  accent="#16a34a" />
            <View style={{ width: 8 }} />
            <StatCard label="Vắng"       value={attendance.absentCount}   accent="#dc2626" />
            <View style={{ width: 8 }} />
            <StatCard label="Muộn"       value={attendance.lateCount}     accent="#d97706" />
          </View>

          <Card style={{ marginBottom: 14 }}>
            <View style={s.progressHeader}>
              <Text style={s.progressLabel}>Tỷ lệ chuyên cần</Text>
              <Text style={[s.progressPct, { color: rate >= 80 ? '#16a34a' : '#dc2626' }]}>{rate.toFixed(1)}%</Text>
            </View>
            <View style={s.progressBg}>
              <View style={[s.progressFill, {
                width: `${Math.min(rate, 100)}%` as any,
                backgroundColor: rate >= 80 ? '#16a34a' : '#dc2626',
              }]} />
            </View>
            <Text style={{ fontSize: 11, color: '#94a3b8', marginTop: 8 }}>
              {rate >= 80 ? '✅ Đạt yêu cầu chuyên cần (≥ 80%)' : '⚠️ Dưới mức yêu cầu (80%)'}
            </Text>
          </Card>

          {/* absenceDates — danh sách các buổi vắng */}
          {attendance.absenceDates?.length > 0 && (
            <>
              <SectionTitle>Lịch sử buổi vắng / muộn</SectionTitle>
              {attendance.absenceDates.map((r: any, i: number) => (
                <View key={i} style={s.attRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={s.attDate}>{formatDate(r.sessionDate)}</Text>
                    <Text style={s.attSub}>{r.subjectName}</Text>
                  </View>
                  <Badge
                    text={statusLabel(r.status)}
                    color={statusColor(r.status)}
                    bg={statusBg(r.status)}
                  />
                </View>
              ))}
            </>
          )}
        </>
      ) : <EmptyView icon="📅" text="Chưa có dữ liệu điểm danh" />}
    </TabPage>
  );
}

/* ── Notifications ──────────────────────────────────────────── */
function NotifTab({ notifications, refreshing, onRefresh }: any) {
  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      <SectionTitle>Thông báo ({notifications.length})</SectionTitle>
      {!notifications.length
        ? <EmptyView icon="🔕" text="Chưa có thông báo" />
        : notifications.map((n: any, i: number) => <NotifItem key={i} item={n} />)
      }
    </TabPage>
  );
}

/* ── Shared helpers ─────────────────────────────────────────── */
function SemPicker({ semesters, active, onSelect, accent }: any) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 14 }}>
      {semesters.map((sem: any) => {
        const isActive = active?.semesterId === sem.semesterId;
        return (
          <TouchableOpacity
            key={sem.semesterId}
            style={[s.semBtn, isActive && { backgroundColor: accent }]}
            onPress={() => onSelect(sem)}
          >
            <Text style={[s.semText, isActive && { color: '#fff' }]}>{sem.semesterName}</Text>
          </TouchableOpacity>
        );
      })}
    </ScrollView>
  );
}

function statusLabel(s: string) {
  if (s === 'Absent')  return 'Vắng';
  if (s === 'Late')    return 'Muộn';
  if (s === 'Excused') return 'Có phép';
  return s;
}
function statusColor(s: string) {
  if (s === 'Absent')  return '#dc2626';
  if (s === 'Late')    return '#d97706';
  if (s === 'Excused') return '#2563eb';
  return '#64748b';
}
function statusBg(s: string) {
  if (s === 'Absent')  return '#fef2f2';
  if (s === 'Late')    return '#fffbeb';
  if (s === 'Excused') return '#eff6ff';
  return '#f1f5f9';
}
function formatDate(d?: string) {
  if (!d) return '';
  return new Date(d).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

const s = StyleSheet.create({
  hero: {
    backgroundColor: '#fff', borderRadius: 16, padding: 20,
    flexDirection: 'row', marginBottom: 12,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.07, shadowRadius: 8, elevation: 4,
    borderTopWidth: 4, borderTopColor: '#2563eb',
  },
  heroLeft:      { flex: 1, justifyContent: 'center' },
  heroLabel:     { fontSize: 11, fontWeight: '700', color: '#94a3b8', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 4 },
  heroGpa:       { fontSize: 44, fontWeight: '900', lineHeight: 50 },
  heroSem:       { fontSize: 11, color: '#94a3b8', marginTop: 4 },
  heroDivider:   { width: 1, backgroundColor: '#f1f5f9', marginHorizontal: 16 },
  heroRight:     { justifyContent: 'space-around' },
  heroStat:      { alignItems: 'center', paddingHorizontal: 6 },
  heroStatVal:   { fontSize: 20, fontWeight: '800', color: '#1e293b' },
  heroStatLabel: { fontSize: 10, color: '#94a3b8', marginTop: 2, textAlign: 'center' },

  grid:    { flexDirection: 'row', flexWrap: 'wrap', gap: 10, marginBottom: 4 },
  subRow:  { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  subName: { flex: 1, fontSize: 14, fontWeight: '600', color: '#1e293b', marginRight: 8 },

  cardSmLabel: { fontSize: 11, color: '#94a3b8', fontWeight: '600', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 },
  bigScore:    { fontSize: 40, fontWeight: '900' },
  remarkBadge: { backgroundColor: '#eff6ff', borderRadius: 10, paddingHorizontal: 12, paddingVertical: 6, maxWidth: 120 },
  remarkText:  { fontSize: 12, fontWeight: '700', color: '#2563eb', textAlign: 'center' },

  subjectCard:   { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  subjectName:   { fontSize: 14, fontWeight: '700', color: '#1e293b', marginBottom: 2 },
  subjectRemark: { fontSize: 11, color: '#94a3b8' },

  statsRow:      { flexDirection: 'row', marginBottom: 12 },
  progressHeader:{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  progressLabel: { fontSize: 13, fontWeight: '600', color: '#1e293b' },
  progressPct:   { fontSize: 14, fontWeight: '800' },
  progressBg:    { height: 8, backgroundColor: '#f1f5f9', borderRadius: 4, overflow: 'hidden' },
  progressFill:  { height: '100%', borderRadius: 4 },

  attRow:  { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  attDate: { fontSize: 13, fontWeight: '700', color: '#1e293b' },
  attSub:  { fontSize: 11, color: '#94a3b8', marginTop: 2 },

  semBtn:  { paddingHorizontal: 16, paddingVertical: 9, borderRadius: 20, backgroundColor: '#f1f5f9', marginRight: 8 },
  semText: { fontSize: 13, fontWeight: '600', color: '#64748b' },
});
