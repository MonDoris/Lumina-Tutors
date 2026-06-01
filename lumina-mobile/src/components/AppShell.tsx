import React, { useRef, useState } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet,
  SafeAreaView, StatusBar, Dimensions, Animated,
} from 'react-native';
import { useAuth } from '../context/AuthContext';

const { width } = Dimensions.get('window');

export interface TabConfig {
  key:     string;
  label:   string;
  icon:    string;          // emoji or text icon
  content: React.ReactNode;
}

interface Props {
  tabs:        TabConfig[];
  accentColor: string;
  roleLabel:   string;
}

export default function AppShell({ tabs, accentColor, roleLabel }: Props) {
  const { user, logout }        = useAuth();
  const [active, setActive]     = useState(0);
  const indicatorX              = useRef(new Animated.Value(0)).current;
  const tabW                    = width / tabs.length;

  const switchTab = (i: number) => {
    Animated.spring(indicatorX, {
      toValue: i * tabW + tabW / 2 - 20,
      useNativeDriver: true,
      damping: 18, stiffness: 180,
    }).start();
    setActive(i);
  };

  const initials = user?.fullName
    ? user.fullName.split(' ').filter(Boolean).slice(-2).map(w => w[0]).join('')
    : '?';

  return (
    <SafeAreaView style={s.root}>
      <StatusBar barStyle="light-content" backgroundColor="#0b1628" />

      {/* ── Top Header ────────────────────────────────── */}
      <View style={s.header}>
        <View style={s.headerLeft}>
          <View style={[s.roleBadge, { borderColor: accentColor + '60' }]}>
            <Text style={[s.roleBadgeText, { color: accentColor }]}>{roleLabel}</Text>
          </View>
          <Text style={s.headerName} numberOfLines={1}>{user?.fullName}</Text>
          <Text style={s.headerSchool} numberOfLines={1}>{user?.schoolName}</Text>
        </View>
        <TouchableOpacity style={[s.avatarBtn, { backgroundColor: accentColor }]} onPress={logout}>
          <Text style={s.avatarText}>{initials}</Text>
        </TouchableOpacity>
      </View>

      {/* ── Content ───────────────────────────────────── */}
      <View style={s.content}>
        {tabs[active].content}
      </View>

      {/* ── Bottom Tab Bar ────────────────────────────── */}
      <View style={s.tabBar}>
        <Animated.View
          style={[
            s.tabIndicator,
            { backgroundColor: accentColor, transform: [{ translateX: indicatorX }] },
          ]}
        />
        {tabs.map((tab, i) => {
          const isActive = i === active;
          return (
            <TouchableOpacity
              key={tab.key}
              style={s.tabItem}
              onPress={() => switchTab(i)}
              activeOpacity={0.7}
            >
              <Text style={[s.tabIcon, isActive && { opacity: 1 }]}>{tab.icon}</Text>
              <Text style={[s.tabLabel, isActive && { color: accentColor, fontWeight: '700' }]}>
                {tab.label}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </SafeAreaView>
  );
}

const s = StyleSheet.create({
  root:          { flex: 1, backgroundColor: '#f0f4f8' },

  header: {
    backgroundColor: '#0b1628',
    paddingHorizontal: 20,
    paddingTop: 14,
    paddingBottom: 16,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  headerLeft:    { flex: 1, marginRight: 12 },
  roleBadge: {
    alignSelf: 'flex-start',
    borderWidth: 1,
    borderRadius: 20,
    paddingHorizontal: 10,
    paddingVertical: 2,
    marginBottom: 4,
  },
  roleBadgeText: { fontSize: 10, fontWeight: '800', letterSpacing: 0.8, textTransform: 'uppercase' },
  headerName:    { fontSize: 17, fontWeight: '800', color: '#faf8f4', letterSpacing: 0.1 },
  headerSchool:  { fontSize: 11, color: 'rgba(250,248,244,0.4)', marginTop: 1 },
  avatarBtn: {
    width: 44, height: 44, borderRadius: 14,
    alignItems: 'center', justifyContent: 'center',
    shadowColor: '#000', shadowOffset: { width: 0, height: 3 },
    shadowOpacity: 0.25, shadowRadius: 6, elevation: 6,
  },
  avatarText:    { fontSize: 15, fontWeight: '800', color: '#0b1628' },

  content:       { flex: 1 },

  tabBar: {
    flexDirection: 'row',
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#e8edf2',
    paddingBottom: 6,
    paddingTop: 4,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: -2 },
    shadowOpacity: 0.06,
    shadowRadius: 8,
    elevation: 10,
  },
  tabIndicator: {
    position: 'absolute',
    top: 0, width: 40, height: 3,
    borderBottomLeftRadius: 2, borderBottomRightRadius: 2,
  },
  tabItem:       { flex: 1, alignItems: 'center', paddingTop: 8, paddingBottom: 2 },
  tabIcon:       { fontSize: 20, opacity: 0.45, marginBottom: 3 },
  tabLabel:      { fontSize: 10, fontWeight: '600', color: '#94a3b8', letterSpacing: 0.3 },
});
