import React, { useEffect, useState, useCallback } from 'react';
import {
  View, Text, StyleSheet, TouchableOpacity, Alert, ScrollView, TextInput,
} from 'react-native';
import AppShell from '../../components/AppShell';
import BottomSheet, { FieldLabel, FieldInput, FieldSelect, ErrorMsg } from '../../components/BottomSheet';
import {
  LoadingView, EmptyView, SectionTitle, StatCard,
  TabPage, Card, NotifItem, Badge,
} from '../../components/ui';
import { supervisorApi, commonApi } from '../../api/client';

/* ═══════════════════════════════════════════════════════════ */
/*  SUPERVISOR — 4 tabs: Tổng quan | Vi phạm | Điểm danh | TB */
/* ═══════════════════════════════════════════════════════════ */

export default function SupervisorHomeScreen() {
  const [report,        setReport]        = useState<any>(null);   // DailyDisciplineReportDto
  const [violations,    setViolations]    = useState<any[]>([]);
  const [notifications, setNotifications] = useState<any[]>([]);
  const [loading,       setLoading]       = useState(true);
  const [refreshing,    setRefreshing]    = useState(false);

  const loadAll = useCallback(async () => {
    try {
      const [reportRes, violRes, notifRes] = await Promise.all([
        supervisorApi.dailyReport(),
        supervisorApi.discipline(),
        commonApi.notifications(),
      ]);
      setReport(reportRes.data ?? null);
      // PagedResult<DisciplineRecordDto> → items array
      const raw = violRes.data;
      setViolations(Array.isArray(raw) ? raw : (raw?.items ?? []));
      setNotifications(notifRes.data?.items ?? notifRes.data ?? []);
    } catch {}
    finally { setLoading(false); setRefreshing(false); }
  }, []);

  useEffect(() => { loadAll(); }, [loadAll]);
  const onRefresh = () => { setRefreshing(true); loadAll(); };

  if (loading) return <LoadingView />;

  return (
    <AppShell accentColor="#7c3aed" roleLabel="Giám thị" tabs={[
      { key: 'home',  label: 'Tổng quan', icon: '🏠',
        content: <DashboardTab report={report} violations={violations} refreshing={refreshing} onRefresh={onRefresh} /> },
      { key: 'viol',  label: 'Vi phạm',   icon: '⚠️',
        content: <ViolationsTab violations={violations} refreshing={refreshing} onRefresh={onRefresh} onReload={loadAll} /> },
      { key: 'att',   label: 'Hôm nay',   icon: '📋',
        content: <TodayTab report={report} violations={violations} /> },
      { key: 'gate',  label: 'Cổng',      icon: '🚪',
        content: <GateCheckTab onReload={loadAll} /> },
      { key: 'notif', label: 'Thông báo', icon: '🔔',
        content: <NotifTab notifications={notifications} refreshing={refreshing} onRefresh={onRefresh} /> },
    ]} />
  );
}

/* ── Dashboard ─────────────────────────────────────────────── */
function DashboardTab({ report, violations, refreshing, onRefresh }: any) {
  const today = new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
  const pending  = violations.filter((v: any) => v.status !== 'Resolved').length;
  const resolved = violations.filter((v: any) => v.status === 'Resolved').length;

  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      <View style={sv.welcome}>
        <Text style={sv.welcomeTitle}>🛡️ Giám thị</Text>
        <Text style={sv.welcomeSub}>{today}</Text>
      </View>

      {/* Discipline stats hôm nay */}
      {report && (
        <Card style={{ marginBottom: 12 }}>
          <Text style={sv.cardSection}>Kỷ luật hôm nay</Text>
          <View style={sv.statsRow}>
            <StatCard label="Tổng vi phạm"  value={report.totalViolations ?? 0} accent="#dc2626" />
            <View style={{ width: 8 }} />
            <StatCard label="Nghiêm trọng"  value={report.severeCount ?? 0}     accent="#7c3aed" />
            <View style={{ width: 8 }} />
            <StatCard label="Đi muộn"       value={report.lateArrivalsCount ?? 0} accent="#d97706" />
          </View>
        </Card>
      )}

      {/* Gate check */}
      {report && (
        <Card style={{ marginBottom: 12 }}>
          <Text style={sv.cardSection}>Kiểm tra cổng hôm nay</Text>
          <View style={sv.statsRow}>
            <StatCard label="Vào trường"   value={report.gateChecksIn  ?? 0} accent="#16a34a" />
            <View style={{ width: 8 }} />
            <StatCard label="Ra trường"    value={report.gateChecksOut ?? 0} accent="#2563eb" />
          </View>
        </Card>
      )}

      {/* All-time stats */}
      <Card style={{ marginBottom: 12 }}>
        <Text style={sv.cardSection}>Tất cả vi phạm</Text>
        <View style={sv.statsRow}>
          <StatCard label="Tổng cộng"   value={violations.length} accent="#64748b" />
          <View style={{ width: 8 }} />
          <StatCard label="Chưa xử lý" value={pending}            accent="#d97706" />
          <View style={{ width: 8 }} />
          <StatCard label="Đã xử lý"   value={resolved}           accent="#16a34a" />
        </View>
      </Card>

      <SectionTitle>Vi phạm gần đây</SectionTitle>
      {!violations.length
        ? <EmptyView icon="✅" text="Không có vi phạm nào" />
        : violations.slice(0, 5).map((v: any, i: number) => <ViolationCard key={i} v={v} />)
      }
    </TabPage>
  );
}

