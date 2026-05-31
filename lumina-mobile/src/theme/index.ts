export const colors = {
  navy:        '#0f1c2e',
  navyLight:   '#1a2d45',
  gold:        '#c9a84c',
  goldLight:   '#e2c97e',
  ivory:       '#faf8f4',
  white:       '#fefefe',
  textPrimary: '#1f2937',
  textMuted:   '#6b7280',
  border:      '#e5e7eb',
  success:     '#16a34a',
  danger:      '#dc2626',
  warning:     '#d97706',
  info:        '#2563eb',
  bg:          '#f3f4f6',
};

export const spacing = {
  xs: 4, sm: 8, md: 16, lg: 24, xl: 32,
};

export const radius = {
  sm: 8, md: 12, lg: 16, full: 9999,
};

export const typography = {
  h1:    { fontSize: 24, fontWeight: '700' as const, color: colors.navy },
  h2:    { fontSize: 20, fontWeight: '700' as const, color: colors.navy },
  h3:    { fontSize: 17, fontWeight: '600' as const, color: colors.textPrimary },
  body:  { fontSize: 15, fontWeight: '400' as const, color: colors.textPrimary },
  small: { fontSize: 13, fontWeight: '400' as const, color: colors.textMuted },
  label: { fontSize: 11, fontWeight: '600' as const, color: colors.textMuted, letterSpacing: 0.8, textTransform: 'uppercase' as const },
};
