import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
} from 'react-native';
import AppShell from '../../components/AppShell';
import {
  LoadingView, EmptyView, SectionTitle, StatCard,
  TabPage, Card, ScorePill, NotifItem, Badge, scoreColor,
} from '../../components/ui';
import { parentApi, commonApi } from '../../api/client';

/* ═══════════════════════════════════════════════════════════ */
/*  PARENT — 4 tabs: Tổng quan | Học tập | Điểm danh | TB   */
/* ═══════════════════════════════════════════════════════════ */

export default function ParentHomeScreen() {
  const [children,      setChildren]      = useState<any[]>([]);
  const [activeChild,   setActiveChild]   = useState<any>(null);
  const [semesters,     setSemesters]     = useState<any[]>([]);
  const [activeSem,     setActiveSem]     = useState<any>(null);
  const [grades,        setGrades]        = useState<any>(null);
  const [attendance,    setAttendance]    = useState<any>(null);
  const [notifications, setNotifications] = useState<any[]>([]);
  const [loading,       setLoading]       = useState(true);
  const [refreshing,    setRefreshing]    = useState(false);

  const fetchChildData = async (studentId: number, semesterId: number) => {
    const [gRes, aRes] = await Promise.all([
      parentApi.childGrades(studentId, semesterId),
      parentApi.childAttendance(studentId, semesterId),
    ]);
    setGrades(gRes.data ?? null);
    setAttendance(aRes.data ?? null);
  };

  const loadAll = useCallback(async () => {
    try {
      const [childRes, semRes, notifRes] = await Promise.all([
        parentApi.children(),
        commonApi.semesters(),
        commonApi.notifications(),
      ]);
      const kids  = childRes.data ?? [];
      const sems  = semRes.data ?? [];
      const notif = notifRes.data?.items ?? notifRes.data ?? [];

      setChildren(kids);
      setSemesters(sems);
      setNotifications(notif);

      const child = kids[0];
      const sem   = sems.find((s: any) => s.isActive) ?? sems[0];
      if (child && sem) {
        setActiveChild(child);
        setActiveSem(sem);
        await fetchChildData(child.userId, sem.semesterId);
      }
    } catch {}
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { loadAll(); }, [loadAll]);
  const onRefresh = () => { setRefreshing(true); loadAll(); };

  const switchChild = async (child: any) => {
    setActiveChild(child);
    if (activeSem) await fetchChildData(child.userId, activeSem.semesterId);
  };

  const switchSem = async (sem: any) => {
    setActiveSem(sem);
    if (activeChild) await fetchChildData(activeChild.userId, sem.semesterId);
  };

  if (loading) return <LoadingView />;

  const shared = { children, activeChild, switchChild, semesters, activeSem, switchSem, grades, attendance };

  return (
    <AppShell accentColor="#7c3aed" roleLabel="Phụ huynh" tabs={[
      { key: 'home',  label: 'Tổng quan', icon: '🏠',
        content: <DashboardTab {...shared} refreshing={refreshing} onRefresh={onRefresh} /> },
      { key: 'grades', label: 'Học tập',  icon: '📊',
        content: <GradesTab {...shared} /> },
      { key: 'att',   label: 'Điểm danh', icon: '📅',
        content: <AttendanceTab {...shared} /> },
      { key: 'notif', label: 'Thông báo', icon: '🔔',
        content: <NotifTab notifications={notifications} refreshing={refreshing} onRefresh={onRefresh} /> },
    ]} />
  );
}

/* ── Child picker (dùng lại ở mọi tab) ─────────────────────── */
function ChildPicker({ children, activeChild, switchChild }: any) {
  if (!children?.length) return null;
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 14 }}>
      {children.map((c: any) => {
        const active = activeChild?.userId === c.userId;
        return (
          <TouchableOpacity key={c.userId} style={p.childBtn} onPress={() => switchChild(c)}>
            <View style={[p.childAvatar, active && { backgroundColor: '#7c3aed' }]}>
              <Text style={[p.childAvatarTxt, active && { color: '#fff' }]}>{(c.fullName ?? '?')[0]}</Text>
            </View>
            <Text style={[p.childLabel, active && { color: '#7c3aed' }]}>
              {c.fullName?.split(' ').pop() ?? c.fullName}
            </Text>
            {c.className ? <Text style={p.childClass}>{c.className}</Text> : null}
          </TouchableOpacity>
        );
      })}
    </ScrollView>
  );
}