/* ── Violations ─────────────────────────────────────────────── */
function ViolationsTab({ violations, refreshing, onRefresh, onReload }: any) {
  const [filter,   setFilter]   = useState<'all' | 'pending' | 'resolved'>('all');
  const [students, setStudents] = useState<any[]>([]);

  // Create violation
  const [showCreate, setShowCreate] = useState(false);
  const [cvStudent,  setCvStudent]  = useState('');
  const [cvType,     setCvType]     = useState('');
  const [cvSeverity, setCvSeverity] = useState('Minor');
  const [cvDesc,     setCvDesc]     = useState('');
  const [cvAction,   setCvAction]   = useState('');
  const [cvSubmit,   setCvSubmit]   = useState(false);
  const [cvError,    setCvError]    = useState('');

  // Resolve
  const [showResolve, setShowResolve]   = useState(false);
  const [resolveId,   setResolveId]     = useState(0);
  const [resolveAct,  setResolveAct]    = useState('');
  const [rSubmit,     setRSubmit]       = useState(false);

  const openCreate = async () => {
    if (!students.length) {
      try { const r = await supervisorApi.students(); setStudents(r.data ?? []); } catch {}
    }
    setCvStudent(''); setCvType(''); setCvSeverity('Minor');
    setCvDesc(''); setCvAction(''); setCvError('');
    setShowCreate(true);
  };

  const submitCreate = async () => {
    if (!cvStudent) { setCvError('Vui lòng chọn học sinh'); return; }
    if (!cvType.trim()) { setCvError('Vui lòng nhập loại vi phạm'); return; }
    setCvSubmit(true); setCvError('');
    const d = new Date();
    const dateStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    try {
      await supervisorApi.createViolation({
        studentId: parseInt(cvStudent), recordDate: dateStr,
        violationType: cvType.trim(), severity: cvSeverity,
        description: cvDesc || null, actionTaken: cvAction || null,
      });
      Alert.alert('✅ Đã ghi nhận vi phạm');
      setShowCreate(false); onReload?.();
    } catch (e: any) { setCvError(e?.response?.data?.message ?? 'Có lỗi xảy ra'); }
    finally { setCvSubmit(false); }
  };

  const openResolve = (v: any) => {
    setResolveId(v.recordId); setResolveAct(v.actionTaken ?? ''); setShowResolve(true);
  };

  const submitResolve = async () => {
    if (!resolveAct.trim()) { Alert.alert('Lỗi','Vui lòng nhập hành động xử lý'); return; }
    setRSubmit(true);
    try {
      await supervisorApi.resolveViolation(resolveId, resolveAct.trim());
      Alert.alert('✅ Đã xử lý vi phạm');
      setShowResolve(false); onReload?.();
    } catch (e: any) { Alert.alert('Lỗi', e?.response?.data?.message ?? 'Có lỗi xảy ra'); }
    finally { setRSubmit(false); }
  };

  const filtered = violations.filter((v: any) => {
    if (filter === 'pending')  return v.status !== 'Resolved';
    if (filter === 'resolved') return v.status === 'Resolved';
    return true;
  });
  const counts = {
    all:      violations.length,
    pending:  violations.filter((v: any) => v.status !== 'Resolved').length,
    resolved: violations.filter((v: any) => v.status === 'Resolved').length,
  };

  return (
    <TabPage refreshing={refreshing} onRefresh={onRefresh}>
      <TouchableOpacity style={sv.addBtn} onPress={openCreate}>
        <Text style={sv.addBtnText}>＋ Ghi nhận vi phạm mới</Text>
      </TouchableOpacity>

      <View style={sv.filterRow}>
        {(['all', 'pending', 'resolved'] as const).map(f => (
          <TouchableOpacity key={f}
            style={[sv.filterBtn, filter === f && sv.filterActive]}
            onPress={() => setFilter(f)}>
            <Text style={[sv.filterText, filter === f && { color: '#fff' }]}>
              {f === 'all' ? 'Tất cả' : f === 'pending' ? 'Chưa xử lý' : 'Đã xử lý'}
            </Text>
            <View style={[sv.filterBadge, filter === f && { backgroundColor: 'rgba(255,255,255,0.25)' }]}>
              <Text style={[sv.filterBadgeText, filter === f && { color: '#fff' }]}>{counts[f]}</Text>
            </View>
          </TouchableOpacity>
        ))}
      </View>

      {!filtered.length
        ? <EmptyView icon="✅" text="Không có vi phạm nào" />
        : filtered.map((v: any, i: number) => (
          <ViolationCard key={i} v={v}
            onResolve={v.status !== 'Resolved' ? () => openResolve(v) : undefined}
          />
        ))
      }

      {/* Modal ghi nhận */}
      <BottomSheet visible={showCreate} title="Ghi nhận vi phạm"
        onClose={() => setShowCreate(false)} onSubmit={submitCreate}
        submitting={cvSubmit} submitLabel="Ghi nhận" accent="#dc2626">
        <FieldLabel>Học sinh</FieldLabel>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 4 }}>
          {students.slice(0, 30).map((s: any) => (
            <TouchableOpacity key={s.userId}
              style={[{ paddingHorizontal: 12, paddingVertical: 8, borderRadius: 20, marginRight: 8,
                backgroundColor: cvStudent === String(s.userId) ? '#dc2626' : '#f1f5f9' }]}
              onPress={() => setCvStudent(String(s.userId))}>
              <Text style={{ fontSize: 12, fontWeight: '600',
                color: cvStudent === String(s.userId) ? '#fff' : '#64748b' }}>
                {s.fullName}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
        <FieldLabel>Loại vi phạm</FieldLabel>
        <FieldInput value={cvType} onChange={setCvType} placeholder="VD: Nghỉ không phép, dùng điện thoại..." />
        <FieldLabel>Mức độ</FieldLabel>
        <FieldSelect value={cvSeverity} onChange={setCvSeverity} options={[
          { label: '🟡 Nhẹ', value: 'Minor' },
          { label: '🟠 Trung bình', value: 'Moderate' },
          { label: '🔴 Nghiêm trọng', value: 'Severe' },
        ]} />
        <FieldLabel>Mô tả chi tiết</FieldLabel>
        <FieldInput value={cvDesc} onChange={setCvDesc} placeholder="Chi tiết hành vi..." multiline />
        <FieldLabel>Hành động xử lý (nếu có)</FieldLabel>
        <FieldInput value={cvAction} onChange={setCvAction} placeholder="VD: Cảnh cáo, gọi phụ huynh..." />
        <ErrorMsg text={cvError} />
      </BottomSheet>

      {/* Modal xử lý */}
      <BottomSheet visible={showResolve} title="Xử lý vi phạm"
        onClose={() => setShowResolve(false)} onSubmit={submitResolve}
        submitting={rSubmit} submitLabel="Xác nhận xử lý" accent="#16a34a">
        <FieldLabel>Hành động xử lý *</FieldLabel>
        <FieldInput value={resolveAct} onChange={setResolveAct}
          placeholder="VD: Gọi phụ huynh, cảnh cáo trước trường, viết kiểm điểm..." multiline />
      </BottomSheet>
    </TabPage>
  );
}

/* ── Today tab (báo cáo kỷ luật hôm nay + vi phạm trong ngày) */
function TodayTab({ report, violations }: any) {
  const today = new Date().toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });

  // Lấy vi phạm hôm nay từ danh sách
  const todayStr  = new Date().toISOString().slice(0, 10);
  const todayViol = violations.filter((v: any) => {
    const d = v.recordDate ?? v.createdAt ?? '';
    return d.toString().startsWith(todayStr);
  });

  // Nếu backend trả records trong report
  const reportRecords = report?.records ?? [];

  const displayViol = reportRecords.length > 0 ? reportRecords : todayViol;

  return (
    <TabPage>
      <SectionTitle>Tổng quan — {today}</SectionTitle>

      {report ? (
        <>
          <View style={sv.statsRow}>
            <StatCard label="Vi phạm"     value={report.totalViolations   ?? 0} accent="#dc2626" />
            <View style={{ width: 8 }} />
            <StatCard label="Nhẹ"         value={report.minorCount        ?? 0} accent="#d97706" />
            <View style={{ width: 8 }} />
            <StatCard label="TB"          value={report.moderateCount     ?? 0} accent="#7c3aed" />
            <View style={{ width: 8 }} />
            <StatCard label="Nghiêm"      value={report.severeCount       ?? 0} accent="#dc2626" />
          </View>

          <Card style={{ marginBottom: 12 }}>
            <View style={sv.statsRow}>
              <StatCard label="Vào trường"  value={report.gateChecksIn      ?? 0} accent="#16a34a" />
              <View style={{ width: 8 }} />
              <StatCard label="Ra trường"   value={report.gateChecksOut     ?? 0} accent="#2563eb" />
              <View style={{ width: 8 }} />
              <StatCard label="Đến muộn"    value={report.lateArrivalsCount ?? 0} accent="#d97706" />
            </View>
          </Card>
        </>
      ) : null}

      <SectionTitle>Vi phạm hôm nay ({displayViol.length})</SectionTitle>
      {!displayViol.length
        ? <EmptyView icon="✅" text="Không có vi phạm nào hôm nay" />
        : displayViol.map((v: any, i: number) => <ViolationCard key={i} v={v} />)
      }
    </TabPage>
  );
}

