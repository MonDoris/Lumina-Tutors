import React, { useEffect, useState, useCallback, useRef } from 'react';
import {
  View, Text, StyleSheet, ScrollView, TouchableOpacity,
  TextInput, Alert, KeyboardAvoidingView, Platform, FlatList, Modal,
} from 'react-native';
import { CameraView, useCameraPermissions } from 'expo-camera';
import AppShell from '../../components/AppShell';
import {
  LoadingView, EmptyView, SectionTitle, StatCard,
  TabPage, Card, ScorePill, NotifItem, Badge, scoreColor,
} from '../../components/ui';
import { studentApi, commonApi, onlineApi, virtualLabApi } from '../../api/client';
import ClassroomWebViewScreen from '../shared/ClassroomWebViewScreen';
import VirtualLabWebViewScreen from '../shared/VirtualLabWebViewScreen';

/* ═══════════════════════════════════════════════════════════ */
/*  STUDENT — 5 tabs                                          */
/* ═══════════════════════════════════════════════════════════ */

export default function StudentHomeScreen() {
  const [semesters,     setSemesters]     = useState<any[]>([]);
  const [activeSem,     setActiveSem]     = useState<any>(null);
  const [grades,        setGrades]        = useState<any>(null);
  const [attendance,    setAttendance]    = useState<any>(null);
  const [notifications, setNotifications] = useState<any[]>([]);
  const [courses,       setCourses]       = useState<any[]>([]);
  const [loading,       setLoading]       = useState(true);
  const [refreshing,    setRefreshing]    = useState(false);

  const loadAll = useCallback(async () => {
    try {
      const [semRes, notifRes, courseRes] = await Promise.all([
        commonApi.semesters(),
        commonApi.notifications(),
        studentApi.courses(),
      ]);
      const sems  = semRes.data ?? [];
      setSemesters(sems);
      setNotifications(notifRes.data?.items ?? notifRes.data ?? []);
      setCourses(courseRes.data ?? []);
      const sem = sems.find((s: any) => s.isActive) ?? sems[0];
      if (sem) {
        setActiveSem(sem);
        const [g, a] = await Promise.all([
          studentApi.grades(sem.semesterId),
          studentApi.attendance(sem.semesterId),
        ]);
        setGrades(g.data); setAttendance(a.data);
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
        content: <DashboardTab {...shared} refreshing={refreshing} onRefresh={onRefresh} /> },
      { key: 'grades', label: 'Điểm số',   icon: '📊',
        content: <GradesTab {...shared} /> },
      { key: 'att',    label: 'Điểm danh', icon: '📅',
        content: <AttendanceTab {...shared} /> },
      { key: 'hw',     label: 'Bài tập',   icon: '📖',
        content: <HomeworkTab courses={courses} /> },
      { key: 'online', label: 'Học online', icon: '🖥️',
        content: <OnlineLearningTab /> },
      { key: 'lab',    label: 'Lab 3D',    icon: '🔬',
        content: <VirtualLabTab /> },
      { key: 'ai',     label: 'Gia sư AI', icon: '🤖',
        content: <AiTutorTab /> },
    ]} />
  );
}

