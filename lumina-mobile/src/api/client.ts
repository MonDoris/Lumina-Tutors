import axios from 'axios';
import * as SecureStore from 'expo-secure-store';

export const BASE_URL = 'http://192.168.0.3:60481';

export const api = axios.create({
  baseURL: `${BASE_URL}/api`,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

// Tự động đính JWT vào mỗi request
api.interceptors.request.use(async (config) => {
  const token = await SecureStore.getItemAsync('jwt_token');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// ── Auth ──────────────────────────────────────────────────────────────────────
export const authApi = {
  login: (email: string, password: string) =>
    api.post('/auth/login', { email, password }),

  me: () => api.get('/auth/me'),
};

// ── Student ───────────────────────────────────────────────────────────────────
export const studentApi = {
  grades:     (semesterId: number) => api.get(`/mobile/student/grades?semesterId=${semesterId}`),
  attendance: (semesterId: number) => api.get(`/mobile/student/attendance?semesterId=${semesterId}`),
};

// ── Teacher ───────────────────────────────────────────────────────────────────
export const teacherApi = {
  classes:       (academicYearId: number) => api.get(`/mobile/teacher/classes?academicYearId=${academicYearId}`),
  gradeBook:     (subjectAssignmentId: number) => api.get(`/mobile/teacher/gradebook/${subjectAssignmentId}`),
  attendanceSessions: (classId: number, date?: string) =>
    api.get(`/mobile/teacher/attendance-sessions?classId=${classId}${date ? `&date=${date}` : ''}`),
};

// ── Parent ────────────────────────────────────────────────────────────────────
export const parentApi = {
  childGrades:     (studentId: number, semesterId: number) =>
    api.get(`/mobile/parent/child-grades?studentId=${studentId}&semesterId=${semesterId}`),
  childAttendance: (studentId: number, semesterId: number) =>
    api.get(`/mobile/parent/child-attendance?studentId=${studentId}&semesterId=${semesterId}`),
};

// ── Supervisor ────────────────────────────────────────────────────────────────
export const supervisorApi = {
  discipline:  (studentId?: number) =>
    api.get(`/mobile/supervisor/discipline${studentId ? `?studentId=${studentId}` : ''}`),
  dailyReport: (date?: string) =>
    api.get(`/mobile/supervisor/daily-report${date ? `?date=${date}` : ''}`),
};

// ── Common ────────────────────────────────────────────────────────────────────
export const commonApi = {
  notifications: (page = 1)    => api.get(`/mobile/notifications?page=${page}`),
  semesters:     ()            => api.get('/mobile/semesters'),
  academicYears: ()            => api.get('/mobile/academic-years'),
};
