import React, { useEffect, useState } from 'react';
import { View, Text, StyleSheet, ScrollView, TouchableOpacity, ActivityIndicator, RefreshControl, FlatList } from 'react-native';
import { useAuth } from '../../context/AuthContext';
import { commonApi, teacherApi } from '../../api/client';
import { colors, spacing, radius, typography } from '../../theme';

export default function TeacherHomeScreen({ navigation }: any) {
  const { user, logout } = useAuth();
  const [classes,   setClasses]   = useState<any[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [refreshing,setRefreshing]= useState(false);

  const loadData = async () => {
    try {
      const yearRes = await commonApi.academicYears();
      const years   = yearRes.data ?? [];
      const active  = years.find((y: any) => y.isActive) ?? years[0];
      if (active) {
        const classRes = await teacherApi.classes(active.academicYearId);
        setClasses(classRes.data ?? []);
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
          <Text style={s.greeting}>Giáo viên {user?.fullName.split(' ').pop()} 👋</Text>
          <Text style={s.sub}>{user?.schoolName}</Text>
        </View>
        <TouchableOpacity style={s.avatar} onPress={logout}>
          <Text style={s.avatarText}>{user?.fullName[0]}</Text>
        </TouchableOpacity>
      </View>

      {/* Quick actions */}
      <View style={s.quickRow}>
        <QuickCard icon="✅" label="Điểm danh QR" color="#dcfce7" onPress={() => navigation.navigate('QRAttendance')} />
        <QuickCard icon="📝" label="Nhập điểm" color="#fef9c3" onPress={() => navigation.navigate('GradeEntry')} />
      </View>

      <Text style={s.sectionTitle}>Lớp đang dạy</Text>

      {classes.length === 0 ? (
        <View style={s.empty}>
          <Text style={s.emptyText}>Chưa có lớp nào được phân công</Text>
        </View>
      ) : (
        classes.map((cls: any) => (
          <TouchableOpacity
            key={cls.classId}
            style={s.classCard}
            onPress={() => navigation.navigate('ClassDetail', { cls })}
            activeOpacity={0.8}>
            <View style={s.classIconWrap}>
              <Text style={s.classIcon}>🏫</Text>
            </View>
            <View style={s.classInfo}>
              <Text style={s.className}>{cls.className}</Text>
              <Text style={s.classDetail}>{cls.gradeLevel} · {cls.studentCount} học sinh</Text>
            </View>
            <Text style={s.chevron}>›</Text>
          </TouchableOpacity>
        ))
      )}

      <View style={{ height: 32 }} />
    </ScrollView>
  );
}

function QuickCard({ icon, label, color, onPress }: any) {
  return (
    <TouchableOpacity style={[s.quickCard, { backgroundColor: color }]} onPress={onPress} activeOpacity={0.8}>
      <Text style={s.quickCardIcon}>{icon}</Text>
      <Text style={s.quickCardLabel}>{label}</Text>
    </TouchableOpacity>
  );
}

const s = StyleSheet.create({
  page:        { flex: 1, backgroundColor: colors.ivory },
  center:      { flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.ivory },
  header:      {
    backgroundColor: colors.navy, paddingTop: 56, paddingBottom: spacing.lg,
    paddingHorizontal: spacing.lg, flexDirection: 'row',
    alignItems: 'center', justifyContent: 'space-between',
  },
  greeting:    { fontSize: 20, fontWeight: '700', color: colors.ivory },
  sub:         { fontSize: 12, color: 'rgba(250,248,244,0.5)', marginTop: 2 },
  avatar:      { width: 40, height: 40, borderRadius: 20, backgroundColor: colors.gold, alignItems: 'center', justifyContent: 'center' },
  avatarText:  { fontSize: 16, fontWeight: '700', color: colors.navy },

  quickRow:    { flexDirection: 'row', padding: spacing.md, gap: spacing.sm },
  quickCard:   {
    flex: 1, borderRadius: radius.md, padding: spacing.md, alignItems: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 6, elevation: 2,
  },
  quickCardIcon:  { fontSize: 32, marginBottom: 6 },
  quickCardLabel: { fontSize: 13, fontWeight: '700', color: colors.textPrimary },

  sectionTitle:{ ...typography.label, marginHorizontal: spacing.md, marginBottom: spacing.sm, marginTop: 4 },

  classCard:   {
    flexDirection: 'row', alignItems: 'center',
    backgroundColor: colors.white, marginHorizontal: spacing.md,
    marginBottom: spacing.sm, borderRadius: radius.md, padding: spacing.md,
    shadowColor: '#000', shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06, shadowRadius: 6, elevation: 2,
  },
  classIconWrap:{ width: 44, height: 44, borderRadius: 12, backgroundColor: '#ede9fe', alignItems: 'center', justifyContent: 'center', marginRight: spacing.md },
  classIcon:   { fontSize: 22 },
  classInfo:   { flex: 1 },
  className:   { fontSize: 16, fontWeight: '700', color: colors.navy },
  classDetail: { fontSize: 12, color: colors.textMuted, marginTop: 2 },
  chevron:     { fontSize: 24, color: colors.textMuted },

  empty:       { margin: spacing.lg, padding: spacing.lg, backgroundColor: colors.white, borderRadius: radius.md, alignItems: 'center' },
  emptyText:   { color: colors.textMuted, fontSize: 14 },
});
