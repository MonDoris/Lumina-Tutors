import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity, Alert,
} from 'react-native';
import AppShell from '../../components/AppShell';
import {
  LoadingView, EmptyView, SectionTitle, StatCard,
  TabPage, Card, ScorePill, NotifItem, Badge,
} from '../../components/ui';
import { teacherApi, commonApi } from '../../api/client';

/* ═══════════════════════════════════════════════════════════ */
/*  TEACHER — 4 tabs: Tổng quan | Lớp & ĐD | Sổ điểm | TB  */
/* ═══════════════════════════════════════════════════════════ */

export default function TeacherHomeScreen() {
  const [classes,          setClasses]          = useState<any[]>([]);
  const [subjectAssignments, setSubjectAssignments] = useState<any[]>([]);
  const [academicYears,    setAcademicYears]    = useState<any[]>([]);
  const [activeYear,       setActiveYear]       = useState<any>(null);
  const [notifications,    setNotifications]    = useState<any[]>([]);
  const [loading,          setLoading]          = useState(true);
  const [refreshing,       setRefreshing]       = useState(false);

  const loadAll = useCallback(async () => {
    try {
      const [yearRes, notifRes, saRes] = await Promise.all([
        commonApi.academicYears(),
        commonApi.notifications(),
        teacherApi.subjectAssignments(),
      ]);
      const years = yearRes.data ?? [];
      setAcademicYears(years);
      setNotifications(notifRes.data?.items ?? notifRes.data ?? []);
      setSubjectAssignments(saRes.data ?? []);

      const year = years.find((y: any) => y.isActive) ?? years[0];
      if (year) {
        setActiveYear(year);
        const cls = await teacherApi.classes(year.academicYearId);
        setClasses(cls.data ?? []);
      }
    } catch {}
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { loadAll(); }, [loadAll]);
  const onRefresh = () => { setRefreshing(true); loadAll(); };

  if (loading) return <LoadingView />;

  const totalStudents = classes.reduce((sum: number, c: any) => sum + (c.enrolledCount ?? 0), 0);

  return (
    <AppShell accentColor="#d97706" roleLabel="Giáo viên" tabs={[
      { key: 'home',    label: 'Tổng quan', icon: '🏠',
        content: <DashboardTab classes={classes} totalStudents={totalStudents} refreshing={refreshing} onRefresh={onRefresh} /> },
      { key: 'classes', label: 'Lớp & ĐD',  icon: '🏫',
        content: <ClassesTab classes={classes} academicYears={academicYears} activeYear={activeYear}
                             setActiveYear={setActiveYear} setClasses={setClasses} /> },
      { key: 'grades',  label: 'Sổ điểm',  icon: '📝',
        content: <GradeBookTab subjectAssignments={subjectAssignments} /> },
      { key: 'notif',   label: 'Thông báo', icon: '🔔',
        content: <NotifTab notifications={notifications} refreshing={refreshing} onRefresh={onRefresh} /> },
    ]} />
  );
}

/* ── Dashboard ─────────────────────────────────────────────── */
function DashboardTab({ classes, totalStudents, refreshing, onRefresh }: any) {
  const today = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: 'numeric', month: 'long' });
  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      <View style={t.welcome}>
        <Text style={t.welcomeTitle}>Chào buổi {greetingTime()} 👋</Text>
        <Text style={t.welcomeSub}>{today}</Text>
      </View>

      <View style={t.statsRow}>
        <StatCard label="Lớp đang dạy"  value={classes.length}   accent="#d97706" />
        <View style={{ width: 10 }} />
        <StatCard label="Tổng học sinh" value={totalStudents}     accent="#2563eb" />
      </View>

      <SectionTitle>Lớp học ({classes.length})</SectionTitle>
      {classes.length === 0
        ? <EmptyView icon="🏫" text="Chưa có lớp nào được phân công" />
        : classes.map((c: any) => <ClassCard key={c.classId} cls={c} />)
      }
    </TabPage>
  );
}

