import axios from 'axios';
import AsyncStorage from '@react-native-async-storage/async-storage';

// ── Cấu hình kết nối ──────────────────────────────────────────────────────────
// Chọn 1 tùy môi trường:
//
// [D] Cloudflare Tunnel — KHUYẾN NGHỊ để chạy & ĐỒNG BỘ MỌI MẠNG (cùng/khác wifi, 4G).
//     Trên laptop chạy:   cloudflared tunnel --url http://localhost:60481
//     → nhận URL https://xxxxx.trycloudflare.com, dán vào dòng dưới (KHÔNG có "/" cuối).
//     Lưu ý: URL đổi mỗi lần chạy lại tunnel (trừ khi dùng tunnel có tên/domain riêng).
// export const BASE_URL = 'https://xxxxx.trycloudflare.com';
//
// [A] Laptop Hotspot (chỉ chạy khi điện thoại nối CÙNG hotspot laptop):
export const BASE_URL = 'http://192.168.137.1:60481';
//
// [B] Radmin VPN (cần cài Radmin VPN app trên điện thoại):
// export const BASE_URL = 'http://26.42.69.95:60481';
//
// [C] WiFi cùng mạng (không dùng WiFi trường vì AP Isolation):
// export const BASE_URL = 'http://10.21.3.60:60481';

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

// Khi token hết hạn / không hợp lệ → server trả 401. Xoá phiên & báo app
// đăng nhập lại (tránh hiển thị "Request failed with status code 401" khó hiểu).
let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(fn: (() => void) | null) { onUnauthorized = fn; }

api.interceptors.response.use(
  (res) => res,
  async (err) => {
    const url: string = err?.config?.url ?? '';
    if (err?.response?.status === 401 && !url.includes('/auth/login')) {
      await AsyncStorage.removeItem('jwt_token');
      await AsyncStorage.removeItem('user_data');
      onUnauthorized?.();
    }
    return Promise.reject(err);
  },
);

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

// ── Online Classroom ──────────────────────────────────────────────────────────
export const onlineApi = {
  sessions:       ()                 => api.get('/mobile/online-sessions'),
  joinByCode:     (roomCode: string) => api.post('/mobile/online-sessions/join', { roomCode }),
  chatHistory:    (sessionId: number)=> api.get(`/mobile/online-sessions/${sessionId}/chat`),
  // Tạo bridge code ngắn hạn để mở WebView (tránh truyền JWT dài qua URL)
  webviewToken:   ()                 => api.post('/mobile/webview-token'),
};

// ── Virtual Lab ───────────────────────────────────────────────────────────────
export const virtualLabApi = {
  sessions: () => api.get('/mobile/virtual-lab/sessions'),
};

// ── Holographic Nexus (phòng 3D thời gian thực) ─────────────────────────────
export const nexusApi = {
  createRoom: ()             => api.post('/mobile/nexus/room'),          // giáo viên
  joinByCode: (room: string) => api.post('/mobile/nexus/join', { roomCode: room }),
  webviewToken: ()           => api.post('/mobile/webview-token'),
};

// ── Common ────────────────────────────────────────────────────────────────────
export const commonApi = {
  notifications: (page = 1) => api.get(`/mobile/notifications?page=${page}`),
  semesters:     ()         => api.get('/mobile/semesters'),
  academicYears: ()         => api.get('/mobile/academic-years'),
};
