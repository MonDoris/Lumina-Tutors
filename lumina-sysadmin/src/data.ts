export type NodeStatus = 'healthy' | 'degraded' | 'critical';
export type Tone = 'jade' | 'bronze' | 'son' | 'ivory';

export interface DrumNode {
  id: string;
  label: string;
  ring: 1 | 2 | 3;
  angle: number;
  status: NodeStatus;
  module: string;
}

export interface SysAlert {
  severity: 'critical' | 'warning';
  title: string;
  detail: string;
  source: string;
  timestamp: string;
}

export interface ModuleDef {
  code: string;
  title: string;
  intro: string;
  stats: { label: string; value: string; tone?: Tone }[];
  spark?: { title: string; unit: string; points: number[] };
  lists: { title: string; rows: { primary: string; secondary: string; tone?: Tone }[] }[];
  alerts: SysAlert[];
}

export interface Branch {
  id: string | null;
  label: string;
  leaves: string[];
}

/* Vòng 1: Core — Vòng 2: Nghiệp vụ — Vòng 3: Hạ tầng */
export const NODES: DrumNode[] = [
  { id: 'auth', label: 'Auth', ring: 1, angle: 0, status: 'healthy', module: 'security' },
  { id: 'api', label: 'API', ring: 1, angle: 90, status: 'healthy', module: 'servers' },
  { id: 'db', label: 'SQL', ring: 1, angle: 180, status: 'healthy', module: 'database' },
  { id: 'cache', label: 'Cache', ring: 1, angle: 270, status: 'healthy', module: 'servers' },
  { id: 'users', label: 'Users', ring: 2, angle: 36, status: 'healthy', module: 'users' },
  { id: 'class', label: 'Class', ring: 2, angle: 108, status: 'healthy', module: 'services' },
  { id: 'fin', label: 'Finance', ring: 2, angle: 180, status: 'healthy', module: 'services' },
  { id: 'ai', label: 'AI Tutor', ring: 2, angle: 252, status: 'healthy', module: 'services' },
  { id: 'lab', label: 'Lab', ring: 2, angle: 324, status: 'healthy', module: 'services' },
  { id: 'queue', label: 'Queue', ring: 3, angle: 20, status: 'degraded', module: 'servers' },
  { id: 'cdn', label: 'CDN', ring: 3, angle: 95, status: 'healthy', module: 'servers' },
  { id: 'mail', label: 'Mail', ring: 3, angle: 160, status: 'critical', module: 'servers' },
  { id: 'logs', label: 'Logs', ring: 3, angle: 230, status: 'healthy', module: 'logs' },
  { id: 'backup', label: 'Backup', ring: 3, angle: 300, status: 'healthy', module: 'database' },
];

export const FLOWS: [string, string][] = [
  ['auth', 'users'],
  ['api', 'class'],
  ['db', 'fin'],
  ['cache', 'ai'],
  ['api', 'lab'],
  ['db', 'backup'],
  ['auth', 'mail'],
  ['api', 'queue'],
  ['db', 'logs'],
];

export const BRANCHES: Branch[] = [
  { id: null, label: 'Tổng quan', leaves: [] },
  { id: 'users', label: 'Người dùng', leaves: ['Tài khoản', 'Vai trò', 'Lời mời'] },
  { id: 'security', label: 'Bảo mật', leaves: ['Phiên đăng nhập', 'IP bị chặn', 'Chính sách'] },
  { id: 'database', label: 'Cơ sở dữ liệu', leaves: ['Bảng dữ liệu', 'Sao lưu', 'Migration'] },
  { id: 'servers', label: 'Máy chủ', leaves: ['Node', 'Hàng đợi', 'CDN'] },
  { id: 'services', label: 'Nghiệp vụ', leaves: ['Lớp học', 'Học phí', 'AI Tutor'] },
  { id: 'logs', label: 'Nhật ký', leaves: ['Lỗi', 'Audit', 'Truy cập'] },
];