/* ── Gate Check Tab ─────────────────────────────────────────── */
function GateCheckTab({ onReload }: any) {
  const [students, setStudents] = useState<any[]>([]);
  const [studentId, setStudentId] = useState('');
  const [checkType, setCheckType] = useState('In');
  const [isLate,    setIsLate]    = useState(false);
  const [note,      setNote]      = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error,     setError]     = useState('');

  useEffect(() => {
    supervisorApi.students().then(r => setStudents(r.data ?? [])).catch(() => {});
  }, []);

  const submit = async () => {
    if (!studentId) { setError('Vui lòng chọn học sinh'); return; }
    setSubmitting(true); setError('');
    try {
      await supervisorApi.gateCheck({
        studentId: parseInt(studentId),
        checkType,
        isLate,
        note: note || null,
      });
      Alert.alert('✅ Đã ghi nhận', `Đã ghi nhận ${checkType === 'In' ? 'vào' : 'ra'} trường${isLate ? ' (muộn)' : ''}`);
      setStudentId(''); setNote(''); setIsLate(false);
      onReload?.();
    } catch (e: any) {
      setError(e?.response?.data?.message ?? 'Có lỗi xảy ra');
    }
    finally { setSubmitting(false); }
  };

  return (
    <TabPage>
      <View style={sv.gateCard}>
        <Text style={sv.gateTitle}>🚪 Kiểm tra cổng trường</Text>
        <Text style={sv.gateSub}>{new Date().toLocaleDateString('vi-VN', { weekday: 'long', day: 'numeric', month: 'long' })}</Text>
      </View>

      <SectionTitle>Loại kiểm tra</SectionTitle>
      <View style={{ flexDirection: 'row', gap: 10, marginBottom: 14 }}>
        {[{ v: 'In', l: '🏫 Vào trường' }, { v: 'Out', l: '🏠 Ra về' }].map(opt => (
          <TouchableOpacity key={opt.v}
            style={[sv.gateTypeBtn, checkType === opt.v && sv.gateTypeBtnActive]}
            onPress={() => setCheckType(opt.v)}>
            <Text style={[sv.gateTypeText, checkType === opt.v && { color: '#fff' }]}>{opt.l}</Text>
          </TouchableOpacity>
        ))}
      </View>

      <SectionTitle>Học sinh</SectionTitle>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 14 }}>
        {students.slice(0, 40).map((s: any) => (
          <TouchableOpacity key={s.userId}
            style={[sv.stuChip, studentId === String(s.userId) && sv.stuChipActive]}
            onPress={() => setStudentId(String(s.userId))}>
            <Text style={[sv.stuChipText, studentId === String(s.userId) && { color: '#fff' }]}>
              {s.fullName}
            </Text>
          </TouchableOpacity>
        ))}
      </ScrollView>

      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10, marginBottom: 14 }}>
        <TouchableOpacity
          style={[sv.lateToggle, isLate && sv.lateToggleOn]}
          onPress={() => setIsLate(!isLate)}>
          <Text style={{ fontSize: 16 }}>{isLate ? '🔴' : '🟢'}</Text>
          <Text style={[{ fontSize: 13, fontWeight: '700', color: '#64748b' }, isLate && { color: '#dc2626' }]}>
            {isLate ? 'Đến muộn' : 'Đúng giờ'}
          </Text>
        </TouchableOpacity>
      </View>

      <SectionTitle>Ghi chú (tùy chọn)</SectionTitle>
      <TextInput
        style={sv.gateNote}
        value={note}
        onChangeText={setNote}
        placeholder="Ghi chú..."
        placeholderTextColor="#94a3b8"
        multiline
        numberOfLines={2}
      />

      {error ? <Text style={{ color: '#dc2626', fontSize: 13, marginBottom: 8 }}>⚠ {error}</Text> : null}

      <TouchableOpacity
        style={[sv.gateSubmitBtn, submitting && { opacity: 0.6 }]}
        onPress={submit}
        disabled={submitting}
      >
        <Text style={sv.gateSubmitText}>
          {submitting ? 'Đang lưu...' : `✅ Xác nhận ${checkType === 'In' ? 'Vào' : 'Ra'} trường`}
        </Text>
      </TouchableOpacity>
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
        ? <EmptyView icon="🔕" text="Chưa có thông báo" />
        : notifications.map((n: any, i: number) => <NotifItem key={i} item={n} />)
      }
    </TabPage>
  );
}

