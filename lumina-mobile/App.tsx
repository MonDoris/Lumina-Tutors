import React, { useState } from 'react';
import { ActivityIndicator, View } from 'react-native';
import { AuthProvider, useAuth } from './src/context/AuthContext';
import LandingScreen        from './src/screens/LandingScreen';
import RoleSelectScreen     from './src/screens/auth/RoleSelectScreen';
import LoginScreen          from './src/screens/auth/LoginScreen';
import StudentHomeScreen    from './src/screens/student/StudentHomeScreen';
import TeacherHomeScreen    from './src/screens/teacher/TeacherHomeScreen';
import ParentHomeScreen     from './src/screens/parent/ParentHomeScreen';
import SupervisorHomeScreen from './src/screens/supervisor/SupervisorHomeScreen';
import { colors } from './src/theme';

function RootApp() {
  const { user, isLoading } = useAuth();
  const [landed, setLanded] = useState(false);
  const [selectedRole, setSelectedRole] = useState<string | null>(null);

  if (isLoading) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.navy }}>
        <ActivityIndicator color={colors.gold} size="large" />
      </View>
    );
  }

  if (!user) {
    if (!landed) {
      return <LandingScreen onStart={() => setLanded(true)} />;
    }
    if (!selectedRole) {
      return <RoleSelectScreen onSelect={setSelectedRole} />;
    }
    return (
      <LoginScreen
        selectedRole={selectedRole}
        onBack={() => setSelectedRole(null)}
      />
    );
  }

  switch (user.roleCode) {
    case 'STUDENT':    return <StudentHomeScreen    />;
    case 'TEACHER':    return <TeacherHomeScreen    />;
    case 'PARENT':     return <ParentHomeScreen     />;
    case 'SUPERVISOR': return <SupervisorHomeScreen />;
    default:           return <TeacherHomeScreen    />;
  }
}

export default function App() {
  return (
    <AuthProvider>
      <RootApp />
    </AuthProvider>
  );
}
