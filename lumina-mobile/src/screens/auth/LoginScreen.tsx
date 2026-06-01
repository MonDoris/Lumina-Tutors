import React, { useState } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ActivityIndicator,
  ScrollView,
} from 'react-native';
import { useAuth } from '../../context/AuthContext';
import { colors, spacing, radius, typography } from '../../theme';

const ROLE_LABELS: Record<string, { label: string; icon: string; accent: string }> = {
  STUDENT:    { label: 'Học sinh',  icon: '🎓', accent: '#2563eb' },
  PARENT:     { label: 'Phụ huynh',icon: '👨‍👩‍👧', accent: '#16a34a' },
  TEACHER:    { label: 'Giáo viên',icon: '📚', accent: '#d97706' },
  SUPERVISOR: { label: 'Giám thị', icon: '🛡️', accent: '#7c3aed' },
  ACCOUNTANT: { label: 'Kế toán',  icon: '💼', accent: '#0891b2' },
  ADMIN:      { label: 'Quản trị', icon: '⚙️', accent: '#dc2626' },
};

interface Props {
  selectedRole: string;
  onBack: () => void;
}

export default function LoginScreen({ selectedRole, onBack }: Props) {
  const { login } = useAuth();
  const roleInfo = ROLE_LABELS[selectedRole] ?? { label: selectedRole, icon: '👤', accent: colors.navy };
  const [email,    setEmail]    = useState('');
  const [password, setPassword] = useState('');
  const [error,    setError]    = useState('');
  const [loading,  setLoading]  = useState(false);
  const [showPass, setShowPass] = useState(false);

  const handleLogin = async () => {
    if (!email.trim() || !password.trim()) {
      setError('Vui lòng nhập đầy đủ email và mật khẩu.');
      return;
    }
    setLoading(true);
    setError('');
    const err = await login(email.trim(), password);
    if (err) setError(err);
    setLoading(false);
  };

  return (
    <KeyboardAvoidingView
      style={s.flex}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
      <ScrollView contentContainerStyle={s.container} keyboardShouldPersistTaps="handled">

        {/* Header */}
        <View style={s.header}>
          <View style={s.logoWrap}>
            <Text style={s.logoIcon}>✦</Text>
          </View>
          <Text style={s.appName}>Lumina Tutors</Text>
          <Text style={s.appSub}>Hệ thống quản lý giáo dục</Text>
        </View>

        {/* Card */}
        <View style={s.card}>
          {/* Role badge + back */}
          <View style={s.roleBadgeRow}>
            <TouchableOpacity style={s.backBtn} onPress={onBack}>
              <Text style={s.backText}>← Đổi vai trò</Text>
            </TouchableOpacity>
            <View style={[s.roleBadge, { backgroundColor: roleInfo.accent + '18', borderColor: roleInfo.accent + '40' }]}>
              <Text style={s.roleBadgeIcon}>{roleInfo.icon}</Text>
              <Text style={[s.roleBadgeLabel, { color: roleInfo.accent }]}>{roleInfo.label}</Text>
            </View>
          </View>

          <Text style={s.cardTitle}>Đăng nhập</Text>
          <Text style={s.cardSub}>Nhập thông tin tài khoản của bạn</Text>

          {error ? (
            <View style={s.errorBox}>
              <Text style={s.errorText}>⚠ {error}</Text>
            </View>
          ) : null}

          <View style={s.field}>
            <Text style={s.label}>Địa chỉ Email</Text>
            <TextInput
              style={s.input}
              value={email}
              onChangeText={setEmail}
              placeholder="example@lumina.edu.vn"
              placeholderTextColor={colors.textMuted}
              keyboardType="email-address"
              autoCapitalize="none"
              autoCorrect={false}
            />
          </View>

          <View style={s.field}>
            <Text style={s.label}>Mật khẩu</Text>
            <View style={s.passwordWrap}>
              <TextInput
                style={[s.input, s.passwordInput]}
                value={password}
                onChangeText={setPassword}
                placeholder="••••••••"
                placeholderTextColor={colors.textMuted}
                secureTextEntry={!showPass}
                autoCapitalize="none"
              />
              <TouchableOpacity style={s.eyeBtn} onPress={() => setShowPass(!showPass)}>
                <Text style={s.eyeIcon}>{showPass ? '🙈' : '👁'}</Text>
              </TouchableOpacity>
            </View>
          </View>

          <TouchableOpacity
            style={[s.btn, loading && s.btnDisabled]}
            onPress={handleLogin}
            disabled={loading}
            activeOpacity={0.85}>
            {loading
              ? <ActivityIndicator color="#fff" />
              : <Text style={s.btnText}>Đăng nhập</Text>}
          </TouchableOpacity>

          <Text style={s.hint}>
            Chưa có tài khoản?{'\n'}
            Vui lòng liên hệ quản trị viên để nhận link kích hoạt.
          </Text>
        </View>

      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const s = StyleSheet.create({
  flex:         { flex: 1, backgroundColor: colors.navy },
  container:    { flexGrow: 1, paddingHorizontal: spacing.lg, paddingBottom: spacing.xl },

  header:       { alignItems: 'center', paddingTop: 72, paddingBottom: spacing.xl },
  logoWrap:     {
    width: 64, height: 64, borderRadius: 18,
    backgroundColor: colors.gold,
    alignItems: 'center', justifyContent: 'center',
    marginBottom: spacing.md,
    shadowColor: colors.gold, shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.45, shadowRadius: 16, elevation: 12,
  },
  logoIcon:     { fontSize: 30, color: colors.navy },
  appName:      { fontSize: 26, fontWeight: '700', color: colors.ivory, marginBottom: 4 },
  appSub:       { fontSize: 13, color: 'rgba(250,248,244,0.5)' },

  card:         {
    backgroundColor: colors.white,
    borderRadius: radius.lg, padding: spacing.lg,
    shadowColor: '#000', shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.15, shadowRadius: 24, elevation: 10,
  },
  cardTitle:    { ...typography.h2, marginBottom: 4 },
  cardSub:      { ...typography.small, marginBottom: spacing.md },

  errorBox:     {
    backgroundColor: 'rgba(220,38,38,0.08)',
    borderWidth: 1, borderColor: 'rgba(220,38,38,0.2)',
    borderRadius: radius.sm, padding: spacing.sm, marginBottom: spacing.md,
  },
  errorText:    { color: colors.danger, fontSize: 13 },

  field:        { marginBottom: spacing.md },
  label:        { ...typography.small, fontWeight: '600', marginBottom: 6, color: colors.textPrimary },
  input:        {
    borderWidth: 1.5, borderColor: colors.border, borderRadius: radius.sm,
    paddingHorizontal: spacing.md, paddingVertical: 12,
    fontSize: 15, color: colors.textPrimary, backgroundColor: colors.white,
  },
  passwordWrap: { position: 'relative' },
  passwordInput:{ paddingRight: 48 },
  eyeBtn:       { position: 'absolute', right: 12, top: 12 },
  eyeIcon:      { fontSize: 18 },

  btn:          {
    backgroundColor: colors.navy, borderRadius: radius.sm,
    paddingVertical: 14, alignItems: 'center', marginTop: spacing.sm,
    shadowColor: colors.navy, shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3, shadowRadius: 10, elevation: 6,
  },
  btnDisabled:  { opacity: 0.6 },
  btnText:      { color: '#fff', fontSize: 16, fontWeight: '700' },

  hint:         { textAlign: 'center', color: colors.textMuted, fontSize: 12, marginTop: spacing.md, lineHeight: 18 },

  roleBadgeRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: spacing.md },
  backBtn:      { padding: 4 },
  backText:     { fontSize: 13, color: colors.info, fontWeight: '500' },
  roleBadge:    {
    flexDirection: 'row', alignItems: 'center', gap: 5,
    paddingHorizontal: 10, paddingVertical: 5,
    borderRadius: radius.full, borderWidth: 1,
  },
  roleBadgeIcon:  { fontSize: 14 },
  roleBadgeLabel: { fontSize: 12, fontWeight: '700' },
});