/* ── Dashboard ─────────────────────────────────────────────── */
function DashboardTab({ grades, attendance, activeSem, semesters, setActiveSem, setGrades, setAttendance, refreshing, onRefresh }: any) {
  const [showQR, setShowQR] = useState(false);
  const rate = attendance?.attendanceRate ?? 0;

  if (showQR) {
    return <QRScanScreen onClose={() => setShowQR(false)} />;
  }

  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      {/* Hero GPA */}
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
            <Text style={s.heroStatLabel}>Vắng mặt</Text>
          </View>
          <View style={s.heroStat}>
            <Text style={[s.heroStatVal, { color: rate >= 80 ? '#16a34a' : '#dc2626' }]}>
              {rate.toFixed(0)}%
            </Text>
            <Text style={s.heroStatLabel}>Chuyên cần</Text>
          </View>
        </View>
      </View>

      {/* QR Scan button */}
      <TouchableOpacity style={s.qrBtn} onPress={() => setShowQR(true)}>
        <Text style={s.qrBtnIcon}>📷</Text>
        <View style={{ flex: 1 }}>
          <Text style={s.qrBtnTitle}>Quét QR Điểm danh</Text>
          <Text style={s.qrBtnSub}>Dùng camera để quét mã QR của giáo viên</Text>
        </View>
        <Text style={{ fontSize: 18, color: '#fff', opacity: 0.7 }}>›</Text>
      </TouchableOpacity>

      {/* Subject previews */}
      {grades?.subjectAverages?.length > 0 && (
        <>
          <SectionTitle>Điểm các môn</SectionTitle>
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

/* ── QR Scan ────────────────────────────────────────────────── */
function QRScanScreen({ onClose }: { onClose: () => void }) {
  const [permission, requestPermission] = useCameraPermissions();
  const [scanned, setScanned] = useState(false);
  const [result,  setResult]  = useState<any>(null);

  const onBarcode = async ({ data }: { data: string }) => {
    if (scanned) return;
    setScanned(true);
    try {
      const res = await studentApi.scanQR(data);
      setResult(res.data);
    } catch (e: any) {
      setResult({ success: false, message: e?.response?.data?.message ?? 'Mã QR không hợp lệ' });
    }
  };

  if (!permission) return <LoadingView />;
  if (!permission.granted) {
    return (
      <View style={s.qrScreen}>
        <Text style={{ color: '#fff', fontSize: 16, textAlign: 'center', marginBottom: 20 }}>
          Cần quyền truy cập camera để quét QR
        </Text>
        <TouchableOpacity style={s.qrPermBtn} onPress={requestPermission}>
          <Text style={{ color: '#fff', fontWeight: '700' }}>Cấp quyền Camera</Text>
        </TouchableOpacity>
        <TouchableOpacity style={[s.qrPermBtn, { backgroundColor: '#334155', marginTop: 10 }]} onPress={onClose}>
          <Text style={{ color: '#fff', fontWeight: '700' }}>Quay lại</Text>
        </TouchableOpacity>
      </View>
    );
  }

  if (result) {
    return (
      <View style={[s.qrScreen, { justifyContent: 'center', alignItems: 'center', padding: 30 }]}>
        <Text style={{ fontSize: 64, marginBottom: 16 }}>{result.success ? '✅' : '❌'}</Text>
        <Text style={{ color: '#fff', fontSize: 18, fontWeight: '800', textAlign: 'center', marginBottom: 8 }}>
          {result.success ? 'Điểm danh thành công!' : 'Thất bại'}
        </Text>
        <Text style={{ color: 'rgba(255,255,255,0.7)', textAlign: 'center', marginBottom: 30 }}>
          {result.message}
        </Text>
        {result.success && (
          <>
            <Text style={{ color: '#86efac', fontSize: 14 }}>Môn: {result.subjectName}</Text>
            <Text style={{ color: '#86efac', fontSize: 14, marginTop: 4 }}>Lớp: {result.className}</Text>
          </>
        )}
        <TouchableOpacity
          style={[s.qrPermBtn, { marginTop: 32, backgroundColor: result.success ? '#16a34a' : '#2563eb' }]}
          onPress={onClose}
        >
          <Text style={{ color: '#fff', fontWeight: '700' }}>Quay lại</Text>
        </TouchableOpacity>
      </View>
    );
  }

  return (
    <View style={{ flex: 1, backgroundColor: '#000' }}>
      <CameraView
        style={{ flex: 1 }}
        facing="back"
        onBarcodeScanned={onBarcode}
        barcodeScannerSettings={{ barcodeTypes: ['qr'] }}
      />
      <View style={s.qrOverlay}>
        <TouchableOpacity style={s.qrCloseBtn} onPress={onClose}>
          <Text style={{ color: '#fff', fontWeight: '700' }}>✕ Đóng</Text>
        </TouchableOpacity>
        <View style={s.qrFrame} />
        <Text style={s.qrHint}>Hướng camera vào mã QR của giáo viên</Text>
      </View>
    </View>
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
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
              <View>
                <Text style={s.cardSmLabel}>Điểm trung bình</Text>
                <Text style={[s.bigScore, { color: scoreColor(grades.semesterGpa) }]}>
                  {grades.semesterGpa?.toFixed(2) ?? '—'}
                </Text>
              </View>
              {grades.semesterRemark ? (
                <View style={{ backgroundColor: '#eff6ff', borderRadius: 10, padding: 10 }}>
                  <Text style={{ fontSize: 13, fontWeight: '800', color: '#2563eb', textAlign: 'center' }}>
                    {grades.semesterRemark}
                  </Text>
                </View>
              ) : null}
            </View>
          </Card>
          <SectionTitle>Điểm từng môn ({grades.subjectAverages?.length ?? 0} môn)</SectionTitle>
          {!grades.subjectAverages?.length ? <EmptyView text="Chưa có điểm" />
            : grades.subjectAverages.map((sub: any, i: number) => (
              <View key={i} style={s.subjectCard}>
                <View style={{ flex: 1 }}>
                  <Text style={s.subjectName}>{sub.subjectName}</Text>
                  {sub.remark ? <Text style={s.subjectRemark}>{sub.remark}</Text> : null}
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
          <View style={{ flexDirection: 'row', marginBottom: 12, gap: 8 }}>
            <StatCard label="Tổng buổi"  value={attendance.totalSessions} accent="#2563eb" />
            <StatCard label="Có mặt"     value={attendance.presentCount}  accent="#16a34a" />
            <StatCard label="Vắng"       value={attendance.absentCount}   accent="#dc2626" />
            <StatCard label="Muộn"       value={attendance.lateCount}     accent="#d97706" />
          </View>
          <Card style={{ marginBottom: 14 }}>
            <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 }}>
              <Text style={{ fontSize: 13, fontWeight: '600', color: '#1e293b' }}>Tỷ lệ chuyên cần</Text>
              <Text style={{ fontSize: 14, fontWeight: '800', color: rate >= 80 ? '#16a34a' : '#dc2626' }}>
                {rate.toFixed(1)}%
              </Text>
            </View>
            <View style={{ height: 8, backgroundColor: '#f1f5f9', borderRadius: 4, overflow: 'hidden' }}>
              <View style={{ height: '100%', borderRadius: 4, width: `${Math.min(rate, 100)}%` as any,
                backgroundColor: rate >= 80 ? '#16a34a' : '#dc2626' }} />
            </View>
            <Text style={{ fontSize: 11, color: '#94a3b8', marginTop: 8 }}>
              {rate >= 80 ? '✅ Đạt yêu cầu' : '⚠️ Dưới mức 80% — cần cải thiện'}
            </Text>
          </Card>
          {attendance.absenceDates?.length > 0 && (
            <>
              <SectionTitle>Lịch sử vắng / muộn ({attendance.absenceDates.length})</SectionTitle>
              {attendance.absenceDates.map((r: any, i: number) => (
                <View key={i} style={s.attRow}>
                  <View style={{ flex: 1 }}>
                    <Text style={s.attDate}>{r.sessionDate ? new Date(r.sessionDate).toLocaleDateString('vi-VN') : ''}</Text>
                    <Text style={s.attSub}>{r.subjectName}</Text>
                  </View>
                  <Badge
                    text={r.status === 'Absent' ? 'Vắng' : r.status === 'Late' ? 'Muộn' : 'Có phép'}
                    color={r.status === 'Absent' ? '#dc2626' : r.status === 'Late' ? '#d97706' : '#2563eb'}
                    bg={r.status === 'Absent' ? '#fef2f2' : r.status === 'Late' ? '#fffbeb' : '#eff6ff'}
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

/* ── Homework ───────────────────────────────────────────────── */
function HomeworkTab({ courses }: any) {
  const [selected,   setSelected]   = useState<any>(null);
  const [assignments, setAssignments] = useState<any[]>([]);
  const [loading,    setLoading]    = useState(false);

  const openCourse = async (c: any) => {
    setSelected(c); setLoading(true);
    try { const r = await studentApi.homework(c.subjectAssignmentId); setAssignments(r.data ?? []); }
    catch {} finally { setLoading(false); }
  };

  if (selected) {
    if (loading) return <LoadingView />;
    return (
      <TabPage>
        <TouchableOpacity style={s.back} onPress={() => setSelected(null)}>
          <Text style={s.backText}>← Danh sách môn</Text>
        </TouchableOpacity>
        <View style={s.courseHeader}>
          <Text style={s.courseHeaderName}>{selected.subjectName}</Text>
          <Text style={s.courseHeaderSub}>{selected.className} · GV: {selected.teacherName}</Text>
        </View>
        <SectionTitle>Bài tập ({assignments.length})</SectionTitle>
        {!assignments.length ? <EmptyView icon="📖" text="Chưa có bài tập" />
          : assignments.map((a: any, i: number) => (
            <View key={i} style={s.hwCard}>
              <View style={{ flex: 1 }}>
                <Text style={s.hwTitle}>{a.title}</Text>
                <Text style={s.hwSub}>{a.subjectName} · Tối đa {a.maxScore} điểm</Text>
                {a.dueDate && (
                  <Text style={[s.hwDue, new Date(a.dueDate) < new Date() && !a.mySubmission && { color: '#dc2626' }]}>
                    📅 {new Date(a.dueDate).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}
                  </Text>
                )}
              </View>
              <View style={{ alignItems: 'flex-end', gap: 6 }}>
                {a.mySubmission ? (
                  <>
                    <Badge text="Đã nộp" color="#16a34a" bg="#f0fdf4" />
                    {a.mySubmission.score != null && <ScorePill score={a.mySubmission.score} />}
                  </>
                ) : (
                  <Badge
                    text={a.dueDate && new Date(a.dueDate) < new Date() ? 'Quá hạn' : 'Chưa nộp'}
                    color={a.dueDate && new Date(a.dueDate) < new Date() ? '#dc2626' : '#d97706'}
                    bg={a.dueDate && new Date(a.dueDate) < new Date() ? '#fef2f2' : '#fffbeb'}
                  />
                )}
              </View>
            </View>
          ))
        }
      </TabPage>
    );
  }

  return (
    <TabPage>
      <SectionTitle>Các môn học ({courses.length} môn)</SectionTitle>
      {!courses.length ? <EmptyView icon="📖" text="Chưa có bài tập" />
        : courses.map((c: any) => (
          <TouchableOpacity key={c.subjectAssignmentId} onPress={() => openCourse(c)} activeOpacity={0.8}>
            <Card style={{ flexDirection: 'row', alignItems: 'center' }}>
              <View style={s.courseIcon}><Text style={{ fontSize: 22 }}>📖</Text></View>
              <View style={{ flex: 1 }}>
                <Text style={{ fontSize: 15, fontWeight: '700', color: '#1e293b' }}>{c.subjectName}</Text>
                <Text style={{ fontSize: 11, color: '#94a3b8', marginTop: 2 }}>GV: {c.teacherName}</Text>
              </View>
              <View style={{ alignItems: 'flex-end', gap: 4 }}>
                {c.pendingCount > 0 && <Badge text={`${c.pendingCount} chưa nộp`} color="#d97706" bg="#fffbeb" />}
                {c.overdueCount > 0 && <Badge text={`${c.overdueCount} quá hạn`} color="#dc2626" bg="#fef2f2" />}
              </View>
              <Text style={{ fontSize: 18, color: '#94a3b8', marginLeft: 8 }}>›</Text>
            </Card>
          </TouchableOpacity>
        ))
      }
    </TabPage>
  );
}

/* ── AI Tutor ───────────────────────────────────────────────── */
function AiTutorTab() {
  const [sessions,   setSessions]   = useState<any[]>([]);
  const [activeSession, setActiveSession] = useState<any>(null);
  const [messages,   setMessages]   = useState<any[]>([]);
  const [input,      setInput]      = useState('');
  const [loading,    setLoading]    = useState(true);
  const [sending,    setSending]    = useState(false);
  const flatRef = useRef<FlatList>(null);

  useEffect(() => {
    studentApi.aiSessions().then(r => { setSessions(r.data ?? []); setLoading(false); }).catch(() => setLoading(false));
  }, []);

  const openSession = async (sess: any) => {
    setActiveSession(sess); setLoading(true);
    try { const r = await studentApi.getMessages(sess.sessionId); setMessages(r.data ?? []); }
    catch {} finally { setLoading(false); }
  };

  const createSession = async () => {
    try {
      const r = await studentApi.createSession('Phiên học mới');
      setSessions(prev => [r.data, ...prev]);
      await openSession(r.data);
    } catch {}
  };

  const send = async () => {
    if (!input.trim() || sending) return;
    const text = input.trim();
    setInput('');
    setSending(true);
    const optimistic = { messageId: Date.now(), role: 'user', content: text, createdAt: new Date().toISOString() };
    setMessages(prev => [...prev, optimistic]);
    try {
      const r = await studentApi.sendMessage(activeSession.sessionId, text);
      setMessages(prev => [...prev, r.data]);
      setTimeout(() => flatRef.current?.scrollToEnd(), 100);
    } catch {
      Alert.alert('Lỗi', 'Không thể gửi tin nhắn');
    }
    finally { setSending(false); }
  };

  if (loading) return <LoadingView />;

  if (activeSession) {
    return (
      <View style={{ flex: 1, backgroundColor: '#f0f4f8' }}>
        <View style={s.chatHeader}>
          <TouchableOpacity onPress={() => setActiveSession(null)}>
            <Text style={s.chatBack}>←</Text>
          </TouchableOpacity>
          <View style={{ flex: 1, marginLeft: 10 }}>
            <Text style={s.chatTitle}>{activeSession.title}</Text>
            <Text style={s.chatSub}>Gia sư AI · Lumina Tutors</Text>
          </View>
          <View style={s.chatAiBadge}><Text style={{ fontSize: 11, color: '#7c3aed', fontWeight: '800' }}>AI</Text></View>
        </View>
        <FlatList
          ref={flatRef}
          data={messages}
          keyExtractor={(m, i) => String(m.messageId ?? i)}
          contentContainerStyle={{ padding: 16 }}
          onContentSizeChange={() => flatRef.current?.scrollToEnd()}
          renderItem={({ item: m }) => (
            <View style={[s.bubble, m.role === 'user' ? s.bubbleUser : s.bubbleAI]}>
              {m.role !== 'user' && <Text style={s.bubbleRoleLabel}>🤖 Gia sư AI</Text>}
              <Text style={[s.bubbleText, m.role !== 'user' && { color: '#1e293b' }]}>{m.content}</Text>
            </View>
          )}
          ListEmptyComponent={
            <View style={{ alignItems: 'center', marginTop: 40 }}>
              <Text style={{ fontSize: 40 }}>🤖</Text>
              <Text style={{ color: '#64748b', marginTop: 10, textAlign: 'center' }}>
                Xin chào! Tôi là Gia sư AI của Lumina Tutors.{'\n'}Hãy đặt câu hỏi học tập cho tôi!
              </Text>
            </View>
          }
        />
        <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : 'height'}>
          <View style={s.chatInput}>
            <TextInput
              style={s.chatInputBox}
              value={input}
              onChangeText={setInput}
              placeholder="Nhập câu hỏi..."
              placeholderTextColor="#94a3b8"
              multiline
              maxLength={500}
            />
            <TouchableOpacity
              style={[s.chatSendBtn, (!input.trim() || sending) && { opacity: 0.5 }]}
              onPress={send}
              disabled={!input.trim() || sending}
            >
              <Text style={{ fontSize: 18 }}>{sending ? '⏳' : '➤'}</Text>
            </TouchableOpacity>
          </View>
        </KeyboardAvoidingView>
      </View>
    );
  }

  return (
    <TabPage>
      <TouchableOpacity style={s.newSessionBtn} onPress={createSession}>
        <Text style={s.newSessionBtnText}>＋ Bắt đầu phiên học mới</Text>
      </TouchableOpacity>
      <SectionTitle>Lịch sử ({sessions.length} phiên)</SectionTitle>
      {!sessions.length ? (
        <EmptyView icon="🤖" text="Chưa có phiên học nào. Tạo phiên mới để bắt đầu!" />
      ) : (
        sessions.map((sess: any) => (
          <TouchableOpacity key={sess.sessionId} onPress={() => openSession(sess)} activeOpacity={0.8}>
            <Card style={{ flexDirection: 'row', alignItems: 'center' }}>
              <View style={s.aiIcon}><Text style={{ fontSize: 22 }}>🤖</Text></View>
              <View style={{ flex: 1 }}>
                <Text style={{ fontSize: 14, fontWeight: '700', color: '#1e293b' }}>{sess.title}</Text>
                <Text style={{ fontSize: 11, color: '#94a3b8', marginTop: 2 }}>
                  {sess.messageCount} tin nhắn · {new Date(sess.createdAt).toLocaleDateString('vi-VN')}
                </Text>
              </View>
              <Text style={{ fontSize: 18, color: '#94a3b8' }}>›</Text>
            </Card>
          </TouchableOpacity>
        ))
      )}
    </TabPage>
  );
}

/* ── Online Learning ────────────────────────────────────────── */
function OnlineLearningTab() {
  const [sessions,        setSessions]        = useState<any[]>([]);
  const [loading,         setLoading]         = useState(true);
  const [roomCode,        setRoomCode]        = useState('');
  const [joining,         setJoining]         = useState(false);
  const [webViewRoomCode, setWebViewRoomCode] = useState<string | null>(null);

  useEffect(() => {
    onlineApi.sessions()
      .then(r => setSessions(r.data ?? []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const joinByCode = async () => {
    const code = roomCode.trim().toUpperCase();
    if (!code) { Alert.alert('Lỗi', 'Vui lòng nhập mã phòng học'); return; }
    setJoining(true);
    try {
      // Verify code exists first, then open WebView
      await onlineApi.joinByCode(code);
      setRoomCode('');
      setWebViewRoomCode(code);
    } catch (e: any) {
      Alert.alert('Không tham gia được', e?.response?.data?.message ?? 'Mã phòng không hợp lệ hoặc phòng đã đóng');
    }
    finally { setJoining(false); }
  };

  if (loading) return <LoadingView />;

  const activeSessions   = sessions.filter((s: any) => s.status === 'Active');
  const upcomingSessions = sessions.filter((s: any) => s.status === 'Scheduled');

  return (
    <>
    {/* Full-screen Modal — covers AppShell header + tab bar */}
    <Modal
      visible={!!webViewRoomCode}
      animationType="slide"
      statusBarTranslucent
      onRequestClose={() => setWebViewRoomCode(null)}
    >
      {webViewRoomCode ? (
        <ClassroomWebViewScreen
          roomCode={webViewRoomCode}
          onClose={() => setWebViewRoomCode(null)}
        />
      ) : null}
    </Modal>

    <TabPage>
      <View style={s.onlineBanner}>
        <Text style={s.onlineBannerTitle}>🖥️ Học trực tuyến</Text>
        <Text style={s.onlineBannerSub}>Tham gia lớp học online với đầy đủ video, whiteboard</Text>
      </View>

      {/* Nhập mã phòng */}
      <View style={s.joinCard}>
        <Text style={s.joinTitle}>Nhập mã phòng học</Text>
        <View style={s.joinRow}>
          <TextInput
            style={s.joinInput}
            value={roomCode}
            onChangeText={t => setRoomCode(t.replace(/\D/g, ''))}
            placeholder="VD: 123456"
            placeholderTextColor="#94a3b8"
            keyboardType="number-pad"
            maxLength={6}
          />
          <TouchableOpacity
            style={[s.joinBtn, joining && { opacity: 0.6 }]}
            onPress={joinByCode}
            disabled={joining}
          >
            <Text style={s.joinBtnText}>{joining ? '⏳' : 'Vào'}</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Đang diễn ra */}
      {activeSessions.length > 0 && (
        <>
          <SectionTitle>🔴 Đang diễn ra ({activeSessions.length})</SectionTitle>
          {activeSessions.map((sess: any) => (
            <OnlineSessionCard
              key={sess.id}
              session={sess}
              onOpen={() => setWebViewRoomCode(sess.roomCode)}
            />
          ))}
        </>
      )}

      {/* Sắp diễn ra */}
      {upcomingSessions.length > 0 && (
        <>
          <SectionTitle>📅 Sắp diễn ra ({upcomingSessions.length})</SectionTitle>
          {upcomingSessions.map((sess: any) => (
            <OnlineSessionCard key={sess.id} session={sess} />
          ))}
        </>
      )}

      {activeSessions.length === 0 && upcomingSessions.length === 0 && (
        <EmptyView icon="🖥️" text="Không có buổi học nào đang mở. Nhập mã phòng để tham gia." />
      )}
    </TabPage>
    </>
  );
}

function OnlineSessionCard({ session, onOpen }: { session: any; onOpen?: (s: any) => void }) {
  const isActive = session.status === 'Active';
  return (
    <View style={[s.sessionCard, isActive && s.sessionCardActive]}>
      <View style={{ flex: 1 }}>
        <Text style={s.sessionTitle} numberOfLines={1}>{session.title}</Text>
        <Text style={s.sessionTeacher}>👨‍🏫 {session.teacherName}</Text>
        {session.scheduledAt && (
          <Text style={s.sessionTime}>
            🕐 {new Date(session.scheduledAt).toLocaleString('vi-VN', {
              hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit',
            })}
          </Text>
        )}
        <View style={{ flexDirection: 'row', gap: 8, marginTop: 6, alignItems: 'center' }}>
          <View style={s.roomCodeBadge}>
            <Text style={s.roomCodeText}>{session.roomCode}</Text>
          </View>
          <Text style={{ fontSize: 11, color: '#94a3b8' }}>
            {session.participantCount ?? 0}/{session.maxParticipants} người
          </Text>
        </View>
      </View>
      {isActive && onOpen && (
        <TouchableOpacity style={s.enterBtn} onPress={() => onOpen(session)}>
          <Text style={s.enterBtnText}>Vào lớp →</Text>
        </TouchableOpacity>
      )}
    </View>
  );
}

/* ── Virtual Lab ────────────────────────────────────────────── */
function VirtualLabTab() {
  const [sessions,       setSessions]       = useState<any[]>([]);
  const [loading,        setLoading]        = useState(true);
  const [sessionCode,    setSessionCode]    = useState('');
  const [activeLabCode,  setActiveLabCode]  = useState<string | null>(null);
  const [activeLabName,  setActiveLabName]  = useState<string | undefined>(undefined);
  const [joining,        setJoining]        = useState(false);

  useEffect(() => {
    virtualLabApi.sessions()
      .then(r => setSessions(r.data ?? []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const joinByCode = async () => {
    const code = sessionCode.trim().toUpperCase();
    if (!code) { Alert.alert('Lỗi', 'Vui lòng nhập mã phòng lab'); return; }
    setJoining(true);
    setActiveLabCode(code);
    setActiveLabName(undefined);
    setJoining(false);
  };

  if (loading) return <LoadingView />;

  return (
    <>
    {/* Full-screen Modal — covers AppShell header + tab bar */}
    <Modal
      visible={!!activeLabCode}
      animationType="slide"
      statusBarTranslucent
      onRequestClose={() => { setActiveLabCode(null); setActiveLabName(undefined); }}
    >
      {activeLabCode ? (
        <VirtualLabWebViewScreen
          sessionCode={activeLabCode}
          labName={activeLabName}
          onClose={() => { setActiveLabCode(null); setActiveLabName(undefined); }}
        />
      ) : null}
    </Modal>

    <TabPage>
      <View style={[s.onlineBanner, { backgroundColor: '#0f172a' }]}>
        <Text style={s.onlineBannerTitle}>🔬 Phòng Lab 3D</Text>
        <Text style={s.onlineBannerSub}>Thí nghiệm hóa học, vật lý, sinh học trong không gian 3D</Text>
      </View>

      {/* Nhập mã phòng */}
      <View style={s.joinCard}>
        <Text style={s.joinTitle}>Nhập mã phòng lab</Text>
        <View style={s.joinRow}>
          <TextInput
            style={s.joinInput}
            value={sessionCode}
            onChangeText={t => setSessionCode(t.replace(/\D/g, ''))}
            placeholder="VD: 123456"
            placeholderTextColor="#94a3b8"
            keyboardType="number-pad"
            maxLength={6}
          />
          <TouchableOpacity
            style={[s.joinBtn, { backgroundColor: '#10b981' }, joining && { opacity: 0.6 }]}
            onPress={joinByCode}
            disabled={joining}
          >
            <Text style={s.joinBtnText}>{joining ? '⏳' : 'Vào'}</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Phòng lab đang mở */}
      {sessions.length > 0 && (
        <>
          <SectionTitle>🟢 Phòng lab đang mở ({sessions.length})</SectionTitle>
          {sessions.map((lab: any) => (
            <TouchableOpacity
              key={lab.id}
              activeOpacity={0.85}
              onPress={() => { setActiveLabCode(lab.sessionCode); setActiveLabName(lab.title ?? lab.subjectTag); }}
            >
              <View style={[s.sessionCard, { borderLeftColor: '#10b981' }]}>
                <View style={{ flex: 1 }}>
                  <Text style={s.sessionTitle}>{lab.title ?? `Lab ${lab.subjectTag}`}</Text>
                  <Text style={s.sessionTeacher}>👨‍🏫 {lab.teacherName}</Text>
                  <View style={{ flexDirection: 'row', gap: 8, marginTop: 6, alignItems: 'center' }}>
                    <View style={[s.roomCodeBadge, { backgroundColor: '#ecfdf5' }]}>
                      <Text style={[s.roomCodeText, { color: '#10b981' }]}>{lab.sessionCode}</Text>
                    </View>
                    <Text style={{ fontSize: 11, color: '#94a3b8' }}>
                      {lab.participantCount ?? 0} người tham gia
                    </Text>
                  </View>
                </View>
                <View style={[s.enterBtn, { backgroundColor: '#10b981' }]}>
                  <Text style={s.enterBtnText}>Vào lab →</Text>
                </View>
              </View>
            </TouchableOpacity>
          ))}
        </>
      )}

      {sessions.length === 0 && (
        <EmptyView icon="🔬" text="Không có phòng lab nào đang mở. Nhập mã phòng để tham gia." />
      )}
    </TabPage>
    </>
  );
}

/* ── Shared helpers ─────────────────────────────────────────── */
function SemPicker({ semesters, active, onSelect, accent }: any) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 14 }}>
      {semesters.map((sem: any) => {
        const isActive = active?.semesterId === sem.semesterId;
        return (
          <TouchableOpacity key={sem.semesterId}
            style={[s.semBtn, isActive && { backgroundColor: accent }]}
            onPress={() => onSelect(sem)}>
            <Text style={[s.semText, isActive && { color: '#fff' }]}>{sem.semesterName}</Text>
          </TouchableOpacity>
        );
      })}
    </ScrollView>
  );
}

const s = StyleSheet.create({
  hero: { backgroundColor: '#fff', borderRadius: 16, padding: 20, marginBottom: 12, flexDirection: 'row',
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.07, shadowRadius: 8, elevation: 4,
    borderTopWidth: 4, borderTopColor: '#2563eb' },
  heroLeft: { flex: 1, justifyContent: 'center' },
  heroLabel:{ fontSize: 11, fontWeight: '700', color: '#94a3b8', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 4 },
  heroGpa:  { fontSize: 44, fontWeight: '900', lineHeight: 50 },
  heroSem:  { fontSize: 11, color: '#94a3b8', marginTop: 4 },
  heroDivider: { width: 1, backgroundColor: '#f1f5f9', marginHorizontal: 16 },
  heroRight:{ justifyContent: 'space-around' },
  heroStat: { alignItems: 'center', paddingHorizontal: 6 },
  heroStatVal:  { fontSize: 20, fontWeight: '800', color: '#1e293b' },
  heroStatLabel:{ fontSize: 10, color: '#94a3b8', marginTop: 2 },

  qrBtn:    { backgroundColor: '#2563eb', borderRadius: 14, padding: 16, flexDirection: 'row', alignItems: 'center', marginBottom: 14, gap: 12 },
  qrBtnIcon:{ fontSize: 28 },
  qrBtnTitle:{ fontSize: 15, fontWeight: '800', color: '#fff' },
  qrBtnSub: { fontSize: 11, color: 'rgba(255,255,255,0.7)', marginTop: 2 },

  subRow: { flexDirection: 'row', alignItems: 'center', backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  subName:{ flex: 1, fontSize: 14, fontWeight: '600', color: '#1e293b', marginRight: 8 },

  cardSmLabel:{ fontSize: 11, color: '#94a3b8', fontWeight: '600', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 6 },
  bigScore:   { fontSize: 40, fontWeight: '900' },
  subjectCard:{ backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  subjectName:  { fontSize: 14, fontWeight: '700', color: '#1e293b', marginBottom: 2 },
  subjectRemark:{ fontSize: 11, color: '#94a3b8' },

  attRow: { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  attDate:{ fontSize: 13, fontWeight: '700', color: '#1e293b' },
  attSub: { fontSize: 11, color: '#94a3b8', marginTop: 2 },

  back:    { marginBottom: 12 },
  backText:{ fontSize: 14, fontWeight: '700', color: '#2563eb' },

  courseHeader:    { backgroundColor: '#1d4ed8', borderRadius: 14, padding: 16, marginBottom: 14 },
  courseHeaderName:{ fontSize: 18, fontWeight: '800', color: '#fff' },
  courseHeaderSub: { fontSize: 12, color: 'rgba(255,255,255,0.7)', marginTop: 3 },

  courseIcon:{ width: 48, height: 48, borderRadius: 14, backgroundColor: '#eff6ff', alignItems: 'center', justifyContent: 'center', marginRight: 12 },

  hwCard: { backgroundColor: '#fff', borderRadius: 12, padding: 14, marginBottom: 8, flexDirection: 'row', alignItems: 'flex-start',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.04, shadowRadius: 4, elevation: 2 },
  hwTitle:{ fontSize: 14, fontWeight: '700', color: '#1e293b', marginBottom: 4 },
  hwSub:  { fontSize: 11, color: '#94a3b8', marginBottom: 4 },
  hwDue:  { fontSize: 11, color: '#d97706', fontWeight: '600' },

  newSessionBtn:    { backgroundColor: '#7c3aed', borderRadius: 12, padding: 14, alignItems: 'center', marginBottom: 14 },
  newSessionBtnText:{ fontSize: 14, fontWeight: '800', color: '#fff' },
  aiIcon:   { width: 44, height: 44, borderRadius: 12, backgroundColor: '#f5f3ff', alignItems: 'center', justifyContent: 'center', marginRight: 12 },

  chatHeader: { backgroundColor: '#0b1628', padding: 16, paddingTop: 20, flexDirection: 'row', alignItems: 'center' },
  chatBack:   { fontSize: 22, color: '#fff' },
  chatTitle:  { fontSize: 15, fontWeight: '800', color: '#fff' },
  chatSub:    { fontSize: 10, color: 'rgba(255,255,255,0.5)', marginTop: 1 },
  chatAiBadge:{ backgroundColor: '#f5f3ff', borderRadius: 8, paddingHorizontal: 8, paddingVertical: 4 },

  bubble:     { maxWidth: '80%', borderRadius: 16, padding: 12, marginBottom: 10 },
  bubbleUser: { backgroundColor: '#2563eb', alignSelf: 'flex-end' },
  bubbleAI:   { backgroundColor: '#fff', alignSelf: 'flex-start', borderWidth: 1, borderColor: '#e2e8f0' },
  bubbleRoleLabel: { fontSize: 10, color: '#7c3aed', fontWeight: '700', marginBottom: 4 },
  bubbleText: { fontSize: 14, color: '#fff', lineHeight: 20 },

  chatInput:  { backgroundColor: '#fff', borderTopWidth: 1, borderTopColor: '#e2e8f0', flexDirection: 'row', padding: 12, gap: 10, alignItems: 'flex-end' },
  chatInputBox:{ flex: 1, borderWidth: 1.5, borderColor: '#e2e8f0', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 10, fontSize: 14, color: '#1e293b', maxHeight: 100 },
  chatSendBtn: { width: 44, height: 44, borderRadius: 22, backgroundColor: '#2563eb', alignItems: 'center', justifyContent: 'center' },

  semBtn:  { paddingHorizontal: 16, paddingVertical: 9, borderRadius: 20, backgroundColor: '#f1f5f9', marginRight: 8 },
  semText: { fontSize: 13, fontWeight: '600', color: '#64748b' },

  // Online Learning
  onlineBanner:    { backgroundColor: '#0f172a', borderRadius: 16, padding: 18, marginBottom: 14 },
  onlineBannerTitle:{ fontSize: 18, fontWeight: '800', color: '#fff' },
  onlineBannerSub: { fontSize: 12, color: 'rgba(255,255,255,0.55)', marginTop: 4 },

  joinCard:  { backgroundColor: '#fff', borderRadius: 14, padding: 16, marginBottom: 14,
    shadowColor: '#000', shadowOffset: { width: 0, height: 2 }, shadowOpacity: 0.07, shadowRadius: 6, elevation: 3 },
  joinTitle: { fontSize: 13, fontWeight: '700', color: '#64748b', marginBottom: 10, textTransform: 'uppercase', letterSpacing: 0.5 },
  joinRow:   { flexDirection: 'row', gap: 10 },
  joinInput: { flex: 1, borderWidth: 1.5, borderColor: '#e2e8f0', borderRadius: 12,
    paddingHorizontal: 14, paddingVertical: 11, fontSize: 16, fontWeight: '800',
    color: '#1e293b', letterSpacing: 2, backgroundColor: '#f8fafc' },
  joinBtn:   { backgroundColor: '#2563eb', borderRadius: 12, paddingHorizontal: 20, alignItems: 'center', justifyContent: 'center' },
  joinBtnText:{ fontSize: 15, fontWeight: '800', color: '#fff' },

  resultCard: { backgroundColor: '#f0fdf4', borderRadius: 14, padding: 16, marginBottom: 14,
    borderWidth: 1.5, borderColor: '#86efac' },
  openWebBtn: { marginTop: 12, backgroundColor: '#2563eb', borderRadius: 10, paddingVertical: 11, alignItems: 'center' },
  openWebBtnText: { fontSize: 13, fontWeight: '800', color: '#fff' },

  sessionCard: { backgroundColor: '#fff', borderRadius: 14, padding: 14, marginBottom: 10,
    flexDirection: 'row', alignItems: 'center', gap: 10,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 4, elevation: 2,
    borderLeftWidth: 4, borderLeftColor: '#e2e8f0' },
  sessionCardActive: { borderLeftColor: '#dc2626', backgroundColor: '#fff9f9' },
  sessionTitle:  { fontSize: 14, fontWeight: '800', color: '#1e293b', marginBottom: 3 },
  sessionTeacher:{ fontSize: 12, color: '#64748b', marginBottom: 2 },
  sessionTime:   { fontSize: 11, color: '#94a3b8' },
  roomCodeBadge: { backgroundColor: '#eff6ff', borderRadius: 8, paddingHorizontal: 8, paddingVertical: 3 },
  roomCodeText:  { fontSize: 12, fontWeight: '800', color: '#2563eb', letterSpacing: 1 },
  enterBtn:      { backgroundColor: '#dc2626', borderRadius: 10, paddingVertical: 10, paddingHorizontal: 14 },
  enterBtnText:  { fontSize: 12, fontWeight: '800', color: '#fff' },

  // QR screen
  qrScreen: { flex: 1, backgroundColor: '#0b1628' },
  qrPermBtn:{ backgroundColor: '#2563eb', borderRadius: 12, padding: 14, alignItems: 'center' },
  qrOverlay:{ position: 'absolute', top: 0, left: 0, right: 0, bottom: 0, alignItems: 'center', justifyContent: 'space-between', padding: 20 },
  qrCloseBtn:{ alignSelf: 'flex-end', backgroundColor: 'rgba(0,0,0,0.5)', borderRadius: 20, paddingHorizontal: 16, paddingVertical: 8 },
  qrFrame:  { width: 220, height: 220, borderWidth: 3, borderColor: '#2563eb', borderRadius: 16 },
  qrHint:   { color: '#fff', fontSize: 14, fontWeight: '600', textAlign: 'center', marginBottom: 20 },
});