function SemPicker({ semesters, activeSem, switchSem }: any) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 14 }}>
      {semesters.map((sem: any) => {
        const active = activeSem?.semesterId === sem.semesterId;
        return (
          <TouchableOpacity
            key={sem.semesterId}
            style={[p.semBtn, active && { backgroundColor: '#7c3aed' }]}
            onPress={() => switchSem(sem)}
          >
            <Text style={[p.semText, active && { color: '#fff' }]}>{sem.semesterName}</Text>
          </TouchableOpacity>
        );
      })}
    </ScrollView>
  );
}

/* ── Dashboard ─────────────────────────────────────────────── */
function DashboardTab({ children, activeChild, switchChild, grades, attendance, activeSem, refreshing, onRefresh }: any) {
  const rate = attendance?.attendanceRate ?? 0;
  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      {children.length > 1 && (
        <>
          <SectionTitle>Con em</SectionTitle>
          <ChildPicker children={children} activeChild={activeChild} switchChild={switchChild} />
        </>
      )}

      {activeChild ? (
        <>
          {/* Child info card */}
          <Card style={{ marginBottom: 12 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
              <View style={p.bigAvatar}>
                <Text style={p.bigAvatarTxt}>{(activeChild.fullName ?? '?')[0]}</Text>
              </View>
              <View style={{ flex: 1 }}>
                <Text style={p.childFullName}>{activeChild.fullName}</Text>
                <Text style={p.childClassName}>{activeChild.className ?? 'Chưa xếp lớp'}</Text>
                <Text style={p.childCode}>{activeChild.studentCode}</Text>
              </View>
            </View>
          </Card>

          {/* Stats */}
          <View style={p.statsRow}>
            <StatCard
              label="ĐTB học kỳ"
              value={grades?.semesterGpa != null ? grades.semesterGpa.toFixed(1) : '—'}
              sub={activeSem?.semesterName}
              accent="#7c3aed"
            />
            <View style={{ width: 10 }} />
            <StatCard
              label="Buổi vắng"
              value={attendance?.absentCount ?? '—'}
              sub={`/ ${attendance?.totalSessions ?? 0} buổi`}
              accent={(attendance?.absentCount ?? 0) > 5 ? '#dc2626' : '#16a34a'}
            />
          </View>

          {/* Chuyên cần */}
          {attendance && (
            <Card>
              <View style={p.progressHead}>
                <Text style={p.progressLabel}>Tỷ lệ chuyên cần</Text>
                <Text style={[p.progressPct, { color: rate >= 80 ? '#16a34a' : '#dc2626' }]}>{rate.toFixed(1)}%</Text>
              </View>
              <View style={p.progressBg}>
                <View style={[p.progressFill, {
                  width: `${Math.min(rate, 100)}%` as any,
                  backgroundColor: rate >= 80 ? '#16a34a' : '#dc2626',
                }]} />
              </View>
              <Text style={{ fontSize: 11, color: '#94a3b8', marginTop: 8 }}>
                {rate >= 80 ? '✅ Đạt yêu cầu chuyên cần' : '⚠️ Cần cải thiện — dưới mức 80%'}
              </Text>
            </Card>
          )}

          {/* Nhận xét */}
          {grades?.semesterRemark ? (
            <Card style={{ marginTop: 12 }}>
              <Text style={{ fontSize: 11, fontWeight: '700', color: '#94a3b8', marginBottom: 4, textTransform: 'uppercase', letterSpacing: 0.5 }}>
                Nhận xét giáo viên
              </Text>
              <Text style={{ fontSize: 14, color: '#1e293b', lineHeight: 20 }}>{grades.semesterRemark}</Text>
            </Card>
          ) : null}
        </>
      ) : (
        <EmptyView icon="👨‍👩‍👧" text="Chưa có học sinh liên kết với tài khoản này" />
      )}
    </TabPage>
  );
}

/* ── Grades ─────────────────────────────────────────────────── */
function GradesTab({ children, activeChild, switchChild, semesters, activeSem, switchSem, grades }: any) {
  return (
    <TabPage>
      {children.length > 1 && (
        <>
          <SectionTitle>Con em</SectionTitle>
          <ChildPicker children={children} activeChild={activeChild} switchChild={switchChild} />
        </>
      )}
      <SectionTitle>Học kỳ</SectionTitle>
      <SemPicker semesters={semesters} activeSem={activeSem} switchSem={switchSem} />

      {grades ? (
        <>
          <Card style={{ marginBottom: 14 }}>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
              <View>
                <Text style={p.gpaLabel}>Điểm trung bình học kỳ</Text>
                <Text style={[p.gpaValue, { color: scoreColor(grades.semesterGpa) }]}>
                  {grades.semesterGpa != null ? grades.semesterGpa.toFixed(2) : '—'}
                </Text>
              </View>
              {grades.semesterRemark ? (
                <View style={{ backgroundColor: '#f5f3ff', borderRadius: 12, padding: 12, maxWidth: 130, alignItems: 'center' }}>
                  <Text style={{ fontSize: 13, fontWeight: '800', color: '#7c3aed', textAlign: 'center' }}>
                    {grades.semesterRemark}
                  </Text>
                </View>
              ) : null}
            </View>
            <Text style={{ fontSize: 12, color: '#94a3b8', marginTop: 8 }}>
              {activeChild?.fullName} · {activeSem?.semesterName}
            </Text>
          </Card>

          <SectionTitle>Điểm từng môn ({grades.subjectAverages?.length ?? 0} môn)</SectionTitle>
          {!grades.subjectAverages?.length
            ? <EmptyView text="Chưa có điểm" />
            : grades.subjectAverages.map((sub: any, i: number) => (
              <View key={i} style={p.subRow}>
                <View style={{ flex: 1 }}>
                  <Text style={p.subName}>{sub.subjectName}</Text>
                  {sub.remark ? <Text style={p.subRemark} numberOfLines={1}>{sub.remark}</Text> : null}
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
function AttendanceTab({ children, activeChild, switchChild, semesters, activeSem, switchSem, attendance }: any) {
  const rate = attendance?.attendanceRate ?? 0;
  return (
    <TabPage>
      {children.length > 1 && (
        <>
          <SectionTitle>Con em</SectionTitle>
          <ChildPicker children={children} activeChild={activeChild} switchChild={switchChild} />
        </>
      )}
      <SectionTitle>Học kỳ</SectionTitle>
      <SemPicker semesters={semesters} activeSem={activeSem} switchSem={switchSem} />

      {attendance ? (
        <>
          <View style={p.statsRow}>
            <StatCard label="Tổng buổi" value={attendance.totalSessions} accent="#7c3aed" />
            <View style={{ width: 8 }} />
            <StatCard label="Có mặt"    value={attendance.presentCount}  accent="#16a34a" />
            <View style={{ width: 8 }} />
            <StatCard label="Vắng"      value={attendance.absentCount}   accent="#dc2626" />
            <View style={{ width: 8 }} />
            <StatCard label="Muộn"      value={attendance.lateCount}     accent="#d97706" />
          </View>

          <Card style={{ marginBottom: 14 }}>
            <View style={p.progressHead}>
              <Text style={p.progressLabel}>Tỷ lệ chuyên cần</Text>
              <Text style={[p.progressPct, { color: rate >= 80 ? '#16a34a' : '#dc2626' }]}>{rate.toFixed(1)}%</Text>
            </View>
            <View style={p.progressBg}>
              <View style={[p.progressFill, {
                width: `${Math.min(rate, 100)}%` as any,
                backgroundColor: rate >= 80 ? '#16a34a' : '#dc2626',
              }]} />
            </View>
          </Card>

          {/* absenceDates — các buổi vắng/muộn */}
          {attendance.absenceDates?.length > 0 && (
            <>
              <SectionTitle>Lịch sử vắng / muộn ({attendance.absenceDates.length} lần)</SectionTitle>
              {attendance.absenceDates.map((r: any, i: number) => (
                <View key={i} style={p.attRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={p.attDate}>
                      {r.sessionDate ? new Date(r.sessionDate).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }) : ''}
                    </Text>
                    <Text style={p.attSub}>{r.subjectName}</Text>
                  </View>
                  <Badge
                    text={r.status === 'Absent' ? 'Vắng' : r.status === 'Late' ? 'Muộn' : r.status === 'Excused' ? 'Có phép' : r.status}
                    color={r.status === 'Absent' ? '#dc2626' : r.status === 'Late' ? '#d97706' : '#2563eb'}
                    bg={r.status === 'Absent' ? '#fef2f2' : r.status === 'Late' ? '#fffbeb' : '#eff6ff'}
                  />
                </View>
              ))}
            </>
          )}
          {!attendance.absenceDates?.length && (
            <EmptyView icon="✅" text={`${activeChild?.fullName?.split(' ').pop() ?? 'Con'} không có buổi vắng nào trong học kỳ này`} />
          )}
        </>
      ) : <EmptyView icon="📅" text="Chưa có dữ liệu điểm danh" />}
    </TabPage>
  );
}

/* ── Notifications ──────────────────────────────────────────── */
function NotifTab({ notifications, refreshing, onRefresh }: any) {
  const unread = notifications.filter((n: any) => !n.isRead).length;
  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      <SectionTitle>Thông báo{unread > 0 ? ` · ${unread} chưa đọc` : ''}</SectionTitle>
      {!notifications.length
        ? <EmptyView icon="🔕" text="Chưa có thông báo nào" />
        : notifications.map((n: any, i: number) => <NotifItem key={i} item={n} />)
      }
    </TabPage>
  );
}

const p = StyleSheet.create({
  childBtn:       { alignItems: 'center', marginRight: 18, paddingVertical: 4, minWidth: 60 },
  childAvatar:    { width: 46, height: 46, borderRadius: 14, backgroundColor: '#f5f3ff', alignItems: 'center', justifyContent: 'center', marginBottom: 5 },
  childAvatarTxt: { fontSize: 18, fontWeight: '800', color: '#7c3aed' },
  childLabel:     { fontSize: 11, fontWeight: '700', color: '#64748b', textAlign: 'center' },
  childClass:     { fontSize: 10, color: '#94a3b8', textAlign: 'center' },

  bigAvatar:      { width: 56, height: 56, borderRadius: 16, backgroundColor: '#7c3aed', alignItems: 'center', justifyContent: 'center' },
  bigAvatarTxt:   { fontSize: 22, fontWeight: '800', color: '#fff' },
  childFullName:  { fontSize: 16, fontWeight: '800', color: '#1e293b' },
  childClassName: { fontSize: 12, color: '#7c3aed', fontWeight: '600', marginTop: 2 },
  childCode:      { fontSize: 11, color: '#94a3b8', marginTop: 1 },

  statsRow:     { flexDirection: 'row', marginBottom: 12 },
  progressHead: { flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 },
  progressLabel:{ fontSize: 13, fontWeight: '600', color: '#1e293b' },
  progressPct:  { fontSize: 14, fontWeight: '800' },
  progressBg:   { height: 8, backgroundColor: '#f1f5f9', borderRadius: 4, overflow: 'hidden' },
  progressFill: { height: '100%', borderRadius: 4 },

  semBtn:  { paddingHorizontal: 16, paddingVertical: 9, borderRadius: 20, backgroundColor: '#f1f5f9', marginRight: 8 },
  semText: { fontSize: 13, fontWeight: '600', color: '#64748b' },

  gpaLabel: { fontSize: 11, color: '#94a3b8', fontWeight: '600', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 },
  gpaValue: { fontSize: 40, fontWeight: '900' },

  subRow:    { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  subName:   { fontSize: 14, fontWeight: '700', color: '#1e293b', marginBottom: 2 },
  subRemark: { fontSize: 11, color: '#94a3b8' },

  attRow:  { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center', shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  attDate: { fontSize: 13, fontWeight: '700', color: '#1e293b' },
  attSub:  { fontSize: 11, color: '#94a3b8', marginTop: 2 },
});