/* ── Classes & Attendance ───────────────────────────────────── */
function ClassesTab({ classes, academicYears, activeYear, setActiveYear, setClasses }: any) {
  const [selected, setSelected] = useState<any>(null);
  const [report,   setReport]   = useState<any>(null);
  const [loading,  setLoading]  = useState(false);

  const openClass = async (cls: any) => {
    setSelected(cls); setReport(null); setLoading(true);
    try {
      const r = await teacherApi.attendanceSessions(cls.classId);
      setReport(r.data);
    } catch {}
    finally { setLoading(false); }
  };

  const switchYear = async (year: any) => {
    setActiveYear(year); setSelected(null);
    try {
      const r = await teacherApi.classes(year.academicYearId);
      setClasses(r.data ?? []);
    } catch {}
  };

  if (selected) {
    const todayStr = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit' });
    return (
      <TabPage>
        <TouchableOpacity style={t.back} onPress={() => setSelected(null)}>
          <Text style={t.backText}>← Danh sách lớp</Text>
        </TouchableOpacity>
        <View style={t.classHeader}>
          <Text style={t.classHeaderName}>{selected.className}</Text>
          <Text style={t.classHeaderSub}>{selected.gradeName} · {selected.enrolledCount ?? 0} học sinh</Text>
        </View>

        {loading && <LoadingView />}
        {!loading && report && (
          <>
            {/* Stats */}
            <View style={t.statsRow}>
              <StatCard label="Có mặt"   value={report.presentCount ?? 0} accent="#16a34a" />
              <View style={{ width: 8 }} />
              <StatCard label="Vắng"     value={report.absentCount  ?? 0} accent="#dc2626" />
              <View style={{ width: 8 }} />
              <StatCard label="Muộn"     value={report.lateCount    ?? 0} accent="#d97706" />
            </View>

            <SectionTitle>Điểm danh — {todayStr}</SectionTitle>
            {!report.records?.length
              ? <EmptyView icon="📋" text="Chưa có phiên điểm danh hôm nay" />
              : report.records.map((r: any, i: number) => (
                <View key={i} style={t.attRow}>
                  <View style={t.attAvatar}>
                    <Text style={t.attAvatarText}>{(r.studentName ?? '?')[0]}</Text>
                  </View>
                  <View style={{ flex: 1 }}>
                    <Text style={t.attName}>{r.studentName}</Text>
                    <Text style={t.attCode}>{r.studentCode}</Text>
                  </View>
                  <Badge
                    text={r.status === 'Present' ? 'Có mặt' : r.status === 'Absent' ? 'Vắng' : r.status === 'Late' ? 'Muộn' : r.status}
                    color={r.status === 'Present' ? '#16a34a' : r.status === 'Absent' ? '#dc2626' : '#d97706'}
                    bg={r.status === 'Present' ? '#f0fdf4' : r.status === 'Absent' ? '#fef2f2' : '#fffbeb'}
                  />
                </View>
              ))
            }
          </>
        )}
      </TabPage>
    );
  }

  return (
    <TabPage>
      {academicYears.length > 1 && (
        <>
          <SectionTitle>Năm học</SectionTitle>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 14 }}>
            {academicYears.map((y: any) => (
              <TouchableOpacity
                key={y.academicYearId}
                style={[t.semBtn, activeYear?.academicYearId === y.academicYearId && { backgroundColor: '#d97706' }]}
                onPress={() => switchYear(y)}
              >
                <Text style={[t.semText, activeYear?.academicYearId === y.academicYearId && { color: '#fff' }]}>
                  {y.yearName}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
        </>
      )}

      <SectionTitle>Lớp đang dạy</SectionTitle>
      {classes.length === 0
        ? <EmptyView icon="🏫" text="Chưa có lớp nào" />
        : classes.map((cls: any) => (
          <TouchableOpacity key={cls.classId} onPress={() => openClass(cls)} activeOpacity={0.8}>
            <ClassCard cls={cls} showArrow />
          </TouchableOpacity>
        ))
      }
    </TabPage>
  );
}

/* ── Grade Book ─────────────────────────────────────────────── */
function GradeBookTab({ subjectAssignments }: any) {
  const [selected, setSelected] = useState<any>(null);
  const [gbData,   setGbData]   = useState<any>(null);
  const [loading,  setLoading]  = useState(false);

  const openGradeBook = async (sa: any) => {
    setSelected(sa); setLoading(true);
    try {
      const r = await teacherApi.gradeBook(sa.id);
      setGbData(r.data);
    } catch {}
    finally { setLoading(false); }
  };

  if (selected) {
    if (loading) return <LoadingView />;
    return (
      <TabPage>
        <TouchableOpacity style={t.back} onPress={() => { setSelected(null); setGbData(null); }}>
          <Text style={t.backText}>← Danh sách môn</Text>
        </TouchableOpacity>
        <View style={t.classHeader}>
          <Text style={t.classHeaderName}>{gbData?.subjectName ?? selected.subjectName}</Text>
          <Text style={t.classHeaderSub}>{gbData?.className ?? selected.className} · {gbData?.semesterName}</Text>
        </View>

        {gbData?.rows?.length > 0 ? (
          <>
            <SectionTitle>Sổ điểm ({gbData.rows.length} học sinh)</SectionTitle>
            {gbData.rows.map((row: any, i: number) => (
              <View key={i} style={t.gbCard}>
                <View style={t.gbLeft}>
                  <Text style={t.gbName}>{row.studentName}</Text>
                  <Text style={t.gbCode}>{row.studentCode}</Text>
                </View>
                <View style={t.gbScores}>
                  {row.regularScores?.filter((sc: any) => sc != null).map((sc: number, j: number) => (
                    <View key={j} style={t.gbScoreItem}>
                      <Text style={t.gbScoreLabel}>TX{j + 1}</Text>
                      <ScorePill score={sc} />
                    </View>
                  ))}
                  {row.midTermScore != null && (
                    <View style={t.gbScoreItem}>
                      <Text style={t.gbScoreLabel}>GK</Text>
                      <ScorePill score={row.midTermScore} />
                    </View>
                  )}
                  {row.finalScore != null && (
                    <View style={t.gbScoreItem}>
                      <Text style={t.gbScoreLabel}>CK</Text>
                      <ScorePill score={row.finalScore} />
                    </View>
                  )}
                  {row.averageScore != null && (
                    <View style={t.gbScoreItem}>
                      <Text style={[t.gbScoreLabel, { color: '#d97706', fontWeight: '800' }]}>TB</Text>
                      <ScorePill score={row.averageScore} />
                    </View>
                  )}
                </View>
              </View>
            ))}
          </>
        ) : <EmptyView text="Chưa có dữ liệu điểm" />}
      </TabPage>
    );
  }

  return (
    <TabPage>
      <SectionTitle>Chọn lớp - môn để xem sổ điểm</SectionTitle>
      {!subjectAssignments.length
        ? <EmptyView icon="📝" text="Chưa có phân công môn học" />
        : subjectAssignments.map((sa: any) => (
          <TouchableOpacity key={sa.id} onPress={() => openGradeBook(sa)} activeOpacity={0.8}>
            <Card style={{ flexDirection: 'row', alignItems: 'center' }}>
              <View style={t.saIcon}>
                <Text style={{ fontSize: 20 }}>📝</Text>
              </View>
              <View style={{ flex: 1 }}>
                <Text style={t.saSubject}>{sa.subjectName}</Text>
                <Text style={t.saClass}>{sa.className}</Text>
              </View>
              <Text style={{ fontSize: 20, color: '#94a3b8' }}>›</Text>
            </Card>
          </TouchableOpacity>
        ))
      }
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

/* ── Shared ─────────────────────────────────────────────────── */
function ClassCard({ cls, showArrow }: { cls: any; showArrow?: boolean }) {
  return (
    <Card style={{ flexDirection: 'row', alignItems: 'center' }}>
      <View style={t.classIcon}>
        <Text style={{ fontSize: 22 }}>🏫</Text>
      </View>
      <View style={{ flex: 1 }}>
        <Text style={t.className}>{cls.className}</Text>
        <Text style={t.classSub}>{cls.gradeName} · {cls.enrolledCount ?? 0} học sinh</Text>
        {cls.homeRoomTeacherName && (
          <Text style={t.classTeacher}>GVCN: {cls.homeRoomTeacherName}</Text>
        )}
      </View>
      {showArrow && <Text style={{ fontSize: 20, color: '#94a3b8' }}>›</Text>}
    </Card>
  );
}

function greetingTime() {
  const h = new Date().getHours();
  if (h < 12) return 'sáng';
  if (h < 18) return 'chiều';
  return 'tối';
}

const t = StyleSheet.create({
  welcome:      { backgroundColor: '#b45309', borderRadius: 16, padding: 18, marginBottom: 12 },
  welcomeTitle: { fontSize: 18, fontWeight: '800', color: '#fff' },
  welcomeSub:   { fontSize: 12, color: 'rgba(255,255,255,0.65)', marginTop: 3 },
  statsRow:     { flexDirection: 'row', marginBottom: 12 },

  semBtn:  { paddingHorizontal: 16, paddingVertical: 9, borderRadius: 20, backgroundColor: '#f1f5f9', marginRight: 8 },
  semText: { fontSize: 13, fontWeight: '600', color: '#64748b' },

  back:     { marginBottom: 12 },
  backText: { fontSize: 14, fontWeight: '700', color: '#d97706' },

  classHeader:    { backgroundColor: '#b45309', borderRadius: 14, padding: 16, marginBottom: 14 },
  classHeaderName:{ fontSize: 18, fontWeight: '800', color: '#fff' },
  classHeaderSub: { fontSize: 12, color: 'rgba(255,255,255,0.7)', marginTop: 3 },

  classIcon:    { width: 48, height: 48, borderRadius: 14, backgroundColor: '#fef3c7', alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  className:    { fontSize: 15, fontWeight: '700', color: '#1e293b' },
  classSub:     { fontSize: 11, color: '#94a3b8', marginTop: 2 },
  classTeacher: { fontSize: 11, color: '#d97706', fontWeight: '600', marginTop: 2 },

  attRow: {
    backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8,
    flexDirection: 'row', alignItems: 'center', gap: 12,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2,
  },
  attAvatar:     { width: 40, height: 40, borderRadius: 12, backgroundColor: '#fef3c7', alignItems: 'center', justifyContent: 'center' },
  attAvatarText: { fontSize: 16, fontWeight: '800', color: '#d97706' },
  attName:       { fontSize: 14, fontWeight: '700', color: '#1e293b' },
  attCode:       { fontSize: 11, color: '#94a3b8', marginTop: 2 },

  saIcon:    { width: 44, height: 44, borderRadius: 12, backgroundColor: '#fef3c7', alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  saSubject: { fontSize: 15, fontWeight: '700', color: '#1e293b' },
  saClass:   { fontSize: 12, color: '#d97706', fontWeight: '600', marginTop: 2 },

  gbCard: {
    backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2,
  },
  gbLeft:      { marginBottom: 10 },
  gbName:      { fontSize: 14, fontWeight: '700', color: '#1e293b' },
  gbCode:      { fontSize: 11, color: '#94a3b8', marginTop: 2 },
  gbScores:    { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  gbScoreItem: { alignItems: 'center', gap: 3 },
  gbScoreLabel:{ fontSize: 9, fontWeight: '700', color: '#94a3b8', textTransform: 'uppercase' },
});