export const MODULES: Record<string, ModuleDef> = {
  users: {
    code: 'users',
    title: 'Người dùng',
    intro: 'Toàn cảnh tài khoản trên mọi trường thành viên — ai đang học, ai đang dạy, ai đang canh giữ.',
    stats: [
      { label: 'Tổng tài khoản', value: '4 218', tone: 'ivory' },
      { label: 'Đang hoạt động', value: '312', tone: 'jade' },
      { label: 'Mới trong tuần', value: '87', tone: 'jade' },
      { label: 'Bị khóa', value: '5', tone: 'son' },
    ],
    spark: { title: 'Phiên đăng nhập 24 giờ', unit: 'phiên', points: [42, 38, 30, 24, 28, 61, 148, 290, 310, 262, 240, 275, 301, 288, 250, 262, 295, 312, 280, 210, 150, 96, 70, 52] },
    lists: [
      {
        title: 'Đăng ký gần nhất',
        rows: [
          { primary: 'Trần Minh Anh — Học sinh', secondary: 'THCS Nguyễn Du · 13:47', tone: 'jade' },
          { primary: 'Lê Thu Hà — Phụ huynh', secondary: 'Liên kết HS Lê Gia Bảo · 13:21', tone: 'ivory' },
          { primary: 'Phạm Quốc Duy — Giáo viên', secondary: 'Chờ duyệt hồ sơ · 11:58', tone: 'bronze' },
          { primary: 'Ngô Hải Yến — Học sinh', secondary: 'THPT Lê Quý Đôn · 10:33', tone: 'jade' },
        ],
      },
      {
        title: 'Phân bổ vai trò',
        rows: [
          { primary: 'Student', secondary: '3 570 tài khoản', tone: 'jade' },
          { primary: 'Parent', secondary: '380 tài khoản', tone: 'ivory' },
          { primary: 'Teacher', secondary: '214 tài khoản', tone: 'ivory' },
          { primary: 'Supervisor / Accountant / Admin', secondary: '54 tài khoản', tone: 'bronze' },
        ],
      },
    ],
    alerts: [
      { severity: 'warning', title: '3 hồ sơ giáo viên chờ duyệt quá 48 giờ', detail: 'HR queue tồn đọng từ 01/07', source: 'hr.approvals', timestamp: '09:12:44' },
    ],
  },
  security: {
    code: 'security',
    title: 'Bảo mật',
    intro: 'Lá chắn của cả hệ thống — phiên đăng nhập, khóa cửa và những kẻ gõ sai mật khẩu.',
    stats: [
      { label: 'Phiên hiện tại', value: '312', tone: 'jade' },
      { label: 'Thất bại 24h', value: '47', tone: 'bronze' },
      { label: 'IP bị chặn', value: '9', tone: 'son' },
      { label: 'Token hết hạn', value: '3', tone: 'ivory' },
    ],
    spark: { title: 'Đăng nhập thất bại 24 giờ', unit: 'lần', points: [1, 0, 2, 1, 0, 1, 3, 5, 4, 2, 3, 6, 9, 4, 2, 1, 2, 3, 2, 1, 1, 0, 1, 2] },
    lists: [
      {
        title: 'Cảnh báo đăng nhập',
        rows: [
          { primary: '5 lần thất bại — gv.hoa@lumina.vn', secondary: 'IP 118.70.··· · 14:02', tone: 'son' },
          { primary: 'Đăng nhập từ thiết bị mới — admin.kv', secondary: 'Windows · Hà Nội · 13:15', tone: 'bronze' },
          { primary: 'Token refresh bất thường', secondary: 'mobile-app v2.1 · 12:40', tone: 'bronze' },
        ],
      },
      {
        title: 'Chính sách đang hiệu lực',
        rows: [
          { primary: 'Cookie 8 giờ · SameSite=Strict', secondary: 'Sliding expiration bật', tone: 'jade' },
          { primary: 'QR điểm danh hết hạn 10 phút', secondary: 'Theo cấu hình hệ thống', tone: 'jade' },
          { primary: 'Link mời hết hạn 3 ngày', secondary: 'Theo cấu hình hệ thống', tone: 'jade' },
        ],
      },
    ],
    alerts: [
      { severity: 'critical', title: 'Nghi vấn dò mật khẩu', detail: '23 lần thất bại từ dải IP 118.70.···/24 trong 15 phút', source: 'auth.bruteforce', timestamp: '14:02:17' },
      { severity: 'warning', title: 'JWT secret vẫn là placeholder', detail: 'Cần thay trước khi triển khai production', source: 'config.audit', timestamp: '08:00:00' },
    ],
  },
  database: {
    code: 'database',
    title: 'Cơ sở dữ liệu',
    intro: 'Trầm tích của tri thức — nơi mọi điểm số, buổi học và giao dịch lắng xuống thành tầng địa chất.',
    stats: [
      { label: 'Độ trễ truy vấn', value: '8 ms', tone: 'jade' },
      { label: 'Kích thước', value: '42 GB', tone: 'ivory' },
      { label: 'Kết nối', value: '36/100', tone: 'jade' },
      { label: 'Sao lưu gần nhất', value: '02:00', tone: 'jade' },
    ],
    spark: { title: 'Độ trễ truy vấn 24 giờ', unit: 'ms', points: [7, 8, 6, 6, 7, 9, 14, 22, 18, 12, 10, 11, 13, 12, 9, 8, 10, 12, 11, 9, 8, 7, 7, 8] },
    lists: [
      {
        title: 'Bảng lớn nhất',
        rows: [
          { primary: 'Attendances', secondary: '12,4 GB · 41,2 triệu dòng', tone: 'ivory' },
          { primary: 'Grades', secondary: '8,1 GB · 28,7 triệu dòng', tone: 'ivory' },
          { primary: 'Messages', secondary: '6,7 GB · 19,3 triệu dòng', tone: 'ivory' },
          { primary: 'AuditLogs', secondary: '5,9 GB · 22,1 triệu dòng', tone: 'ivory' },
        ],
      },
      {
        title: 'Chuỗi sao lưu',
        rows: [
          { primary: 'Full backup — 03/07 02:00', secondary: '41,8 GB · thành công', tone: 'jade' },
          { primary: 'Diff backup — 02/07 14:00', secondary: '1,2 GB · thành công', tone: 'jade' },
          { primary: 'Log backup — mỗi 15 phút', secondary: 'Chuỗi liền mạch 42 ngày', tone: 'jade' },
        ],
      },
    ],
    alerts: [
      { severity: 'warning', title: 'Index phân mảnh 34%', detail: 'IX_Attendances_SchoolId_Date cần rebuild', source: 'sql.maintenance', timestamp: '06:30:12' },
    ],
  },
  servers: {
    code: 'servers',
    title: 'Máy chủ',
    intro: 'Xương sống hạ tầng — từng node, từng hàng đợi, từng nhịp thở của phần cứng.',
    stats: [
      { label: 'Uptime', value: '42 ngày', tone: 'jade' },
      { label: 'CPU', value: '34%', tone: 'jade' },
      { label: 'RAM', value: '6,2/16 GB', tone: 'jade' },
      { label: 'Req/s', value: '1 284', tone: 'ivory' },
    ],
    spark: { title: 'Requests mỗi giây 24 giờ', unit: 'req/s', points: [180, 140, 110, 90, 120, 380, 940, 1450, 1380, 1210, 1284, 1310, 1420, 1290, 1180, 1220, 1350, 1400, 1150, 820, 560, 390, 260, 200] },
    lists: [
      {
        title: 'Tình trạng node',
        rows: [
          { primary: 'WEB-01 · WEB-02', secondary: 'Khỏe mạnh · cân bằng tải 52/48', tone: 'jade' },
          { primary: 'QUEUE-01', secondary: 'Trễ 240 ms — vượt ngưỡng 200 ms', tone: 'bronze' },
          { primary: 'MAIL-01', secondary: 'Mất phản hồi từ 14:02', tone: 'son' },
          { primary: 'CDN edge', secondary: '99,98% cache hit', tone: 'jade' },
        ],
      },
    ],
    alerts: [
      { severity: 'critical', title: 'Mail-01 mất phản hồi', detail: 'SMTP timeout ×3 liên tiếp — thông báo phụ huynh đang tồn đọng', source: 'mail.smtp', timestamp: '14:02:17' },
      { severity: 'warning', title: 'Hàng đợi trễ 240 ms', detail: 'queue.notify vượt ngưỡng 200 ms trong 12 phút', source: 'queue.notify', timestamp: '13:58:41' },
    ],
  },
  services: {
    code: 'services',
    title: 'Nghiệp vụ',
    intro: 'Nhịp sống giáo dục đang chảy qua hệ thống — lớp học, học phí, AI Tutor và phòng lab ảo.',
    stats: [
      { label: 'Lớp đang mở', value: '128', tone: 'jade' },
      { label: 'Học phí tháng này', value: '1,84 tỷ ₫', tone: 'bronze' },
      { label: 'Phiên AI Tutor', value: '1 042', tone: 'jade' },
      { label: 'Lab đang chạy', value: '17', tone: 'ivory' },
    ],
    spark: { title: 'Phiên AI Tutor 24 giờ', unit: 'phiên', points: [12, 8, 5, 3, 6, 18, 52, 96, 88, 74, 81, 102, 118, 95, 84, 91, 112, 121, 98, 71, 48, 32, 21, 15] },
    lists: [
      {
        title: 'Gói thuê bao',
        rows: [
          { primary: 'Premium — 84 trường', secondary: 'Gia hạn tự động bật', tone: 'jade' },
          { primary: 'Standard — 132 trường', secondary: '12 gói hết hạn trong 7 ngày', tone: 'bronze' },
          { primary: 'Trial — 41 trường', secondary: '8 gói chuyển đổi tuần này', tone: 'ivory' },
        ],
      },
      {
        title: 'Hoạt động nổi bật',
        rows: [
          { primary: 'Điểm danh QR', secondary: '18 240 lượt hôm nay', tone: 'jade' },
          { primary: 'Bảng điểm phát hành', secondary: '312 lớp trong tuần', tone: 'ivory' },
          { primary: 'Tin nhắn phụ huynh', secondary: '4 108 tin trong ngày', tone: 'ivory' },
        ],
      },
    ],
    alerts: [
      { severity: 'warning', title: '12 gói Standard sắp hết hạn', detail: 'Chưa có yêu cầu gia hạn — nhắc kế toán trước 10/07', source: 'subscription.renewal', timestamp: '07:45:00' },
    ],
  },
  logs: {
    code: 'logs',
    title: 'Nhật ký',
    intro: 'Ký ức của hệ thống — mọi sự kiện đều để lại vết khắc, như hoa văn trên mặt trống.',
    stats: [
      { label: 'Sự kiện hôm nay', value: '18 402', tone: 'ivory' },
      { label: 'Lỗi', value: '23', tone: 'son' },
      { label: 'Cảnh báo', value: '141', tone: 'bronze' },
      { label: 'Audit', value: '2 274', tone: 'jade' },
    ],
    spark: { title: 'Sự kiện mỗi giờ', unit: 'sự kiện', points: [220, 180, 150, 130, 160, 420, 980, 1540, 1480, 1310, 1384, 1410, 1520, 1390, 1280, 1320, 1450, 1500, 1250, 920, 660, 490, 360, 300] },
    lists: [
      {
        title: 'Dòng gần nhất',
        rows: [
          { primary: '[ERR] SmtpClient.Send — timeout 30s', secondary: 'MAIL-01 · 14:02:17', tone: 'son' },
          { primary: '[WRN] Queue latency 240ms > 200ms', secondary: 'QUEUE-01 · 13:58:41', tone: 'bronze' },
          { primary: '[INF] Đăng nhập SYSADMIN thành công', secondary: 'admin.kv · 13:15:02', tone: 'jade' },
          { primary: '[AUD] Cập nhật gói Premium — Trường THCS Nguyễn Du', secondary: 'accountant.tl · 11:47:55', tone: 'ivory' },
        ],
      },
    ],
    alerts: [],
  },
};