/* ── Violation card ─────────────────────────────────────────── */
function ViolationCard({ v, onResolve }: { v: any; onResolve?: () => void }) {
  const isHigh   = v.severity === 'High' || v.severity === 'Severe';
  const resolved = v.status === 'Resolved';

  return (
    <View style={[sv.vCard, isHigh && sv.vCardHigh]}>
      <View style={sv.vHead}>
        <Text style={sv.vStudent} numberOfLines={1}>{v.studentName ?? '—'}</Text>
        <View style={{ flexDirection: 'row', gap: 5, flexShrink: 0 }}>
          <Badge
            text={isHigh ? 'Nghiêm' : v.severity === 'Moderate' ? 'TB' : 'Nhẹ'}
            color={isHigh ? '#dc2626' : v.severity === 'Moderate' ? '#7c3aed' : '#d97706'}
            bg={isHigh ? '#fef2f2' : v.severity === 'Moderate' ? '#f5f3ff' : '#fffbeb'}
          />
          <Badge
            text={resolved ? 'Đã xử lý' : 'Chưa xử lý'}
            color={resolved ? '#16a34a' : '#64748b'}
            bg={resolved ? '#f0fdf4' : '#f1f5f9'}
          />
        </View>
      </View>
      {v.violationType && <Text style={sv.vType}>{v.violationType}</Text>}
      {v.description   && <Text style={sv.vDesc} numberOfLines={2}>{v.description}</Text>}
      <View style={sv.vFoot}>
        <Text style={sv.vDate}>{formatDate(v.recordDate ?? v.createdAt)}</Text>
        {v.className && <Text style={sv.vClass}>{v.className}</Text>}
      </View>
      {v.actionTaken && (
        <View style={sv.vAction}>
          <Text style={sv.vActionLabel}>Xử lý: </Text>
          <Text style={sv.vActionText}>{v.actionTaken}</Text>
        </View>
      )}
      {!resolved && onResolve && (
        <View style={{ flexDirection: 'row', gap: 8, marginTop: 10 }}>
          <TouchableOpacity style={[sv.resolveBtn, { flex: 1 }]} onPress={onResolve}>
            <Text style={sv.resolveBtnText}>✅ Xử lý</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[sv.resolveBtn, { flex: 1, borderColor: '#fbbf24', backgroundColor: '#fffbeb' }]}
            onPress={() => Alert.alert('Chuyển cấp', 'Tính năng chuyển vi phạm lên ban giám hiệu đang được phát triển.')}
          >
            <Text style={[sv.resolveBtnText, { color: '#d97706' }]}>⬆ Chuyển cấp</Text>
          </TouchableOpacity>
        </View>
      )}
    </View>
  );
}

