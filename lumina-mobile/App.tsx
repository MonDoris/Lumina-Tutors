import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { AuthProvider, useAuth } from './src/context/AuthContext';
import LoginScreen          from './src/screens/auth/LoginScreen';
import StudentHomeScreen    from './src/screens/student/StudentHomeScreen';
import TeacherHomeScreen    from './src/screens/teacher/TeacherHomeScreen';
import ParentHomeScreen     from './src/screens/parent/ParentHomeScreen';
import SupervisorHomeScreen from './src/screens/supervisor/SupervisorHomeScreen';
import { colors } from './src/theme';

const Stack = createNativeStackNavigator();

function RootNavigator() {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.navy }}>
        <ActivityIndicator color={colors.gold} size="large" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Stack.Navigator screenOptions={{ headerShown: false }}>
        {!user ? (
          <Stack.Screen name="Login" component={LoginScreen} />
        ) : user.roleCode === 'STUDENT' ? (
          <Stack.Screen name="StudentHome" component={StudentHomeScreen} />
        ) : user.roleCode === 'TEACHER' || user.roleCode === 'ADMIN' ? (
          <Stack.Screen name="TeacherHome" component={TeacherHomeScreen} />
        ) : user.roleCode === 'PARENT' ? (
          <Stack.Screen name="ParentHome" component={ParentHomeScreen} />
        ) : user.roleCode === 'SUPERVISOR' ? (
          <Stack.Screen name="SupervisorHome" component={SupervisorHomeScreen} />
        ) : (
          <Stack.Screen name="Login" component={LoginScreen} />
        )}
      </Stack.Navigator>
    </NavigationContainer>
  );
}

export default function App() {
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <RootNavigator />
      </AuthProvider>
    </SafeAreaProvider>
  );
}
