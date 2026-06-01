import React from 'react';
import {
  Modal, View, Text, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, ActivityIndicator,
} from 'react-native';

interface Props {
  visible:     boolean;
  title:       string;
  onClose:     () => void;
  children:    React.ReactNode;
  accent?:     string;
  submitLabel?: string;
  onSubmit?:   () => void;
  submitting?:  boolean;
}

export default function BottomSheet({
  visible, title, onClose, children,
  accent = '#2563eb', submitLabel = 'Lưu', onSubmit, submitting,
}: Props) {
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <TouchableOpacity style={s.overlay} activeOpacity={1} onPress={onClose} />
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={s.wrapper}
      >
        <View style={s.sheet}>
          {/* Handle */}
          <View style={s.handle} />

          {/* Header */}
          <View style={s.header}>
            <Text style={s.title}>{title}</Text>
            <TouchableOpacity style={s.closeBtn} onPress={onClose}>
              <Text style={s.closeX}>✕</Text>
            </TouchableOpacity>
          </View>

          {/* Content */}
          <ScrollView style={s.body} showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled">
            {children}
            <View style={{ height: 16 }} />
          </ScrollView>

          {/* Footer */}
          {onSubmit && (
            <View style={s.footer}>
              <TouchableOpacity style={s.cancelBtn} onPress={onClose}>
                <Text style={s.cancelText}>Hủy</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[s.submitBtn, { backgroundColor: accent }, submitting && { opacity: 0.6 }]}
                onPress={onSubmit}
                disabled={submitting}
              >
                {submitting
                  ? <ActivityIndicator color="#fff" size="small" />
                  : <Text style={s.submitText}>{submitLabel}</Text>
                }
              </TouchableOpacity>
            </View>
          )}
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}

/* ── Field helpers used in forms ────────────────────────────── */
export function FieldLabel({ children }: { children: string }) {
  return <Text style={f.label}>{children}</Text>;
}

export function FieldInput({
  value, onChange, placeholder, keyboardType, multiline,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  keyboardType?: 'default' | 'numeric' | 'decimal-pad';
  multiline?: boolean;
}) {
  const { TextInput } = require('react-native');
  return (
    <TextInput
      style={[f.input, multiline && f.inputMulti]}
      value={value}
      onChangeText={onChange}
      placeholder={placeholder}
      placeholderTextColor="#94a3b8"
      keyboardType={keyboardType ?? 'default'}
      multiline={multiline}
      numberOfLines={multiline ? 3 : 1}
    />
  );
}

export function FieldSelect({
  value, onChange, options, placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  options: { label: string; value: string }[];
  placeholder?: string;
}) {
  const { ScrollView: SV, TouchableOpacity: TO, Text: T } = require('react-native');
  return (
    <SV horizontal showsHorizontalScrollIndicator={false} style={{ marginBottom: 4 }}>
      {options.map(opt => (
        <TO
          key={opt.value}
          style={[f.chip, value === opt.value && f.chipActive]}
          onPress={() => onChange(opt.value)}
        >
          <T style={[f.chipText, value === opt.value && f.chipTextActive]}>
            {opt.label}
          </T>
        </TO>
      ))}
    </SV>
  );
}

export function ErrorMsg({ text }: { text?: string }) {
  if (!text) return null;
  return (
    <View style={f.errBox}>
      <Text style={f.errText}>⚠ {text}</Text>
    </View>
  );
}

const s = StyleSheet.create({
  overlay: { flex: 1, backgroundColor: 'rgba(0,0,0,0.45)' },
  wrapper: { position: 'absolute', bottom: 0, left: 0, right: 0 },
  sheet: {
    backgroundColor: '#fff',
    borderTopLeftRadius: 24, borderTopRightRadius: 24,
    maxHeight: '90%',
  },
  handle:  { width: 40, height: 4, borderRadius: 2, backgroundColor: '#e2e8f0', alignSelf: 'center', marginTop: 10, marginBottom: 4 },
  header:  { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingHorizontal: 20, paddingVertical: 14, borderBottomWidth: 1, borderBottomColor: '#f1f5f9' },
  title:   { fontSize: 16, fontWeight: '800', color: '#1e293b' },
  closeBtn:{ width: 32, height: 32, borderRadius: 10, backgroundColor: '#f1f5f9', alignItems: 'center', justifyContent: 'center' },
  closeX:  { fontSize: 14, color: '#64748b', fontWeight: '700' },
  body:    { paddingHorizontal: 20, paddingTop: 16 },
  footer:  { flexDirection: 'row', gap: 12, padding: 16, paddingBottom: 28, borderTopWidth: 1, borderTopColor: '#f1f5f9' },
  cancelBtn:  { flex: 1, paddingVertical: 13, borderRadius: 12, backgroundColor: '#f1f5f9', alignItems: 'center' },
  cancelText: { fontSize: 14, fontWeight: '700', color: '#64748b' },
  submitBtn:  { flex: 2, paddingVertical: 13, borderRadius: 12, alignItems: 'center' },
  submitText: { fontSize: 14, fontWeight: '800', color: '#fff' },
});

const f = StyleSheet.create({
  label: { fontSize: 12, fontWeight: '700', color: '#64748b', textTransform: 'uppercase', letterSpacing: 0.5, marginBottom: 8, marginTop: 14 },
  input: {
    borderWidth: 1.5, borderColor: '#e2e8f0', borderRadius: 12,
    paddingHorizontal: 14, paddingVertical: 11,
    fontSize: 15, color: '#1e293b', backgroundColor: '#fafafa',
  },
  inputMulti: { height: 80, textAlignVertical: 'top' },
  chip:         { paddingHorizontal: 14, paddingVertical: 8, borderRadius: 20, backgroundColor: '#f1f5f9', marginRight: 8 },
  chipActive:   { backgroundColor: '#1e293b' },
  chipText:     { fontSize: 13, fontWeight: '600', color: '#64748b' },
  chipTextActive:{ color: '#fff' },
  errBox: { backgroundColor: '#fef2f2', borderRadius: 10, padding: 12, marginTop: 8 },
  errText:{ fontSize: 13, color: '#dc2626' },
});