function formatDate(d?: string) {
  if (!d) return '';
  return new Date(d.toString()).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

const sv = StyleSheet.create({
  welcome:      { backgroundColor: '#6d28d9', borderRadius: 16, padding: 18, marginBottom: 12 },
  welcomeTitle: { fontSize: 18, fontWeight: '800', color: '#fff' },
  welcomeSub:   { fontSize: 12, color: 'rgba(255,255,255,0.65)', marginTop: 4 },

  cardSection: { fontSize: 11, fontWeight: '800', color: '#64748b', textTransform: 'uppercase', letterSpacing: 0.6, marginBottom: 12 },
  statsRow:    { flexDirection: 'row' },

  filterRow:       { flexDirection: 'row', gap: 8, marginBottom: 14 },
  filterBtn:       { flex: 1, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 6, paddingVertical: 10, borderRadius: 12, backgroundColor: '#f1f5f9' },
  filterActive:    { backgroundColor: '#6d28d9' },
  filterText:      { fontSize: 12, fontWeight: '700', color: '#64748b' },
  filterBadge:     { backgroundColor: '#e2e8f0', borderRadius: 10, paddingHorizontal: 7, paddingVertical: 2 },
  filterBadgeText: { fontSize: 11, fontWeight: '800', color: '#64748b' },

  vCard: {
    backgroundColor: '#fff', borderRadius: 14, padding: 14, marginBottom: 10,
    borderLeftWidth: 4, borderLeftColor: '#e2e8f0',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.05, shadowRadius: 4, elevation: 2,
  },
  vCardHigh: { borderLeftColor: '#dc2626' },
  vHead:     { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', marginBottom: 6, gap: 8 },
  vStudent:  { fontSize: 14, fontWeight: '800', color: '#1e293b', flex: 1 },
  vType:     { fontSize: 12, fontWeight: '700', color: '#7c3aed', marginBottom: 4 },
  vDesc:     { fontSize: 13, color: '#64748b', lineHeight: 18, marginBottom: 8 },
  vFoot:     { flexDirection: 'row', justifyContent: 'space-between' },
  vDate:     { fontSize: 11, color: '#94a3b8' },
  vClass:    { fontSize: 11, color: '#7c3aed', fontWeight: '600' },
  vAction:   { flexDirection: 'row', marginTop: 8, backgroundColor: '#f0fdf4', borderRadius: 8, padding: 8 },
  vActionLabel:{ fontSize: 12, fontWeight: '700', color: '#16a34a' },
  vActionText: { fontSize: 12, color: '#16a34a', flex: 1 },

  addBtn:     { backgroundColor: '#dc2626', borderRadius: 12, paddingVertical: 13, alignItems: 'center', marginBottom: 14 },
  addBtnText: { fontSize: 14, fontWeight: '800', color: '#fff' },

  resolveBtn:     { backgroundColor: '#f0fdf4', borderRadius: 10, paddingVertical: 9, alignItems: 'center', borderWidth: 1, borderColor: '#86efac' },
  resolveBtnText: { fontSize: 13, fontWeight: '700', color: '#16a34a' },

  gateCard:        { backgroundColor: '#6d28d9', borderRadius: 16, padding: 18, marginBottom: 14 },
  gateTitle:       { fontSize: 18, fontWeight: '800', color: '#fff' },
  gateSub:         { fontSize: 12, color: 'rgba(255,255,255,0.65)', marginTop: 4 },
  gateTypeBtn:     { flex: 1, paddingVertical: 12, borderRadius: 12, backgroundColor: '#f1f5f9', alignItems: 'center' },
  gateTypeBtnActive:{ backgroundColor: '#6d28d9' },
  gateTypeText:    { fontSize: 14, fontWeight: '700', color: '#64748b' },
  stuChip:         { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, backgroundColor: '#f1f5f9', marginRight: 8 },
  stuChipActive:   { backgroundColor: '#6d28d9' },
  stuChipText:     { fontSize: 12, fontWeight: '600', color: '#64748b' },
  lateToggle:      { flexDirection: 'row', alignItems: 'center', gap: 8, paddingHorizontal: 14, paddingVertical: 10, borderRadius: 12, backgroundColor: '#f1f5f9', borderWidth: 1, borderColor: '#e2e8f0' },
  lateToggleOn:    { backgroundColor: '#fef2f2', borderColor: '#fca5a5' },
  gateNote:        { borderWidth: 1.5, borderColor: '#e2e8f0', borderRadius: 12, paddingHorizontal: 14, paddingVertical: 11, fontSize: 14, color: '#1e293b', marginBottom: 14, minHeight: 60, textAlignVertical: 'top' },
  gateSubmitBtn:   { backgroundColor: '#6d28d9', borderRadius: 12, paddingVertical: 14, alignItems: 'center' },
  gateSubmitText:  { fontSize: 15, fontWeight: '800', color: '#fff' },
});
