import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
  Alert, TextInput, Modal,
} from 'react-native';
import QRCode from 'react-native-qrcode-svg';
import AppShell from '../../components/AppShell';
import BottomSheet, {
  FieldLabel, FieldInput, FieldSelect, ErrorMsg,
} from '../../components/BottomSheet';
import {
  LoadingView, EmptyView, SectionTitle, StatCard,
  TabPage, Card, ScorePill, NotifItem, Badge,
} from '../../components/ui';
import { teacherApi, commonApi } from '../../api/client';

/* ═══════════════════════════════════════════════════════════ */
export default function TeacherHomeScreen() {
  const [classes,          setClasses]          = useState<any[]>([]);
  const [subjectAssignments, setSubjectAssignments] = useState<any[]>([]);
  const [gradeCategories,  setGradeCategories]  = useState<any[]>([]);
  const [academicYears,    setAcademicYears]    = useState<any[]>([]);
  const [activeYear,       setActiveYear]       = useState<any>(null);
  const [notifications,    setNotifications]    = useState<any[]>([]);
  const [homeworkList,     setHomeworkList]     = useState<any[]>([]);
  const [loading,          setLoading]          = useState(true);
  const [refreshing,       setRefreshing]       = useState(false);

  const loadAll = useCallback(async () => {
    try {
      const [yearRes, notifRes, saRes, catRes, hwRes] = await Promise.all([
        commonApi.academicYears(),
        commonApi.notifications(),
        teacherApi.subjectAssignments(),
        teacherApi.gradeCategories(),
        teacherApi.homework(),
      ]);
      const years = yearRes.data ?? [];
      setAcademicYears(years);
      setNotifications(notifRes.data?.items ?? notifRes.data ?? []);
      setSubjectAssignments(saRes.data ?? []);
      setGradeCategories(catRes.data ?? []);
      setHomeworkList(hwRes.data ?? []);

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

  const totalStudents = classes.reduce((s: number, c: any) => s + (c.enrolledCount ?? 0), 0);

  return (
    <AppShell accentColor="#d97706" roleLabel="Giáo viên" tabs={[
      { key: 'home',    label: 'Tổng quan', icon: '🏠',
        content: <DashboardTab classes={classes} totalStudents={totalStudents} refreshing={refreshing} onRefresh={onRefresh} /> },
      { key: 'classes', label: 'Lớp & ĐD',  icon: '🏫',
        content: <ClassesTab classes={classes} academicYears={academicYears} activeYear={activeYear}
                             setActiveYear={setActiveYear} setClasses={setClasses} /> },
      { key: 'grades',  label: 'Sổ điểm',  icon: '📝',
        content: <GradeBookTab subjectAssignments={subjectAssignments} gradeCategories={gradeCategories} /> },
      { key: 'hw',      label: 'Bài tập',   icon: '📖',
        content: <HomeworkTab homeworkList={homeworkList} /> },
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
        <StatCard label="Lớp đang dạy"  value={classes.length}  accent="#d97706" />
        <View style={{ width: 10 }} />
        <StatCard label="Tổng học sinh" value={totalStudents}    accent="#2563eb" />
      </View>
      <SectionTitle>Lớp học</SectionTitle>
      {classes.length === 0 ? <EmptyView icon="🏫" text="Chưa có lớp nào" />
        : classes.map((c: any) => <ClassCard key={c.classId} cls={c} />)}
    </TabPage>
  );
}

/* ── Classes & Attendance ───────────────────────────────────── */
function ClassesTab({ classes, academicYears, activeYear, setActiveYear, setClasses }: any) {
  const [selected,   setSelected]   = useState<any>(null);
  const [report,     setReport]     = useState<any>(null);
  const [schedules,  setSchedules]  = useState<any[]>([]);
  const [loading,    setLoading]    = useState(false);

  // QR modal
  const [qrSessionId, setQrSessionId] = useState<number | null>(null);

  // Create session modal
  const [showCreate, setShowCreate] = useState(false);
  const [csSchedule, setCsSchedule] = useState('');
  const [csTopic,    setCsTopic]    = useState('');
  const [csSubmit,   setCsSubmit]   = useState(false);
  const [csError,    setCsError]    = useState('');

  // Update attendance modal
  const [showUpdate,  setShowUpdate]  = useState(false);
  const [updateRecord, setUpdateRecord] = useState<any>(null);
  const [updateStatus, setUpdateStatus] = useState('');
  const [updateNote,   setUpdateNote]   = useState('');
  const [updateSubmit, setUpdateSubmit] = useState(false);

  const openClass = async (cls: any) => {
    setSelected(cls); setReport(null); setLoading(true);
    try {
      const [rRes, sRes] = await Promise.all([
        teacherApi.attendanceSessions(cls.classId),
        // find schedules by getting subject assignment for this class
        // we pass classId=0 as placeholder since schedules endpoint needs subjectAssignmentId
        Promise.resolve({ data: [] }),
      ]);
      setReport(rRes.data);
      // Load schedules for subject assignments of this class
      // This would need more context; for now show create button
    } catch {}
    finally { setLoading(false); }
  };

  const openSchedules = async (sa: any) => {
    try {
      const res = await teacherApi.schedules(sa.id);
      setSchedules(res.data ?? []);
      setShowCreate(true);
    } catch {}
  };

  const createSession = async () => {
    if (!csSchedule) { setCsError('Vui lòng chọn tiết học'); return; }
    setCsSubmit(true); setCsError('');
    try {
      const today = new Date();
      const dateStr = `${today.getFullYear()}-${String(today.getMonth()+1).padStart(2,'0')}-${String(today.getDate()).padStart(2,'0')}`;
      const res = await teacherApi.createSession({
        scheduleId: parseInt(csSchedule),
        sessionDate: dateStr,
        qrExpiryMinutes: 10,
        topicNote: csTopic || null,
      });
      if (res.data) {
        setShowCreate(false); setCsSchedule(''); setCsTopic('');
        if (selected) openClass(selected);
        // Mở QR ngay sau khi tạo
        if (res.data.sessionId) setQrSessionId(res.data.sessionId);
      }
    } catch (e: any) {
      setCsError(e?.response?.data?.message ?? 'Có lỗi xảy ra');
    }
    finally { setCsSubmit(false); }
  };

  const openUpdateModal = (record: any) => {
    setUpdateRecord(record);
    setUpdateStatus(record.status);
    setUpdateNote(record.note ?? '');
    setShowUpdate(true);
  };

  const submitUpdate = async () => {
    if (!updateRecord || !report) return;
    setUpdateSubmit(true);
    try {
      await teacherApi.updateAttendance(report.sessionId ?? 0, {
        studentId: updateRecord.studentId,
        status: updateStatus,
        note: updateNote || null,
      });
      Alert.alert('✅ Đã cập nhật');
      setShowUpdate(false);
      if (selected) openClass(selected);
    } catch (e: any) {
      Alert.alert('Lỗi', e?.response?.data?.message ?? 'Có lỗi xảy ra');
    }
    finally { setUpdateSubmit(false); }
  };

  const switchYear = async (year: any) => {
    setActiveYear(year); setSelected(null);
    try { const r = await teacherApi.classes(year.academicYearId); setClasses(r.data ?? []); }
    catch {}
  };

  if (selected) {
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
        {!loading && (
          <>
            {report && (
              <View style={t.statsRow}>
                <StatCard label="Có mặt"  value={report.presentCount ?? 0} accent="#16a34a" />
                <View style={{ width: 8 }} />
                <StatCard label="Vắng"    value={report.absentCount  ?? 0} accent="#dc2626" />
                <View style={{ width: 8 }} />
                <StatCard label="Muộn"    value={report.lateCount    ?? 0} accent="#d97706" />
              </View>
            )}

            <View style={{ flexDirection: 'row', gap: 10, marginBottom: 14 }}>
              <TouchableOpacity style={[t.actionBtn, { backgroundColor: '#16a34a', flex: 1 }]}
                onPress={() => { setSchedules([]); setShowCreate(true); }}>
                <Text style={t.actionBtnText}>＋ Tạo điểm danh</Text>
              </TouchableOpacity>
              {report?.sessionId && (
                <TouchableOpacity style={[t.actionBtn, { backgroundColor: '#2563eb', flex: 1 }]}
                  onPress={() => setQrSessionId(report.sessionId)}>
                  <Text style={t.actionBtnText}>📷 Xem mã QR</Text>
                </TouchableOpacity>
              )}
            </View>
            {qrSessionId && <QRModal sessionId={qrSessionId} onClose={() => setQrSessionId(null)} />}

            <SectionTitle>Điểm danh hôm nay</SectionTitle>
            {!report?.records?.length
              ? <EmptyView icon="📋" text="Chưa có phiên điểm danh hôm nay" />
              : report.records.map((r: any, i: number) => (
                <TouchableOpacity key={i} style={t.attRow} onPress={() => openUpdateModal(r)} activeOpacity={0.8}>
                  <View style={t.attAvatar}>
                    <Text style={t.attAvatarText}>{(r.studentName ?? '?')[0]}</Text>
                  </View>
                  <View style={{ flex: 1 }}>
                    <Text style={t.attName}>{r.studentName}</Text>
                    <Text style={t.attCode}>{r.studentCode}</Text>
                  </View>
                  <Badge
                    text={r.status === 'Present' ? 'Có mặt' : r.status === 'Absent' ? 'Vắng' : 'Muộn'}
                    color={r.status === 'Present' ? '#16a34a' : r.status === 'Absent' ? '#dc2626' : '#d97706'}
                    bg={r.status === 'Present' ? '#f0fdf4' : r.status === 'Absent' ? '#fef2f2' : '#fffbeb'}
                  />
                  <Text style={{ fontSize: 14, color: '#94a3b8', marginLeft: 6 }}>›</Text>
                </TouchableOpacity>
              ))
            }
          </>
        )}

        {/* Modal tạo điểm danh */}
        <BottomSheet
          visible={showCreate} title="Tạo phiên điểm danh"
          onClose={() => setShowCreate(false)}
          onSubmit={createSession} submitting={csSubmit}
          submitLabel="Tạo điểm danh" accent="#16a34a"
        >
          {schedules.length > 0 && (
            <>
              <FieldLabel>Chọn tiết học</FieldLabel>
              <FieldSelect
                value={csSchedule}
                onChange={setCsSchedule}
                options={schedules.map((s: any) => ({
                  label: `${s.dayName} - T${s.periodStart}-${s.periodEnd} (${s.startTime})`,
                  value: String(s.scheduleId),
                }))}
              />
            </>
          )}
          {schedules.length === 0 && (
            <Text style={{ color: '#94a3b8', fontSize: 13, marginBottom: 8 }}>
              Phiên điểm danh sẽ được tạo cho lớp {selected?.className} hôm nay.{'\n'}
              Vui lòng đảm bảo lớp đã có lịch học.
            </Text>
          )}
          <FieldLabel>Chủ đề / Ghi chú (tùy chọn)</FieldLabel>
          <FieldInput value={csTopic} onChange={setCsTopic} placeholder="VD: Ôn tập chương 3" />
          <ErrorMsg text={csError} />
        </BottomSheet>

        {/* Modal cập nhật điểm danh */}
        <BottomSheet
          visible={showUpdate}
          title={`Cập nhật: ${updateRecord?.studentName ?? ''}`}
          onClose={() => setShowUpdate(false)}
          onSubmit={submitUpdate} submitting={updateSubmit}
          submitLabel="Lưu thay đổi" accent="#d97706"
        >
          <FieldLabel>Trạng thái</FieldLabel>
          <FieldSelect
            value={updateStatus}
            onChange={setUpdateStatus}
            options={[
              { label: '✅ Có mặt', value: 'Present' },
              { label: '❌ Vắng',   value: 'Absent'  },
              { label: '⏰ Muộn',   value: 'Late'    },
              { label: '📋 Có phép',value: 'Excused' },
            ]}
          />
          <FieldLabel>Ghi chú</FieldLabel>
          <FieldInput value={updateNote} onChange={setUpdateNote} placeholder="Lý do vắng, ghi chú..." multiline />
        </BottomSheet>
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
              <TouchableOpacity key={y.academicYearId}
                style={[t.semBtn, activeYear?.academicYearId === y.academicYearId && { backgroundColor: '#d97706' }]}
                onPress={() => switchYear(y)}>
                <Text style={[t.semText, activeYear?.academicYearId === y.academicYearId && { color: '#fff' }]}>
                  {y.yearName}
                </Text>
              </TouchableOpacity>
            ))}
          </ScrollView>
        </>
      )}
      <SectionTitle>Lớp đang dạy</SectionTitle>
      {classes.length === 0 ? <EmptyView icon="🏫" text="Chưa có lớp nào" />
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
function GradeBookTab({ subjectAssignments, gradeCategories }: any) {
  const [selected,   setSelected]   = useState<any>(null);
  const [gbData,     setGbData]     = useState<any>(null);
  const [loading,    setLoading]    = useState(false);
  const [calculating, setCalc]      = useState(false);

  // Enter score modal
  const [showScore,  setShowScore]  = useState(false);
  const [scoreRow,   setScoreRow]   = useState<any>(null);
  const [scoreValue, setScoreValue] = useState('');
  const [scoreCat,   setScoreCat]   = useState('');
  const [scoreOrder, setScoreOrder] = useState('1');
  const [scoreNote,  setScoreNote]  = useState('');
  const [scoreSubmit,setScoreSubmit]= useState(false);
  const [scoreError, setScoreError] = useState('');

  const openGradeBook = async (sa: any) => {
    setSelected(sa); setLoading(true);
    try { const r = await teacherApi.gradeBook(sa.id); setGbData(r.data); }
    catch {} finally { setLoading(false); }
  };

  const openScoreModal = (row: any) => {
    setScoreRow(row);
    setScoreValue('');
    setScoreNote('');
    setScoreCat(gradeCategories[0]?.id?.toString() ?? '');
    setScoreOrder('1');
    setShowScore(true);
  };

  const submitScore = async () => {
    const val = parseFloat(scoreValue.replace(',', '.'));
    if (isNaN(val) || val < 0 || val > 10) { setScoreError('Điểm phải từ 0 đến 10'); return; }
    if (!scoreCat) { setScoreError('Vui lòng chọn loại điểm'); return; }
    setScoreSubmit(true); setScoreError('');
    try {
      await teacherApi.enterScore({
        studentId:           scoreRow.studentId,
        subjectAssignmentId: selected.id,
        gradeCategoryId:     parseInt(scoreCat),
        entryOrder:          parseInt(scoreOrder),
        score:               val,
        note:                scoreNote || null,
      });
      Alert.alert('✅ Thành công', `Đã nhập điểm ${val} cho ${scoreRow.studentName}`);
      setShowScore(false);
      openGradeBook(selected);
    } catch (e: any) {
      setScoreError(e?.response?.data?.message ?? 'Có lỗi xảy ra');
    }
    finally { setScoreSubmit(false); }
  };

  const calcAll = async () => {
    if (!selected) return;
    setCalc(true);
    try {
      const r = await teacherApi.calculateAverages(selected.id);
      Alert.alert('✅ Tính xong', `Đã tính ĐTBm cho ${r.data?.updated ?? '?'} học sinh`);
      openGradeBook(selected);
    } catch {}
    finally { setCalc(false); }
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

        {/* Actions */}
        <View style={{ flexDirection: 'row', gap: 10, marginBottom: 14 }}>
          <TouchableOpacity
            style={[t.actionBtn, { backgroundColor: '#2563eb', flex: 1 }, calculating && { opacity: 0.6 }]}
            onPress={calcAll} disabled={calculating}
          >
            <Text style={t.actionBtnText}>{calculating ? 'Đang tính...' : '⟳ Tính ĐTBm tất cả'}</Text>
          </TouchableOpacity>
        </View>

        <SectionTitle>Sổ điểm ({gbData?.rows?.length ?? 0} học sinh) — nhấn để nhập điểm</SectionTitle>
        {!gbData?.rows?.length
          ? <EmptyView text="Chưa có dữ liệu điểm" />
          : gbData.rows.map((row: any, i: number) => (
            <TouchableOpacity key={i} onPress={() => openScoreModal(row)} activeOpacity={0.8}>
              <View style={t.gbCard}>
                <View style={t.gbLeft}>
                  <Text style={t.gbName}>{row.studentName}</Text>
                  <Text style={t.gbCode}>{row.studentCode}</Text>
                </View>
                <View style={t.gbScores}>
                  {row.regularScores?.map((sc: number | null, j: number) => sc != null && (
                    <View key={j} style={t.gbScoreItem}>
                      <Text style={t.gbScoreLabel}>TX{j+1}</Text>
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
                  {row.averageScore != null
                    ? <View style={t.gbScoreItem}>
                        <Text style={[t.gbScoreLabel, { color: '#d97706', fontWeight: '800' }]}>TB</Text>
                        <ScorePill score={row.averageScore} />
                      </View>
                    : <View style={t.gbAddBtn}><Text style={t.gbAddBtnText}>+ Nhập</Text></View>
                  }
                </View>
              </View>
            </TouchableOpacity>
          ))
        }

        {/* Modal nhập điểm */}
        <BottomSheet
          visible={showScore}
          title={`Nhập điểm — ${scoreRow?.studentName ?? ''}`}
          onClose={() => setShowScore(false)}
          onSubmit={submitScore} submitting={scoreSubmit}
          submitLabel="Lưu điểm" accent="#2563eb"
        >
          <FieldLabel>Loại điểm</FieldLabel>
          <FieldSelect
            value={scoreCat}
            onChange={setScoreCat}
            options={gradeCategories.map((c: any) => ({
              label: `${c.categoryName} (HS ${c.coefficient})`,
              value: String(c.id),
            }))}
          />
          <FieldLabel>Số điểm (0 – 10)</FieldLabel>
          <FieldInput
            value={scoreValue}
            onChange={setScoreValue}
            placeholder="VD: 8.5"
            keyboardType="decimal-pad"
          />
          <FieldLabel>Thứ tự đầu điểm</FieldLabel>
          <FieldSelect
            value={scoreOrder}
            onChange={setScoreOrder}
            options={[1,2,3,4,5].map(n => ({ label: `Đầu ${n}`, value: String(n) }))}
          />
          <FieldLabel>Ghi chú (tùy chọn)</FieldLabel>
          <FieldInput value={scoreNote} onChange={setScoreNote} placeholder="Ghi chú..." />
          <ErrorMsg text={scoreError} />
        </BottomSheet>
      </TabPage>
    );
  }

  return (
    <TabPage>
      <SectionTitle>Chọn lớp - môn để nhập điểm</SectionTitle>
      {!subjectAssignments.length
        ? <EmptyView icon="📝" text="Chưa có phân công môn học" />
        : subjectAssignments.map((sa: any) => (
          <TouchableOpacity key={sa.id} onPress={() => openGradeBook(sa)} activeOpacity={0.8}>
            <Card style={{ flexDirection: 'row', alignItems: 'center' }}>
              <View style={t.saIcon}><Text style={{ fontSize: 20 }}>📝</Text></View>
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
      {!notifications.length ? <EmptyView icon="🔕" text="Chưa có thông báo" />
        : notifications.map((n: any, i: number) => <NotifItem key={i} item={n} />)}
    </TabPage>
  );
}

/* ── Shared ─────────────────────────────────────────────────── */
function ClassCard({ cls, showArrow }: { cls: any; showArrow?: boolean }) {
  return (
    <Card style={{ flexDirection: 'row', alignItems: 'center' }}>
      <View style={t.classIcon}><Text style={{ fontSize: 22 }}>🏫</Text></View>
      <View style={{ flex: 1 }}>
        <Text style={t.className}>{cls.className}</Text>
        <Text style={t.classSub}>{cls.gradeName} · {cls.enrolledCount ?? 0} học sinh</Text>
      </View>
      {showArrow && <Text style={{ fontSize: 20, color: '#94a3b8' }}>›</Text>}
    </Card>
  );
}

/* ── QR Code Modal ─────────────────────────────────────────── */
function QRModal({ sessionId, onClose }: { sessionId: number; onClose: () => void }) {
  const [session,  setSession]  = useState<any>(null);
  const [loading,  setLoading]  = useState(true);
  const [notified, setNotified] = useState(false);

  useEffect(() => {
    teacherApi.getSession(sessionId)
      .then(r => setSession(r.data))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [sessionId]);

  const notify = async () => {
    try {
      await teacherApi.notifyAbsent(sessionId);
      Alert.alert('✅ Đã gửi', 'Thông báo vắng học đã được gửi tới phụ huynh');
      setNotified(true);
    } catch (e: any) {
      Alert.alert('Lỗi', e?.response?.data?.message ?? 'Có lỗi xảy ra');
    }
  };

  return (
    <Modal visible animationType="slide" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: '#0b1628', alignItems: 'center', justifyContent: 'center', padding: 24 }}>
        <TouchableOpacity style={{ position: 'absolute', top: 50, right: 20 }} onPress={onClose}>
          <Text style={{ color: '#fff', fontSize: 16, fontWeight: '700' }}>✕ Đóng</Text>
        </TouchableOpacity>
        <Text style={{ color: '#c9a84c', fontSize: 13, fontWeight: '800', letterSpacing: 1, marginBottom: 8, textTransform: 'uppercase' }}>
          Mã QR Điểm Danh
        </Text>
        {loading ? <LoadingView /> : session ? (
          <>
            <View style={{ backgroundColor: '#fff', padding: 20, borderRadius: 20, marginBottom: 20 }}>
              <QRCode
                value={session.qrToken ?? 'invalid'}
                size={220}
                color="#0b1628"
                backgroundColor="#fff"
              />
            </View>
            <Text style={{ color: '#fff', fontSize: 16, fontWeight: '800', marginBottom: 4 }}>
              {session.className ?? ''}
            </Text>
            <Text style={{ color: 'rgba(255,255,255,0.6)', fontSize: 12, marginBottom: 6 }}>
              {session.isQRExpired ? '❌ Mã QR đã hết hạn' : `✅ Hết hạn lúc ${new Date(session.qrExpiresAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}`}
            </Text>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8, marginBottom: 24 }}>
              <View style={{ width: 8, height: 8, borderRadius: 4, backgroundColor: session.sessionStatus === 'Open' ? '#16a34a' : '#dc2626' }} />
              <Text style={{ color: 'rgba(255,255,255,0.7)', fontSize: 12 }}>
                {session.sessionStatus === 'Open' ? 'Phiên đang mở' : 'Phiên đã đóng'}
              </Text>
            </View>
            <TouchableOpacity
              style={{ backgroundColor: notified ? '#475569' : '#d97706', borderRadius: 12, paddingVertical: 12, paddingHorizontal: 28 }}
              onPress={notify}
              disabled={notified}
            >
              <Text style={{ color: '#fff', fontWeight: '800', fontSize: 14 }}>
                {notified ? '✅ Đã thông báo' : '📱 Thông báo phụ huynh HS vắng'}
              </Text>
            </TouchableOpacity>
          </>
        ) : (
          <Text style={{ color: '#ef4444' }}>Không tải được phiên điểm danh</Text>
        )}
      </View>
    </Modal>
  );
}

/* ── Homework Tab ───────────────────────────────────────────── */
function HomeworkTab({ homeworkList }: any) {
  return (
    <TabPage>
      <SectionTitle>Bài tập đã tạo ({homeworkList.length})</SectionTitle>
      {!homeworkList.length ? (
        <EmptyView icon="📖" text="Chưa có bài tập nào. Tạo bài tập trên web để quản lý." />
      ) : (
        homeworkList.map((a: any, i: number) => (
          <View key={i} style={t.hwCard}>
            <View style={{ flex: 1 }}>
              <Text style={t.hwTitle}>{a.title}</Text>
              <Text style={t.hwMeta}>{a.subjectName} · {a.className}</Text>
              {a.dueDate && (
                <Text style={t.hwDue}>
                  📅 {new Date(a.dueDate).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                </Text>
              )}
            </View>
            <View style={{ alignItems: 'flex-end', gap: 6 }}>
              <Badge
                text={a.isPublished ? 'Đã đăng' : 'Nháp'}
                color={a.isPublished ? '#16a34a' : '#64748b'}
                bg={a.isPublished ? '#f0fdf4' : '#f1f5f9'}
              />
              {a.submittedCount != null && (
                <Text style={{ fontSize: 11, color: '#94a3b8' }}>
                  {a.submittedCount}/{a.totalStudents ?? '?'} nộp
                </Text>
              )}
            </View>
          </View>
        ))
      )}
    </TabPage>
  );
}

function greetingTime() {
  const h = new Date().getHours();
  if (h < 12) return 'sáng'; if (h < 18) return 'chiều'; return 'tối';
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

  actionBtn:    { paddingVertical: 12, borderRadius: 12, alignItems: 'center' },
  actionBtnText:{ fontSize: 13, fontWeight: '800', color: '#fff' },

  classIcon:    { width: 48, height: 48, borderRadius: 14, backgroundColor: '#fef3c7', alignItems: 'center', justifyContent: 'center', marginRight: 12 },
  className:    { fontSize: 15, fontWeight: '700', color: '#1e293b' },
  classSub:     { fontSize: 11, color: '#94a3b8', marginTop: 2 },

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
  gbScores:    { flexDirection: 'row', flexWrap: 'wrap', gap: 8, alignItems: 'center' },
  gbScoreItem: { alignItems: 'center', gap: 3 },
  gbScoreLabel:{ fontSize: 9, fontWeight: '700', color: '#94a3b8', textTransform: 'uppercase' },
  gbAddBtn:    { backgroundColor: '#eff6ff', borderRadius: 8, paddingHorizontal: 10, paddingVertical: 5 },
  gbAddBtnText:{ fontSize: 12, fontWeight: '700', color: '#2563eb' },

  hwCard: { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'flex-start',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  hwTitle:{ fontSize: 14, fontWeight: '700', color: '#1e293b', marginBottom: 4 },
  hwMeta: { fontSize: 11, color: '#94a3b8', marginBottom: 3 },
  hwDue:  { fontSize: 11, color: '#d97706', fontWeight: '600' },
});
