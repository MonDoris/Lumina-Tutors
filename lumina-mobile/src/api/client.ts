import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

export const BASE_URL = 'http://192.168.0.2:60481';

export const api = axios.create({
  baseURL: `${BASE_URL}/api`,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use(async (config) => {
  const token = await AsyncStorage.getItem('jwt_token');
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
  grades:         (semesterId: number) => api.get(`/mobile/student/grades?semesterId=${semesterId}`),
  attendance:     (semesterId: number) => api.get(`/mobile/student/attendance?semesterId=${semesterId}`),
  scanQR:         (qrToken: string)    => api.post('/mobile/student/scan-qr', { qrToken }),
  courses:        ()                   => api.get('/mobile/student/courses'),
  homework:       (saId: number)       => api.get(`/mobile/student/homework/${saId}`),
  aiSessions:     ()                   => api.get('/mobile/student/ai-tutor/sessions'),
  createSession:  (title: string)      => api.post('/mobile/student/ai-tutor/sessions', { title }),
  getMessages:    (sid: number)        => api.get(`/mobile/student/ai-tutor/${sid}/messages`),
  sendMessage:    (sid: number, content: string) =>
    api.post(`/mobile/student/ai-tutor/${sid}/messages`, { content }),
};

// ── Teacher ───────────────────────────────────────────────────────────────────
export const teacherApi = {
  classes:            (academicYearId: number) => api.get(`/mobile/teacher/classes?academicYearId=${academicYearId}`),
  subjectAssignments: ()                        => api.get('/mobile/teacher/subject-assignments'),
  gradeBook:          (saId: number)            => api.get(`/mobile/teacher/gradebook/${saId}`),
  gradeCategories:    ()                        => api.get('/mobile/teacher/grade-categories'),
  schedules:          (saId: number)            => api.get(`/mobile/teacher/schedules?subjectAssignmentId=${saId}`),
  enterScore:         (body: object)            => api.post('/mobile/teacher/enter-score', body),
  bulkEnterScores:    (body: object)            => api.post('/mobile/teacher/bulk-enter-scores', body),
  calculateAverages:  (saId: number)            => api.post(`/mobile/teacher/calculate-averages/${saId}`, {}),
  attendanceSessions: (classId: number, date?: string) =>
    api.get(`/mobile/teacher/attendance-sessions?classId=${classId}${date ? `&date=${date}` : ''}`),
  getSession:         (sessionId: number)       => api.get(`/mobile/teacher/sessions/${sessionId}`),
  createSession:      (body: object)            => api.post('/mobile/teacher/attendance-sessions', body),
  updateAttendance:   (sessionId: number, body: object) =>
    api.patch(`/mobile/teacher/attendance-sessions/${sessionId}/record`, body),
  notifyAbsent:       (sessionId: number)       => api.post(`/mobile/teacher/sessions/${sessionId}/notify`, {}),
  homework:           ()                        => api.get('/mobile/teacher/homework'),
};

// ── Parent ────────────────────────────────────────────────────────────────────
export const parentApi = {
  children:        ()                                      => api.get('/mobile/parent/children'),
  childGrades:     (studentId: number, semesterId: number) =>
    api.get(`/mobile/parent/child-grades?studentId=${studentId}&semesterId=${semesterId}`),
  childAttendance: (studentId: number, semesterId: number) =>
    api.get(`/mobile/parent/child-attendance?studentId=${studentId}&semesterId=${semesterId}`),
  childCourses:    (studentId: number)                     =>
    api.get(`/mobile/parent/child-courses?studentId=${studentId}`),
  childHomework:   (studentId: number, saId: number)       =>
    api.get(`/mobile/parent/child-homework?studentId=${studentId}&subjectAssignmentId=${saId}`),
};

// ── Supervisor ────────────────────────────────────────────────────────────────
export const supervisorApi = {
  discipline:       (studentId?: number) =>
    api.get(`/mobile/supervisor/discipline${studentId ? `?studentId=${studentId}` : ''}`),
  dailyReport:      (date?: string) =>
    api.get(`/mobile/supervisor/daily-report${date ? `?date=${date}` : ''}`),
  students:         ()                             => api.get('/mobile/supervisor/students'),
  createViolation:  (body: object)                 => api.post('/mobile/supervisor/violations', body),
  resolveViolation: (id: number, actionTaken: string) =>
    api.post(`/mobile/supervisor/violations/${id}/resolve`, { actionTaken }),
  escalateViolation:(id: number, escalateToUserId: number) =>
    api.post(`/mobile/supervisor/violations/${id}/escalate`, { escalateToUserId }),
  gateCheck:        (body: object)                 => api.post('/mobile/supervisor/gate-check', body),
};

// ── Common ────────────────────────────────────────────────────────────────────
export const commonApi = {
  notifications: (page = 1) => api.get(`/mobile/notifications?page=${page}`),
  semesters:     ()         => api.get('/mobile/semesters'),
  academicYears: ()         => api.get('/mobile/academic-years'),
};
