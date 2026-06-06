import React, { createContext, useContext, useState, useEffect } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { authApi } from '../api/client';

const MOBILE_ROLES = ['STUDENT', 'TEACHER', 'PARENT', 'SUPERVISOR'];

interface User {
  userId: number;
  fullName: string;
  email: string;
  roleCode: string;   // TEACHER | STUDENT | PARENT | SUPERVISOR | ADMIN | ACCOUNTANT
  schoolId: number;
  schoolName: string;
  avatarUrl?: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<string | null>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user,      setUser]      = useState<User | null>(null);
  const [token,     setToken]     = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Khôi phục session khi khởi động app
  useEffect(() => {
    (async () => {
      try {
        const savedToken = await AsyncStorage.getItem('jwt_token');
        const savedUser  = await AsyncStorage.getItem('user_data');
        if (savedToken && savedUser) {
          setToken(savedToken);
          setUser(JSON.parse(savedUser));
        }
      } catch {}
      finally { setIsLoading(false); }
    })();
  }, []);

  const login = async (email: string, password: string): Promise<string | null> => {
    try {
      const res = await authApi.login(email, password);
      const data = res.data;

      if (!MOBILE_ROLES.includes(data.roleCode)) {
        return `Tài khoản "${data.roleCode}" không được hỗ trợ trên ứng dụng di động. Vui lòng liên hệ quản trị viên.`;
      }

      await AsyncStorage.setItem('jwt_token', data.token);
      await AsyncStorage.setItem('user_data', JSON.stringify({
        userId:     data.userId,
        fullName:   data.fullName,
        email:      data.email,
        roleCode:   data.roleCode,
        schoolId:   data.schoolId,
        schoolName: data.schoolName,
        avatarUrl:  data.avatarUrl,
      }));

      setToken(data.token);
      setUser({ userId: data.userId, fullName: data.fullName, email: data.email,
                roleCode: data.roleCode, schoolId: data.schoolId,
                schoolName: data.schoolName, avatarUrl: data.avatarUrl });
      return null; // null = thành công
    } catch (err: any) {
      if (!err?.response) {
        return 'Không thể kết nối đến máy chủ. Kiểm tra kết nối mạng và thử lại.';
      }
      return err?.response?.data?.message ?? 'Email hoặc mật khẩu không đúng.';
    }
  };

  const logout = async () => {
    await AsyncStorage.removeItem('jwt_token');
    await AsyncStorage.removeItem('user_data');
    setToken(null);
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, token, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
};
