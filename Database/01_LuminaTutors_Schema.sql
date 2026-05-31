-- ============================================================
--  LUMINA TUTORS — Education Management System
--  Database: SQL Server 2019+
--  Version : 1.0.0
--  Standard: Thông tư 22/2021/TT-BGDĐT | Clean Architecture
--  Author  : Lumina Tutors Dev Team
--  Created : 2025
-- ============================================================
-- Execution order: Run this single file top-to-bottom.
-- Dependencies   : None (self-contained).
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'LuminaTutorsDB')
BEGIN
    CREATE DATABASE LuminaTutorsDB
        COLLATE Vietnamese_CI_AS;
END
GO

USE LuminaTutorsDB;
GO

-- ============================================================
-- SECTION 1 — IDENTITY, SCHOOLS & AUTHORIZATION
-- ============================================================

-- ----------------------------------------------------------
-- 1.1  Schools  (Multi-tenant root entity)
-- ----------------------------------------------------------
CREATE TABLE Schools (
    SchoolId        INT             NOT NULL IDENTITY(1,1),
    SchoolCode      NVARCHAR(20)    NOT NULL,           -- Mã trường (VD: LT-HCM-001)
    SchoolName      NVARCHAR(200)   NOT NULL,
    Address         NVARCHAR(500)   NULL,
    Province        NVARCHAR(100)   NULL,               -- Tỉnh / TP
    PhoneNumber     NVARCHAR(20)    NULL,
    Email           NVARCHAR(150)   NULL,
    LogoUrl         NVARCHAR(500)   NULL,
    WebsiteUrl      NVARCHAR(300)   NULL,
    LicenseCode     NVARCHAR(100)   NULL,               -- Mã cấp phép của Sở GD&ĐT
    IsActive        BIT             NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Schools PRIMARY KEY (SchoolId),
    CONSTRAINT UQ_Schools_Code UNIQUE (SchoolCode)
);
GO

-- ----------------------------------------------------------
-- 1.2  Roles  (6 system roles — fixed enum)
-- ----------------------------------------------------------
CREATE TABLE Roles (
    RoleId          TINYINT         NOT NULL IDENTITY(1,1),
    RoleName        NVARCHAR(50)    NOT NULL,           -- Admin | Teacher | Student | Parent | Supervisor | Accountant
    RoleCode        VARCHAR(30)     NOT NULL,           -- ADMIN | TEACHER | STUDENT | PARENT | SUPERVISOR | ACCOUNTANT
    Description     NVARCHAR(300)   NULL,
    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_Code UNIQUE (RoleCode)
);
GO

-- ----------------------------------------------------------
-- 1.3  Users  (Central identity table — all roles share this)
-- ----------------------------------------------------------
CREATE TABLE Users (
    UserId          INT             NOT NULL IDENTITY(1,1),
    SchoolId        INT             NOT NULL,           -- Tenant boundary
    RoleId          TINYINT         NOT NULL,
    Email           NVARCHAR(150)   NOT NULL,
    PasswordHash    NVARCHAR(500)   NOT NULL,
    FullName        NVARCHAR(150)   NOT NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    AvatarUrl       NVARCHAR(500)   NULL,
    IsActive        BIT             NOT NULL DEFAULT 1,
    IsEmailVerified BIT             NOT NULL DEFAULT 0,
    LastLoginAt     DATETIME2(0)    NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email_School UNIQUE (Email, SchoolId),   -- Email duy nhất per school
    CONSTRAINT FK_Users_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Users_Roles  FOREIGN KEY (RoleId)   REFERENCES Roles(RoleId)
);
GO

-- ----------------------------------------------------------
-- 1.4  RefreshTokens  (JWT refresh token management)
-- ----------------------------------------------------------
CREATE TABLE RefreshTokens (
    TokenId         BIGINT          NOT NULL IDENTITY(1,1),
    UserId          INT             NOT NULL,
    Token           NVARCHAR(500)   NOT NULL,
    ExpiresAt       DATETIME2(0)    NOT NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    RevokedAt       DATETIME2(0)    NULL,
    ReplacedByToken NVARCHAR(500)   NULL,
    CreatedByIp     NVARCHAR(50)    NULL,
    RevokedByIp     NVARCHAR(50)    NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (TokenId),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------
-- 1.5  InviteLinks  (Admin tạo link mời cho Parent / Student)
-- ----------------------------------------------------------
CREATE TABLE InviteLinks (
    InviteId        INT             NOT NULL IDENTITY(1,1),
    SchoolId        INT             NOT NULL,
    Token           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    TargetRoleId    TINYINT         NOT NULL,           -- Role sẽ được cấp khi kích hoạt
    TargetEmail     NVARCHAR(150)   NULL,               -- NULL = open link (bất kỳ ai)
    CreatedByUserId INT             NOT NULL,           -- Admin tạo
    LinkedStudentId INT             NULL,               -- Nếu mời PH → gắn sẵn Student
    ExpiresAt       DATETIME2(0)    NOT NULL,
    UsedAt          DATETIME2(0)    NULL,
    UsedByUserId    INT             NULL,
    IsRevoked       BIT             NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_InviteLinks PRIMARY KEY (InviteId),
    CONSTRAINT UQ_InviteLinks_Token UNIQUE (Token),
    CONSTRAINT FK_InviteLinks_Schools  FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT FK_InviteLinks_Roles    FOREIGN KEY (TargetRoleId)    REFERENCES Roles(RoleId),
    CONSTRAINT FK_InviteLinks_Creator  FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT FK_InviteLinks_Student  FOREIGN KEY (LinkedStudentId) REFERENCES Users(UserId)
);
GO


-- ============================================================
-- SECTION 2 — ACADEMIC STRUCTURE
-- ============================================================

-- ----------------------------------------------------------
-- 2.1  AcademicYears  (Năm học, VD: 2024-2025)
-- ----------------------------------------------------------
CREATE TABLE AcademicYears (
    AcademicYearId  INT             NOT NULL IDENTITY(1,1),
    SchoolId        INT             NOT NULL,
    YearName        NVARCHAR(20)    NOT NULL,           -- VD: "2024-2025"
    StartDate       DATE            NOT NULL,
    EndDate         DATE            NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 0, -- Chỉ 1 năm học active mỗi lúc
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AcademicYears PRIMARY KEY (AcademicYearId),
    CONSTRAINT UQ_AcademicYears_Name_School UNIQUE (SchoolId, YearName),
    CONSTRAINT FK_AcademicYears_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId),
    CONSTRAINT CK_AcademicYears_Dates CHECK (EndDate > StartDate)
);
GO

-- ----------------------------------------------------------
-- 2.2  Semesters  (Học kỳ — 2 HK / năm học)
-- ----------------------------------------------------------
CREATE TABLE Semesters (
    SemesterId      INT             NOT NULL IDENTITY(1,1),
    AcademicYearId  INT             NOT NULL,
    SchoolId        INT             NOT NULL,
    SemesterNumber  TINYINT         NOT NULL,           -- 1 hoặc 2
    SemesterName    NVARCHAR(50)    NOT NULL,           -- VD: "Học kỳ 1 (2024-2025)"
    StartDate       DATE            NOT NULL,
    EndDate         DATE            NOT NULL,
    IsActive        BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_Semesters PRIMARY KEY (SemesterId),
    CONSTRAINT UQ_Semesters_School_Year_Num UNIQUE (SchoolId, AcademicYearId, SemesterNumber),
    CONSTRAINT FK_Semesters_AcademicYears FOREIGN KEY (AcademicYearId) REFERENCES AcademicYears(AcademicYearId),
    CONSTRAINT FK_Semesters_Schools       FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT CK_Semesters_Number        CHECK (SemesterNumber IN (1, 2)),
    CONSTRAINT CK_Semesters_Dates         CHECK (EndDate > StartDate)
);
GO

-- ----------------------------------------------------------
-- 2.3  GradeLevels  (Khối lớp: Lớp 1 → Lớp 12)
-- ----------------------------------------------------------
CREATE TABLE GradeLevels (
    GradeLevelId    INT             NOT NULL IDENTITY(1,1),
    SchoolId        INT             NOT NULL,
    GradeNumber     TINYINT         NOT NULL,           -- 1..12
    GradeName       NVARCHAR(20)    NOT NULL,           -- VD: "Lớp 10"
    EducationLevel  VARCHAR(20)     NOT NULL,           -- TIEU_HOC | THCS | THPT
    CONSTRAINT PK_GradeLevels PRIMARY KEY (GradeLevelId),
    CONSTRAINT UQ_GradeLevels_School_Num UNIQUE (SchoolId, GradeNumber),
    CONSTRAINT FK_GradeLevels_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId),
    CONSTRAINT CK_GradeLevels_Number CHECK (GradeNumber BETWEEN 1 AND 12),
    CONSTRAINT CK_GradeLevels_EduLevel CHECK (EducationLevel IN ('TIEU_HOC', 'THCS', 'THPT'))
);
GO

-- ----------------------------------------------------------
-- 2.4  Subjects  (Môn học)
-- ----------------------------------------------------------
CREATE TABLE Subjects (
    SubjectId       INT             NOT NULL IDENTITY(1,1),
    SchoolId        INT             NOT NULL,
    SubjectCode     NVARCHAR(20)    NOT NULL,           -- VD: TOAN, VAN, ANH, HOA, SU
    SubjectName     NVARCHAR(100)   NOT NULL,           -- VD: "Toán học", "Ngữ văn"
    SubjectCategory VARCHAR(20)     NOT NULL DEFAULT 'MAIN',  -- MAIN | ELECTIVE | EXTRACURRICULAR
    Has3DLab        BIT             NOT NULL DEFAULT 0, -- Có phòng học 3D không
    IsActive        BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_Subjects PRIMARY KEY (SubjectId),
    CONSTRAINT UQ_Subjects_School_Code UNIQUE (SchoolId, SubjectCode),
    CONSTRAINT FK_Subjects_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId)
);
GO

-- ----------------------------------------------------------
-- 2.5  Classes  (Lớp học cụ thể, VD: 10A1 — năm học 2024-2025)
-- ----------------------------------------------------------
CREATE TABLE Classes (
    ClassId             INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    AcademicYearId      INT             NOT NULL,
    GradeLevelId        INT             NOT NULL,
    ClassName           NVARCHAR(20)    NOT NULL,       -- VD: "10A1"
    HomeRoomTeacherId   INT             NULL,           -- GVCN (FK → Users)
    MaxStudents         TINYINT         NOT NULL DEFAULT 40,
    RoomNumber          NVARCHAR(20)    NULL,           -- Phòng học mặc định
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Classes PRIMARY KEY (ClassId),
    CONSTRAINT UQ_Classes_Name_Year_School UNIQUE (SchoolId, AcademicYearId, ClassName),
    CONSTRAINT FK_Classes_Schools         FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Classes_AcademicYears   FOREIGN KEY (AcademicYearId)    REFERENCES AcademicYears(AcademicYearId),
    CONSTRAINT FK_Classes_GradeLevels     FOREIGN KEY (GradeLevelId)      REFERENCES GradeLevels(GradeLevelId),
    CONSTRAINT FK_Classes_HomeRoomTeacher FOREIGN KEY (HomeRoomTeacherId)  REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 2.6  ClassEnrollments  (Học sinh ghi danh vào lớp)
-- ----------------------------------------------------------
CREATE TABLE ClassEnrollments (
    EnrollmentId    INT             NOT NULL IDENTITY(1,1),
    ClassId         INT             NOT NULL,
    StudentId       INT             NOT NULL,           -- FK → Users (role=Student)
    EnrolledDate    DATE            NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE),
    Status          VARCHAR(20)     NOT NULL DEFAULT 'ACTIVE',  -- ACTIVE | TRANSFERRED | WITHDRAWN
    TransferNote    NVARCHAR(500)   NULL,
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_ClassEnrollments PRIMARY KEY (EnrollmentId),
    CONSTRAINT UQ_ClassEnrollments_Class_Student UNIQUE (ClassId, StudentId),
    CONSTRAINT FK_ClassEnrollments_Classes  FOREIGN KEY (ClassId)   REFERENCES Classes(ClassId),
    CONSTRAINT FK_ClassEnrollments_Students FOREIGN KEY (StudentId) REFERENCES Users(UserId),
    CONSTRAINT CK_ClassEnrollments_Status CHECK (Status IN ('ACTIVE', 'TRANSFERRED', 'WITHDRAWN'))
);
GO

-- ----------------------------------------------------------
-- 2.7  SubjectAssignments  (Phân công dạy: GV ↔ Môn ↔ Lớp ↔ HK)
-- ----------------------------------------------------------
CREATE TABLE SubjectAssignments (
    AssignmentId    INT             NOT NULL IDENTITY(1,1),
    SchoolId        INT             NOT NULL,
    SemesterId      INT             NOT NULL,
    ClassId         INT             NOT NULL,
    SubjectId       INT             NOT NULL,
    TeacherId       INT             NOT NULL,           -- FK → Users (role=Teacher)
    PeriodsPerWeek  TINYINT         NOT NULL DEFAULT 2, -- Số tiết/tuần (ảnh hưởng số ĐTX tối thiểu)
    CreatedAt       DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_SubjectAssignments PRIMARY KEY (AssignmentId),
    CONSTRAINT UQ_SubjectAssignments UNIQUE (SemesterId, ClassId, SubjectId),
    CONSTRAINT FK_SubjectAssignments_Schools    FOREIGN KEY (SchoolId)   REFERENCES Schools(SchoolId),
    CONSTRAINT FK_SubjectAssignments_Semesters  FOREIGN KEY (SemesterId) REFERENCES Semesters(SemesterId),
    CONSTRAINT FK_SubjectAssignments_Classes    FOREIGN KEY (ClassId)    REFERENCES Classes(ClassId),
    CONSTRAINT FK_SubjectAssignments_Subjects   FOREIGN KEY (SubjectId)  REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_SubjectAssignments_Teachers   FOREIGN KEY (TeacherId)  REFERENCES Users(UserId)
);
GO


-- ============================================================
-- SECTION 3 — ROLE-SPECIFIC PROFILES
-- ============================================================

-- ----------------------------------------------------------
-- 3.1  StudentProfiles
-- ----------------------------------------------------------
CREATE TABLE StudentProfiles (
    StudentProfileId    INT             NOT NULL IDENTITY(1,1),
    UserId              INT             NOT NULL,
    SchoolId            INT             NOT NULL,
    StudentCode         NVARCHAR(30)    NOT NULL,       -- Mã học sinh (VD: LT-2024-0001)
    DateOfBirth         DATE            NULL,
    Gender              VARCHAR(10)     NULL,           -- MALE | FEMALE | OTHER
    PlaceOfBirth        NVARCHAR(200)   NULL,
    PermanentAddress    NVARCHAR(500)   NULL,
    EthnicGroup         NVARCHAR(50)    NULL,
    AdmissionDate       DATE            NULL,
    AdmissionType       VARCHAR(20)     NULL,           -- NEW | TRANSFER
    CONSTRAINT PK_StudentProfiles PRIMARY KEY (StudentProfileId),
    CONSTRAINT UQ_StudentProfiles_User UNIQUE (UserId),
    CONSTRAINT UQ_StudentProfiles_Code_School UNIQUE (SchoolId, StudentCode),
    CONSTRAINT FK_StudentProfiles_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_StudentProfiles_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId),
    CONSTRAINT CK_StudentProfiles_Gender  CHECK (Gender IN ('MALE', 'FEMALE', 'OTHER') OR Gender IS NULL)
);
GO

-- ----------------------------------------------------------
-- 3.2  TeacherProfiles
-- ----------------------------------------------------------
CREATE TABLE TeacherProfiles (
    TeacherProfileId    INT             NOT NULL IDENTITY(1,1),
    UserId              INT             NOT NULL,
    SchoolId            INT             NOT NULL,
    TeacherCode         NVARCHAR(30)    NOT NULL,       -- Mã giáo viên
    DateOfBirth         DATE            NULL,
    Gender              VARCHAR(10)     NULL,
    Qualification       NVARCHAR(100)   NULL,           -- Trình độ: Cử nhân, Thạc sĩ, Tiến sĩ
    SpecializationSubject NVARCHAR(100) NULL,           -- Môn chuyên
    HireDate            DATE            NULL,
    ContractType        VARCHAR(30)     NULL,           -- FULL_TIME | PART_TIME | CONTRACT
    TaxCode             NVARCHAR(20)    NULL,
    BankAccountNumber   NVARCHAR(50)    NULL,
    BankName            NVARCHAR(100)   NULL,
    CONSTRAINT PK_TeacherProfiles PRIMARY KEY (TeacherProfileId),
    CONSTRAINT UQ_TeacherProfiles_User UNIQUE (UserId),
    CONSTRAINT UQ_TeacherProfiles_Code_School UNIQUE (SchoolId, TeacherCode),
    CONSTRAINT FK_TeacherProfiles_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_TeacherProfiles_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId)
);
GO

-- ----------------------------------------------------------
-- 3.3  ParentProfiles
-- ----------------------------------------------------------
CREATE TABLE ParentProfiles (
    ParentProfileId     INT             NOT NULL IDENTITY(1,1),
    UserId              INT             NOT NULL,
    SchoolId            INT             NOT NULL,
    Occupation          NVARCHAR(100)   NULL,
    WorkAddress         NVARCHAR(300)   NULL,
    CONSTRAINT PK_ParentProfiles PRIMARY KEY (ParentProfileId),
    CONSTRAINT UQ_ParentProfiles_User UNIQUE (UserId),
    CONSTRAINT FK_ParentProfiles_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_ParentProfiles_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId)
);
GO

-- ----------------------------------------------------------
-- 3.4  ParentStudentRelations  (Many-to-Many PH ↔ HS)
-- ----------------------------------------------------------
CREATE TABLE ParentStudentRelations (
    RelationId          INT             NOT NULL IDENTITY(1,1),
    ParentUserId        INT             NOT NULL,
    StudentUserId       INT             NOT NULL,
    Relationship        NVARCHAR(30)    NOT NULL,       -- Cha | Mẹ | Người giám hộ
    IsPrimaryContact    BIT             NOT NULL DEFAULT 0,  -- Liên lạc chính
    CONSTRAINT PK_ParentStudentRelations PRIMARY KEY (RelationId),
    CONSTRAINT UQ_ParentStudentRelations UNIQUE (ParentUserId, StudentUserId),
    CONSTRAINT FK_PSR_Parents  FOREIGN KEY (ParentUserId)  REFERENCES Users(UserId),
    CONSTRAINT FK_PSR_Students FOREIGN KEY (StudentUserId) REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 3.5  SupervisorProfiles
-- ----------------------------------------------------------
CREATE TABLE SupervisorProfiles (
    SupervisorProfileId INT             NOT NULL IDENTITY(1,1),
    UserId              INT             NOT NULL,
    SchoolId            INT             NOT NULL,
    SupervisorCode      NVARCHAR(30)    NOT NULL,
    DateOfBirth         DATE            NULL,
    Gender              VARCHAR(10)     NULL,
    HireDate            DATE            NULL,
    CONSTRAINT PK_SupervisorProfiles PRIMARY KEY (SupervisorProfileId),
    CONSTRAINT UQ_SupervisorProfiles_User UNIQUE (UserId),
    CONSTRAINT UQ_SupervisorProfiles_Code_School UNIQUE (SchoolId, SupervisorCode),
    CONSTRAINT FK_SupervisorProfiles_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_SupervisorProfiles_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId)
);
GO

-- ----------------------------------------------------------
-- 3.6  AccountantProfiles
-- ----------------------------------------------------------
CREATE TABLE AccountantProfiles (
    AccountantProfileId INT             NOT NULL IDENTITY(1,1),
    UserId              INT             NOT NULL,
    SchoolId            INT             NOT NULL,
    AccountantCode      NVARCHAR(30)    NOT NULL,
    DateOfBirth         DATE            NULL,
    Gender              VARCHAR(10)     NULL,
    HireDate            DATE            NULL,
    CONSTRAINT PK_AccountantProfiles PRIMARY KEY (AccountantProfileId),
    CONSTRAINT UQ_AccountantProfiles_User UNIQUE (UserId),
    CONSTRAINT UQ_AccountantProfiles_Code_School UNIQUE (SchoolId, AccountantCode),
    CONSTRAINT FK_AccountantProfiles_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_AccountantProfiles_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId)
);
GO


-- ============================================================
-- SECTION 4 — SCHEDULES & ATTENDANCE (QR Model A)
-- ============================================================

-- ----------------------------------------------------------
-- 4.1  Schedules  (Thời khóa biểu — drag-drop per semester)
-- ----------------------------------------------------------
CREATE TABLE Schedules (
    ScheduleId          INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    SemesterId          INT             NOT NULL,
    SubjectAssignmentId INT             NOT NULL,       -- GV + Môn + Lớp đã được phân công
    DayOfWeek           TINYINT         NOT NULL,       -- 2=Thứ 2 .. 7=Thứ 7
    PeriodStart         TINYINT         NOT NULL,       -- Tiết bắt đầu (1..10)
    PeriodEnd           TINYINT         NOT NULL,       -- Tiết kết thúc
    StartTime           TIME(0)         NOT NULL,       -- Giờ thực tế
    EndTime             TIME(0)         NOT NULL,
    RoomOverride        NVARCHAR(20)    NULL,           -- Ghi đè phòng mặc định của lớp
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Schedules PRIMARY KEY (ScheduleId),
    CONSTRAINT FK_Schedules_Schools             FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Schedules_Semesters           FOREIGN KEY (SemesterId)        REFERENCES Semesters(SemesterId),
    CONSTRAINT FK_Schedules_SubjectAssignments  FOREIGN KEY (SubjectAssignmentId) REFERENCES SubjectAssignments(AssignmentId),
    CONSTRAINT CK_Schedules_DayOfWeek          CHECK (DayOfWeek BETWEEN 2 AND 7),
    CONSTRAINT CK_Schedules_Period             CHECK (PeriodStart <= PeriodEnd),
    CONSTRAINT CK_Schedules_Time              CHECK (EndTime > StartTime)
);
GO

-- ----------------------------------------------------------
-- 4.2  ScheduleChangeLogs  (Audit lịch thay đổi TKB)
-- ----------------------------------------------------------
CREATE TABLE ScheduleChangeLogs (
    ChangeLogId         INT             NOT NULL IDENTITY(1,1),
    ScheduleId          INT             NOT NULL,
    ChangedByUserId     INT             NOT NULL,
    ChangeType          VARCHAR(20)     NOT NULL,       -- CREATED | MODIFIED | CANCELLED | SUBSTITUTED
    OldDayOfWeek        TINYINT         NULL,
    OldPeriodStart      TINYINT         NULL,
    OldRoomNumber       NVARCHAR(20)    NULL,
    NewDayOfWeek        TINYINT         NULL,
    NewPeriodStart      TINYINT         NULL,
    NewRoomNumber       NVARCHAR(20)    NULL,
    Reason              NVARCHAR(500)   NULL,
    AppliedDate         DATE            NULL,           -- Ngày áp dụng thay đổi (nếu 1 buổi cụ thể)
    ChangedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_ScheduleChangeLogs PRIMARY KEY (ChangeLogId),
    CONSTRAINT FK_ScheduleChangeLogs_Schedules FOREIGN KEY (ScheduleId)      REFERENCES Schedules(ScheduleId),
    CONSTRAINT FK_ScheduleChangeLogs_Users     FOREIGN KEY (ChangedByUserId) REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 4.3  AttendanceSessions  (QR Model A: GV tạo QR mỗi buổi)
-- ----------------------------------------------------------
CREATE TABLE AttendanceSessions (
    SessionId           INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    ScheduleId          INT             NOT NULL,
    SessionDate         DATE            NOT NULL,       -- Ngày học thực tế
    QRToken             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),  -- Token duy nhất cho buổi
    QRExpiresAt         DATETIME2(0)    NOT NULL,       -- Thường 5-15 phút sau khi tạo
    CreatedByTeacherId  INT             NOT NULL,
    SessionStatus       VARCHAR(20)     NOT NULL DEFAULT 'OPEN',   -- OPEN | CLOSED | CANCELLED
    TopicNote           NVARCHAR(300)   NULL,           -- Chủ đề buổi học (ghi chú cho sổ đầu bài)
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    ClosedAt            DATETIME2(0)    NULL,
    CONSTRAINT PK_AttendanceSessions PRIMARY KEY (SessionId),
    CONSTRAINT UQ_AttendanceSessions_Schedule_Date UNIQUE (ScheduleId, SessionDate),
    CONSTRAINT UQ_AttendanceSessions_QRToken UNIQUE (QRToken),
    CONSTRAINT FK_AttendanceSessions_Schools  FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_AttendanceSessions_Schedules FOREIGN KEY (ScheduleId)       REFERENCES Schedules(ScheduleId),
    CONSTRAINT FK_AttendanceSessions_Teachers  FOREIGN KEY (CreatedByTeacherId) REFERENCES Users(UserId),
    CONSTRAINT CK_AttendanceSessions_Status    CHECK (SessionStatus IN ('OPEN', 'CLOSED', 'CANCELLED'))
);
GO

-- ----------------------------------------------------------
-- 4.4  Attendances  (Bản ghi điểm danh từng HS mỗi buổi)
-- ----------------------------------------------------------
CREATE TABLE Attendances (
    AttendanceId        INT             NOT NULL IDENTITY(1,1),
    SessionId           INT             NOT NULL,
    StudentId           INT             NOT NULL,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'ABSENT', -- PRESENT | ABSENT | LATE | EXCUSED
    CheckedInAt         DATETIME2(0)    NULL,           -- Thời điểm HS scan QR thành công
    CheckMethod         VARCHAR(20)     NULL,           -- QR_SCAN | MANUAL | FACE_RECOGNITION
    Note                NVARCHAR(300)   NULL,
    NotifiedParent      BIT             NOT NULL DEFAULT 0,
    NotifiedAt          DATETIME2(0)    NULL,
    UpdatedByTeacherId  INT             NULL,           -- GV override thủ công
    UpdatedAt           DATETIME2(0)    NULL,
    CONSTRAINT PK_Attendances PRIMARY KEY (AttendanceId),
    CONSTRAINT UQ_Attendances_Session_Student UNIQUE (SessionId, StudentId),
    CONSTRAINT FK_Attendances_Sessions  FOREIGN KEY (SessionId)  REFERENCES AttendanceSessions(SessionId),
    CONSTRAINT FK_Attendances_Students  FOREIGN KEY (StudentId)  REFERENCES Users(UserId),
    CONSTRAINT FK_Attendances_Teachers  FOREIGN KEY (UpdatedByTeacherId) REFERENCES Users(UserId),
    CONSTRAINT CK_Attendances_Status    CHECK (Status IN ('PRESENT', 'ABSENT', 'LATE', 'EXCUSED'))
);
GO

-- ----------------------------------------------------------
-- 4.5  SchoolEvents  (Sự kiện tập trung: chào cờ, ngoại khoá)
-- ----------------------------------------------------------
CREATE TABLE SchoolEvents (
    EventId             INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    EventName           NVARCHAR(200)   NOT NULL,
    EventType           VARCHAR(30)     NOT NULL,       -- ASSEMBLY | FIELD_TRIP | AWARD | SPORT | OTHER
    EventDate           DATE            NOT NULL,
    StartTime           TIME(0)         NULL,
    EndTime             TIME(0)         NULL,
    Location            NVARCHAR(200)   NULL,
    Description         NVARCHAR(1000)  NULL,
    OrganizedByUserId   INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_SchoolEvents PRIMARY KEY (EventId),
    CONSTRAINT FK_SchoolEvents_Schools FOREIGN KEY (SchoolId)           REFERENCES Schools(SchoolId),
    CONSTRAINT FK_SchoolEvents_Users   FOREIGN KEY (OrganizedByUserId)  REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 4.6  EventAttendances  (Điểm danh sự kiện — Giám thị quản lý)
-- ----------------------------------------------------------
CREATE TABLE EventAttendances (
    EventAttId          INT             NOT NULL IDENTITY(1,1),
    EventId             INT             NOT NULL,
    StudentId           INT             NOT NULL,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'ABSENT',
    CheckedByUserId     INT             NULL,           -- Giám thị scan
    CheckedAt           DATETIME2(0)    NULL,
    Note                NVARCHAR(300)   NULL,
    CONSTRAINT PK_EventAttendances PRIMARY KEY (EventAttId),
    CONSTRAINT UQ_EventAttendances_Event_Student UNIQUE (EventId, StudentId),
    CONSTRAINT FK_EventAttendances_Events   FOREIGN KEY (EventId)         REFERENCES SchoolEvents(EventId),
    CONSTRAINT FK_EventAttendances_Students FOREIGN KEY (StudentId)       REFERENCES Users(UserId),
    CONSTRAINT FK_EventAttendances_Checker  FOREIGN KEY (CheckedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_EventAttendances_Status   CHECK (Status IN ('PRESENT', 'ABSENT', 'LATE', 'EXCUSED'))
);
GO


-- ============================================================
-- SECTION 5 — LESSONS, ASSIGNMENTS & QUESTION BANK
-- ============================================================

-- ----------------------------------------------------------
-- 5.1  Lessons  (Bài giảng / Tài liệu học tập)
-- ----------------------------------------------------------
CREATE TABLE Lessons (
    LessonId            INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    SubjectAssignmentId INT             NOT NULL,
    Title               NVARCHAR(300)   NOT NULL,
    Description         NVARCHAR(2000)  NULL,
    ContentHtml         NVARCHAR(MAX)   NULL,           -- Nội dung rich-text
    LessonType          VARCHAR(20)     NOT NULL DEFAULT 'LECTURE', -- LECTURE | MATERIAL | 3D_LAB
    Is3DEnabled         BIT             NOT NULL DEFAULT 0,
    Lab3DConfig         NVARCHAR(MAX)   NULL,           -- JSON: { subject, experiment, mode }
    IsPublished         BIT             NOT NULL DEFAULT 0,
    PublishedAt         DATETIME2(0)    NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Lessons PRIMARY KEY (LessonId),
    CONSTRAINT FK_Lessons_Schools             FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Lessons_SubjectAssignments  FOREIGN KEY (SubjectAssignmentId) REFERENCES SubjectAssignments(AssignmentId)
);
GO

-- ----------------------------------------------------------
-- 5.2  LessonMaterials  (File đính kèm bài giảng)
-- ----------------------------------------------------------
CREATE TABLE LessonMaterials (
    MaterialId          INT             NOT NULL IDENTITY(1,1),
    LessonId            INT             NOT NULL,
    FileName            NVARCHAR(300)   NOT NULL,
    FileUrl             NVARCHAR(1000)  NOT NULL,       -- Storage URL (Azure Blob / S3)
    FileType            VARCHAR(20)     NOT NULL,       -- PDF | VIDEO | SLIDE | AUDIO | IMAGE | OTHER
    FileSizeKB          INT             NULL,
    SortOrder           TINYINT         NOT NULL DEFAULT 0,
    UploadedAt          DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_LessonMaterials PRIMARY KEY (MaterialId),
    CONSTRAINT FK_LessonMaterials_Lessons FOREIGN KEY (LessonId) REFERENCES Lessons(LessonId) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------
-- 5.3  QuestionBank  (Ngân hàng câu hỏi)
-- ----------------------------------------------------------
CREATE TABLE QuestionBank (
    QuestionId          INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    SubjectId           INT             NOT NULL,
    CreatedByTeacherId  INT             NOT NULL,
    QuestionText        NVARCHAR(MAX)   NOT NULL,
    QuestionType        VARCHAR(20)     NOT NULL,       -- MULTIPLE_CHOICE | TRUE_FALSE | SHORT_ANSWER | ESSAY | FILE_UPLOAD
    DifficultyLevel     VARCHAR(10)     NOT NULL DEFAULT 'MEDIUM', -- EASY | MEDIUM | HARD
    GradeLevelId        INT             NULL,           -- Phù hợp khối lớp nào
    ChapterTag          NVARCHAR(100)   NULL,           -- Chương/Bài trong SGK
    ExplanationText     NVARCHAR(MAX)   NULL,           -- Giải thích đáp án
    IsApproved          BIT             NOT NULL DEFAULT 0,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_QuestionBank PRIMARY KEY (QuestionId),
    CONSTRAINT FK_QuestionBank_Schools     FOREIGN KEY (SchoolId)           REFERENCES Schools(SchoolId),
    CONSTRAINT FK_QuestionBank_Subjects    FOREIGN KEY (SubjectId)          REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_QuestionBank_Teachers    FOREIGN KEY (CreatedByTeacherId) REFERENCES Users(UserId),
    CONSTRAINT FK_QuestionBank_GradeLevels FOREIGN KEY (GradeLevelId)       REFERENCES GradeLevels(GradeLevelId),
    CONSTRAINT CK_QuestionBank_Type        CHECK (QuestionType IN ('MULTIPLE_CHOICE','TRUE_FALSE','SHORT_ANSWER','ESSAY','FILE_UPLOAD')),
    CONSTRAINT CK_QuestionBank_Difficulty  CHECK (DifficultyLevel IN ('EASY','MEDIUM','HARD'))
);
GO

-- ----------------------------------------------------------
-- 5.4  QuestionOptions  (Các lựa chọn cho câu trắc nghiệm)
-- ----------------------------------------------------------
CREATE TABLE QuestionOptions (
    OptionId            INT             NOT NULL IDENTITY(1,1),
    QuestionId          INT             NOT NULL,
    OptionLabel         CHAR(1)         NOT NULL,       -- A | B | C | D
    OptionText          NVARCHAR(1000)  NOT NULL,
    IsCorrect           BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_QuestionOptions PRIMARY KEY (OptionId),
    CONSTRAINT UQ_QuestionOptions_Q_Label UNIQUE (QuestionId, OptionLabel),
    CONSTRAINT FK_QuestionOptions_Questions FOREIGN KEY (QuestionId) REFERENCES QuestionBank(QuestionId) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------
-- 5.5  Assignments  (Bài tập / Kiểm tra giao cho lớp)
-- ----------------------------------------------------------
CREATE TABLE Assignments (
    AssignmentId        INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    SubjectAssignmentId INT             NOT NULL,
    Title               NVARCHAR(300)   NOT NULL,
    Instructions        NVARCHAR(MAX)   NULL,
    AssignmentType      VARCHAR(20)     NOT NULL,       -- HOMEWORK | QUIZ | MIDTERM | FINAL | PROJECT
    GradeCategoryId     INT             NULL,           -- Loại điểm (TT22) — sẽ tạo ở Section 6
    MaxScore            DECIMAL(5,2)    NOT NULL DEFAULT 10.00,
    DueDate             DATETIME2(0)    NULL,
    AllowLateSubmission BIT             NOT NULL DEFAULT 0,
    LatePenaltyPercent  TINYINT         NOT NULL DEFAULT 0,
    IsPublished         BIT             NOT NULL DEFAULT 0,
    PublishedAt         DATETIME2(0)    NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Assignments PRIMARY KEY (AssignmentId),
    CONSTRAINT FK_Assignments_Schools             FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Assignments_SubjectAssignments  FOREIGN KEY (SubjectAssignmentId) REFERENCES SubjectAssignments(AssignmentId),
    CONSTRAINT CK_Assignments_MaxScore            CHECK (MaxScore > 0),
    CONSTRAINT CK_Assignments_Type               CHECK (AssignmentType IN ('HOMEWORK','QUIZ','MIDTERM','FINAL','PROJECT'))
);
GO

-- ----------------------------------------------------------
-- 5.6  AssignmentSubmissions  (Học sinh nộp bài)
-- ----------------------------------------------------------
CREATE TABLE AssignmentSubmissions (
    SubmissionId        INT             NOT NULL IDENTITY(1,1),
    AssignmentId        INT             NOT NULL,
    StudentId           INT             NOT NULL,
    SubmittedAt         DATETIME2(0)    NULL,
    IsLate              BIT             NOT NULL DEFAULT 0,
    AnswerText          NVARCHAR(MAX)   NULL,           -- Bài tự luận / câu trả lời ngắn
    Score               DECIMAL(5,2)    NULL,
    GradedAt            DATETIME2(0)    NULL,
    GradedByUserId      INT             NULL,
    Feedback            NVARCHAR(2000)  NULL,
    SubmissionStatus    VARCHAR(20)     NOT NULL DEFAULT 'DRAFT', -- DRAFT | SUBMITTED | GRADED | RETURNED
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AssignmentSubmissions PRIMARY KEY (SubmissionId),
    CONSTRAINT UQ_AssignmentSubmissions_Assign_Student UNIQUE (AssignmentId, StudentId),
    CONSTRAINT FK_AssignmentSubmissions_Assignments FOREIGN KEY (AssignmentId)    REFERENCES Assignments(AssignmentId),
    CONSTRAINT FK_AssignmentSubmissions_Students    FOREIGN KEY (StudentId)       REFERENCES Users(UserId),
    CONSTRAINT FK_AssignmentSubmissions_Grader      FOREIGN KEY (GradedByUserId)  REFERENCES Users(UserId),
    CONSTRAINT CK_AssignmentSubmissions_Status      CHECK (SubmissionStatus IN ('DRAFT','SUBMITTED','GRADED','RETURNED'))
);
GO

-- ----------------------------------------------------------
-- 5.7  SubmissionFiles  (File đính kèm bài nộp)
-- ----------------------------------------------------------
CREATE TABLE SubmissionFiles (
    FileId              INT             NOT NULL IDENTITY(1,1),
    SubmissionId        INT             NOT NULL,
    FileName            NVARCHAR(300)   NOT NULL,
    FileUrl             NVARCHAR(1000)  NOT NULL,
    FileType            VARCHAR(20)     NOT NULL,
    FileSizeKB          INT             NULL,
    UploadedAt          DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_SubmissionFiles PRIMARY KEY (FileId),
    CONSTRAINT FK_SubmissionFiles_Submissions FOREIGN KEY (SubmissionId) REFERENCES AssignmentSubmissions(SubmissionId) ON DELETE CASCADE
);
GO


-- ============================================================
-- SECTION 6 — GRADING (THÔNG TƯ 22/2021/TT-BGDĐT)
-- ============================================================
-- Thông tư 22 phân loại điểm số:
--   ĐTX (Thường xuyên) — Hệ số 1  — nhiều lần/HK
--   ĐGK (Giữa kỳ)      — Hệ số 2  — 1 lần/HK
--   ĐCK (Cuối kỳ)      — Hệ số 3  — 1 lần/HK
-- Công thức: ĐTBm = (ΣĐTXi + ĐGK×2 + ĐCK×3) / (n_TX + 2 + 3)
-- Số ĐTX tối thiểu phụ thuộc số tiết/tuần.
-- ============================================================

-- ----------------------------------------------------------
-- 6.1  GradeCategories  (Loại điểm — seed data cần thiết)
-- ----------------------------------------------------------
CREATE TABLE GradeCategories (
    GradeCategoryId     INT             NOT NULL IDENTITY(1,1),
    CategoryCode        VARCHAR(10)     NOT NULL,       -- DTX | DGK | DCK
    CategoryName        NVARCHAR(50)    NOT NULL,       -- Điểm thường xuyên | Điểm giữa kỳ | Điểm cuối kỳ
    Coefficient         TINYINT         NOT NULL,       -- 1 | 2 | 3
    MaxCountPerSemester TINYINT         NULL,           -- NULL = không giới hạn (ĐTX); 1 (ĐGK, ĐCK)
    IsMultipleAllowed   BIT             NOT NULL,       -- ĐTX = 1, ĐGK/ĐCK = 0
    CONSTRAINT PK_GradeCategories PRIMARY KEY (GradeCategoryId),
    CONSTRAINT UQ_GradeCategories_Code UNIQUE (CategoryCode)
);
GO

-- ----------------------------------------------------------
-- 6.2  SubjectGradeRequirements  (Số ĐTX tối thiểu theo môn & khối)
-- Theo TT22: ≥1 tiết/tuần → 2 ĐTX; ≥2 tiết → 3 ĐTX; ≥3 tiết → 4 ĐTX
-- ----------------------------------------------------------
CREATE TABLE SubjectGradeRequirements (
    RequirementId       INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    SubjectId           INT             NOT NULL,
    GradeLevelId        INT             NOT NULL,
    GradeCategoryId     INT             NOT NULL,
    MinCount            TINYINT         NOT NULL,       -- Số đầu điểm tối thiểu bắt buộc/HK
    CONSTRAINT PK_SubjectGradeRequirements PRIMARY KEY (RequirementId),
    CONSTRAINT UQ_SGR UNIQUE (SchoolId, SubjectId, GradeLevelId, GradeCategoryId),
    CONSTRAINT FK_SGR_Schools         FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT FK_SGR_Subjects        FOREIGN KEY (SubjectId)       REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_SGR_GradeLevels     FOREIGN KEY (GradeLevelId)    REFERENCES GradeLevels(GradeLevelId),
    CONSTRAINT FK_SGR_GradeCategories FOREIGN KEY (GradeCategoryId) REFERENCES GradeCategories(GradeCategoryId)
);
GO

-- ----------------------------------------------------------
-- 6.3  ScoreEntries  (Từng đầu điểm cụ thể — raw scores)
-- ----------------------------------------------------------
CREATE TABLE ScoreEntries (
    ScoreEntryId        INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    StudentId           INT             NOT NULL,
    SubjectAssignmentId INT             NOT NULL,       -- Xác định: Lớp + Môn + GV + HK
    GradeCategoryId     INT             NOT NULL,
    EntryOrder          TINYINT         NOT NULL DEFAULT 1,  -- Thứ tự ĐTX (ĐTX1, ĐTX2, ...)
    Score               DECIMAL(4,2)    NOT NULL,       -- 0.00 – 10.00
    ExamDate            DATE            NULL,
    Note                NVARCHAR(200)   NULL,
    EnteredByTeacherId  INT             NOT NULL,
    EnteredAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    IsLocked            BIT             NOT NULL DEFAULT 0,  -- Sau khi duyệt thì khóa
    CONSTRAINT PK_ScoreEntries PRIMARY KEY (ScoreEntryId),
    CONSTRAINT UQ_ScoreEntries UNIQUE (StudentId, SubjectAssignmentId, GradeCategoryId, EntryOrder),
    CONSTRAINT FK_ScoreEntries_Schools             FOREIGN KEY (SchoolId)           REFERENCES Schools(SchoolId),
    CONSTRAINT FK_ScoreEntries_Students            FOREIGN KEY (StudentId)          REFERENCES Users(UserId),
    CONSTRAINT FK_ScoreEntries_SubjectAssignments  FOREIGN KEY (SubjectAssignmentId) REFERENCES SubjectAssignments(AssignmentId),
    CONSTRAINT FK_ScoreEntries_GradeCategories     FOREIGN KEY (GradeCategoryId)    REFERENCES GradeCategories(GradeCategoryId),
    CONSTRAINT FK_ScoreEntries_Teachers            FOREIGN KEY (EnteredByTeacherId) REFERENCES Users(UserId),
    CONSTRAINT CK_ScoreEntries_Score               CHECK (Score BETWEEN 0.00 AND 10.00)
);
GO

-- ----------------------------------------------------------
-- 6.4  GradeBook  (Bảng điểm tổng kết — tính từ ScoreEntries)
-- Computed/stored bởi SP_CalculateSubjectAverage
-- ----------------------------------------------------------
CREATE TABLE GradeBook (
    GradeBookId         INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    StudentId           INT             NOT NULL,
    SubjectAssignmentId INT             NOT NULL,
    AverageScore        DECIMAL(4,2)    NULL,           -- ĐTBm — tính theo công thức TT22
    LetterGrade         NVARCHAR(10)    NULL,           -- A+ | A | B+ | B | C+ | C | D+ | D | F (THPT)
    Remark              VARCHAR(20)     NULL,           -- EXCELLENT | GOOD | AVERAGE | BELOW_AVG | FAIL
    IsCalculated        BIT             NOT NULL DEFAULT 0,
    CalculatedAt        DATETIME2(0)    NULL,
    IsLocked            BIT             NOT NULL DEFAULT 0,
    ApprovedByUserId    INT             NULL,
    ApprovedAt          DATETIME2(0)    NULL,
    CONSTRAINT PK_GradeBook PRIMARY KEY (GradeBookId),
    CONSTRAINT UQ_GradeBook_Student_Subject UNIQUE (StudentId, SubjectAssignmentId),
    CONSTRAINT FK_GradeBook_Schools             FOREIGN KEY (SchoolId)           REFERENCES Schools(SchoolId),
    CONSTRAINT FK_GradeBook_Students            FOREIGN KEY (StudentId)          REFERENCES Users(UserId),
    CONSTRAINT FK_GradeBook_SubjectAssignments  FOREIGN KEY (SubjectAssignmentId) REFERENCES SubjectAssignments(AssignmentId),
    CONSTRAINT FK_GradeBook_Approver            FOREIGN KEY (ApprovedByUserId)   REFERENCES Users(UserId),
    CONSTRAINT CK_GradeBook_Score               CHECK (AverageScore IS NULL OR AverageScore BETWEEN 0.00 AND 10.00)
);
GO

-- ----------------------------------------------------------
-- 6.5  Exams  (Kỳ thi / Kiểm tra tập trung)
-- ----------------------------------------------------------
CREATE TABLE Exams (
    ExamId              INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    SemesterId          INT             NOT NULL,
    SubjectId           INT             NOT NULL,
    GradeLevelId        INT             NOT NULL,
    ExamName            NVARCHAR(200)   NOT NULL,
    ExamType            VARCHAR(20)     NOT NULL,       -- MIDTERM | FINAL | MAKEUP | REGULAR
    ExamDate            DATE            NOT NULL,
    StartTime           TIME(0)         NOT NULL,
    DurationMinutes     SMALLINT        NOT NULL,
    MaxScore            DECIMAL(4,2)    NOT NULL DEFAULT 10.00,
    CreatedByUserId     INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Exams PRIMARY KEY (ExamId),
    CONSTRAINT FK_Exams_Schools      FOREIGN KEY (SchoolId)      REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Exams_Semesters    FOREIGN KEY (SemesterId)    REFERENCES Semesters(SemesterId),
    CONSTRAINT FK_Exams_Subjects     FOREIGN KEY (SubjectId)     REFERENCES Subjects(SubjectId),
    CONSTRAINT FK_Exams_GradeLevels  FOREIGN KEY (GradeLevelId)  REFERENCES GradeLevels(GradeLevelId),
    CONSTRAINT FK_Exams_Creator      FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Exams_Type         CHECK (ExamType IN ('MIDTERM','FINAL','MAKEUP','REGULAR'))
);
GO

-- ----------------------------------------------------------
-- 6.6  ExamRooms  (Phòng thi — Giám thị quản lý)
-- ----------------------------------------------------------
CREATE TABLE ExamRooms (
    ExamRoomId          INT             NOT NULL IDENTITY(1,1),
    ExamId              INT             NOT NULL,
    RoomName            NVARCHAR(50)    NOT NULL,       -- VD: "Phòng thi 01"
    Capacity            TINYINT         NOT NULL DEFAULT 30,
    SupervisorId        INT             NOT NULL,       -- Giám thị phụ trách phòng
    AssistantId         INT             NULL,           -- Giám thị phụ (nếu có)
    CONSTRAINT PK_ExamRooms PRIMARY KEY (ExamRoomId),
    CONSTRAINT FK_ExamRooms_Exams      FOREIGN KEY (ExamId)      REFERENCES Exams(ExamId),
    CONSTRAINT FK_ExamRooms_Supervisor FOREIGN KEY (SupervisorId) REFERENCES Users(UserId),
    CONSTRAINT FK_ExamRooms_Assistant  FOREIGN KEY (AssistantId)  REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 6.7  ExamRoomAssignments  (Sắp xếp chỗ ngồi ngẫu nhiên)
-- ----------------------------------------------------------
CREATE TABLE ExamRoomAssignments (
    AssignmentId        INT             NOT NULL IDENTITY(1,1),
    ExamRoomId          INT             NOT NULL,
    StudentId           INT             NOT NULL,
    SeatNumber          VARCHAR(10)     NOT NULL,       -- VD: "A1", "B3"
    CONSTRAINT PK_ExamRoomAssignments PRIMARY KEY (AssignmentId),
    CONSTRAINT UQ_ExamRoomAssignments_Room_Student UNIQUE (ExamRoomId, StudentId),
    CONSTRAINT UQ_ExamRoomAssignments_Room_Seat    UNIQUE (ExamRoomId, SeatNumber),
    CONSTRAINT FK_ExamRoomAssignments_Rooms    FOREIGN KEY (ExamRoomId) REFERENCES ExamRooms(ExamRoomId),
    CONSTRAINT FK_ExamRoomAssignments_Students FOREIGN KEY (StudentId)  REFERENCES Users(UserId)
);
GO


-- ============================================================
-- SECTION 7 — TUITION FEES & FINANCE (Kế toán module)
-- ============================================================

-- ----------------------------------------------------------
-- 7.1  TuitionFeeConfigs  (Cấu hình học phí theo khối/lớp/HK)
-- ----------------------------------------------------------
CREATE TABLE TuitionFeeConfigs (
    ConfigId            INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    AcademicYearId      INT             NOT NULL,
    GradeLevelId        INT             NULL,           -- NULL = áp dụng toàn trường
    FeeType             NVARCHAR(100)   NOT NULL,       -- "Học phí" | "Tiền xe" | "Bảo hiểm" ...
    Amount              DECIMAL(12,0)   NOT NULL,       -- VND
    DueDayOfMonth       TINYINT         NOT NULL DEFAULT 15,  -- Ngày đến hạn hàng tháng
    BillingCycle        VARCHAR(20)     NOT NULL DEFAULT 'MONTHLY',  -- MONTHLY | SEMESTER | YEARLY
    Description         NVARCHAR(500)   NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    CreatedByUserId     INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_TuitionFeeConfigs PRIMARY KEY (ConfigId),
    CONSTRAINT FK_TuitionFeeConfigs_Schools      FOREIGN KEY (SchoolId)       REFERENCES Schools(SchoolId),
    CONSTRAINT FK_TuitionFeeConfigs_AcademicYears FOREIGN KEY (AcademicYearId) REFERENCES AcademicYears(AcademicYearId),
    CONSTRAINT FK_TuitionFeeConfigs_GradeLevels  FOREIGN KEY (GradeLevelId)   REFERENCES GradeLevels(GradeLevelId),
    CONSTRAINT FK_TuitionFeeConfigs_Creator      FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 7.2  TuitionInvoices  (Hóa đơn học phí cho từng HS)
-- ----------------------------------------------------------
CREATE TABLE TuitionInvoices (
    InvoiceId           INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    StudentId           INT             NOT NULL,
    ConfigId            INT             NOT NULL,
    InvoiceCode         NVARCHAR(50)    NOT NULL,       -- Mã hóa đơn (VD: INV-2024-000001)
    BillingPeriod       NVARCHAR(20)    NOT NULL,       -- VD: "2024-09" (YYYY-MM) hoặc "HK1-2024-2025"
    Amount              DECIMAL(12,0)   NOT NULL,
    Discount            DECIMAL(12,0)   NOT NULL DEFAULT 0,
    FinalAmount         AS (Amount - Discount) PERSISTED, -- Tính tự động
    DueDate             DATE            NOT NULL,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'PENDING', -- PENDING | PAID | OVERDUE | CANCELLED | PARTIAL
    Notes               NVARCHAR(500)   NULL,
    QRCodeData          NVARCHAR(500)   NULL,           -- Dữ liệu QR xác thực hóa đơn
    CreatedByUserId     INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_TuitionInvoices PRIMARY KEY (InvoiceId),
    CONSTRAINT UQ_TuitionInvoices_Code_School UNIQUE (SchoolId, InvoiceCode),
    CONSTRAINT FK_TuitionInvoices_Schools  FOREIGN KEY (SchoolId)       REFERENCES Schools(SchoolId),
    CONSTRAINT FK_TuitionInvoices_Students FOREIGN KEY (StudentId)      REFERENCES Users(UserId),
    CONSTRAINT FK_TuitionInvoices_Configs  FOREIGN KEY (ConfigId)       REFERENCES TuitionFeeConfigs(ConfigId),
    CONSTRAINT FK_TuitionInvoices_Creator  FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_TuitionInvoices_Status   CHECK (Status IN ('PENDING','PAID','OVERDUE','CANCELLED','PARTIAL')),
    CONSTRAINT CK_TuitionInvoices_Amount   CHECK (Amount > 0)
);
GO

-- ----------------------------------------------------------
-- 7.3  TuitionPayments  (Giao dịch thanh toán)
-- ----------------------------------------------------------
CREATE TABLE TuitionPayments (
    PaymentId           INT             NOT NULL IDENTITY(1,1),
    InvoiceId           INT             NOT NULL,
    SchoolId            INT             NOT NULL,
    AmountPaid          DECIMAL(12,0)   NOT NULL,
    PaymentDate         DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    PaymentMethod       VARCHAR(30)     NOT NULL,       -- VNPAY | MOMO | ZALOPAY | BANK_TRANSFER | CASH
    TransactionCode     NVARCHAR(200)   NULL,           -- Mã giao dịch từ cổng thanh toán
    GatewayResponse     NVARCHAR(MAX)   NULL,           -- JSON response raw từ gateway
    PaymentStatus       VARCHAR(20)     NOT NULL DEFAULT 'PENDING', -- PENDING | SUCCESS | FAILED | REFUNDED
    Note                NVARCHAR(500)   NULL,
    ProcessedByUserId   INT             NULL,           -- Kế toán xác nhận (nếu thanh toán offline)
    CONSTRAINT PK_TuitionPayments PRIMARY KEY (PaymentId),
    CONSTRAINT FK_TuitionPayments_Invoices  FOREIGN KEY (InvoiceId)        REFERENCES TuitionInvoices(InvoiceId),
    CONSTRAINT FK_TuitionPayments_Schools   FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_TuitionPayments_Processor FOREIGN KEY (ProcessedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_TuitionPayments_Method    CHECK (PaymentMethod IN ('VNPAY','MOMO','ZALOPAY','BANK_TRANSFER','CASH')),
    CONSTRAINT CK_TuitionPayments_Status    CHECK (PaymentStatus IN ('PENDING','SUCCESS','FAILED','REFUNDED')),
    CONSTRAINT CK_TuitionPayments_Amount    CHECK (AmountPaid > 0)
);
GO


-- ============================================================
-- SECTION 8 — HR & PAYROLL (Nhân sự)
-- ============================================================

-- ----------------------------------------------------------
-- 8.1  TeacherContracts  (Hợp đồng lao động giáo viên)
-- ----------------------------------------------------------
CREATE TABLE TeacherContracts (
    ContractId          INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    TeacherId           INT             NOT NULL,
    ContractCode        NVARCHAR(50)    NOT NULL,
    ContractType        VARCHAR(30)     NOT NULL,       -- PROBATION | FIXED_TERM | INDEFINITE | PART_TIME
    StartDate           DATE            NOT NULL,
    EndDate             DATE            NULL,           -- NULL = vô thời hạn
    BaseSalary          DECIMAL(12,0)   NOT NULL,
    SignedAt             DATE            NULL,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'ACTIVE', -- ACTIVE | EXPIRED | TERMINATED
    DocumentUrl         NVARCHAR(1000)  NULL,           -- File hợp đồng scan
    CreatedByUserId     INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_TeacherContracts PRIMARY KEY (ContractId),
    CONSTRAINT UQ_TeacherContracts_Code_School UNIQUE (SchoolId, ContractCode),
    CONSTRAINT FK_TeacherContracts_Schools  FOREIGN KEY (SchoolId)       REFERENCES Schools(SchoolId),
    CONSTRAINT FK_TeacherContracts_Teachers FOREIGN KEY (TeacherId)      REFERENCES Users(UserId),
    CONSTRAINT FK_TeacherContracts_Creator  FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_TeacherContracts_Status   CHECK (Status IN ('ACTIVE','EXPIRED','TERMINATED'))
);
GO

-- ----------------------------------------------------------
-- 8.2  StaffAttendances  (Chấm công nhân viên/giáo viên)
-- ----------------------------------------------------------
CREATE TABLE StaffAttendances (
    StaffAttId          INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    UserId              INT             NOT NULL,       -- GV, GT, KT, Admin
    AttendanceDate      DATE            NOT NULL,
    CheckInTime         TIME(0)         NULL,
    CheckOutTime        TIME(0)         NULL,
    WorkingHours        AS (
                            CASE WHEN CheckInTime IS NOT NULL AND CheckOutTime IS NOT NULL
                            THEN DATEDIFF(MINUTE, CheckInTime, CheckOutTime) / 60.0
                            ELSE NULL END
                        ) PERSISTED,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'PRESENT', -- PRESENT | ABSENT | LATE | HALF_DAY | ON_LEAVE
    Note                NVARCHAR(300)   NULL,
    CONSTRAINT PK_StaffAttendances PRIMARY KEY (StaffAttId),
    CONSTRAINT UQ_StaffAttendances_User_Date UNIQUE (UserId, AttendanceDate),
    CONSTRAINT FK_StaffAttendances_Schools FOREIGN KEY (SchoolId) REFERENCES Schools(SchoolId),
    CONSTRAINT FK_StaffAttendances_Users   FOREIGN KEY (UserId)   REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 8.3  Payrolls  (Bảng lương tháng)
-- ----------------------------------------------------------
CREATE TABLE Payrolls (
    PayrollId           INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    UserId              INT             NOT NULL,
    PayrollMonth        TINYINT         NOT NULL,       -- 1..12
    PayrollYear         SMALLINT        NOT NULL,
    BaseSalary          DECIMAL(12,0)   NOT NULL,
    TeachingAllowance   DECIMAL(12,0)   NOT NULL DEFAULT 0,
    PositionAllowance   DECIMAL(12,0)   NOT NULL DEFAULT 0,
    OvertimePay         DECIMAL(12,0)   NOT NULL DEFAULT 0,
    Bonus               DECIMAL(12,0)   NOT NULL DEFAULT 0,
    InsuranceDeduction  DECIMAL(12,0)   NOT NULL DEFAULT 0,  -- BHXH + BHYT + BHTN
    TaxDeduction        DECIMAL(12,0)   NOT NULL DEFAULT 0,  -- Thuế TNCN
    OtherDeductions     DECIMAL(12,0)   NOT NULL DEFAULT 0,
    NetSalary           AS (BaseSalary + TeachingAllowance + PositionAllowance + OvertimePay + Bonus
                            - InsuranceDeduction - TaxDeduction - OtherDeductions) PERSISTED,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'DRAFT', -- DRAFT | APPROVED | PAID
    ApprovedByUserId    INT             NULL,
    ApprovedAt          DATETIME2(0)    NULL,
    PaidAt              DATETIME2(0)    NULL,
    Note                NVARCHAR(500)   NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Payrolls PRIMARY KEY (PayrollId),
    CONSTRAINT UQ_Payrolls_User_Month_Year UNIQUE (UserId, PayrollMonth, PayrollYear),
    CONSTRAINT FK_Payrolls_Schools   FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Payrolls_Users     FOREIGN KEY (UserId)          REFERENCES Users(UserId),
    CONSTRAINT FK_Payrolls_Approver  FOREIGN KEY (ApprovedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Payrolls_Month     CHECK (PayrollMonth BETWEEN 1 AND 12),
    CONSTRAINT CK_Payrolls_Status    CHECK (Status IN ('DRAFT','APPROVED','PAID'))
);
GO

-- ----------------------------------------------------------
-- 8.4  TeacherEvaluations  (Đánh giá giáo viên định kỳ 360°)
-- ----------------------------------------------------------
CREATE TABLE TeacherEvaluations (
    EvaluationId        INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    TeacherId           INT             NOT NULL,
    EvaluatorId         INT             NOT NULL,       -- Admin / GV khác / Học sinh
    EvaluatorRole       VARCHAR(20)     NOT NULL,       -- ADMIN | PEER | STUDENT
    AcademicYearId      INT             NOT NULL,
    TeachingScore       TINYINT         NULL,           -- 1-5
    ProfessionalScore   TINYINT         NULL,
    AttitudeScore       TINYINT         NULL,
    OverallScore        TINYINT         NULL,
    Comments            NVARCHAR(2000)  NULL,
    EvaluatedAt         DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_TeacherEvaluations PRIMARY KEY (EvaluationId),
    CONSTRAINT FK_TeacherEvaluations_Schools       FOREIGN KEY (SchoolId)       REFERENCES Schools(SchoolId),
    CONSTRAINT FK_TeacherEvaluations_Teachers      FOREIGN KEY (TeacherId)      REFERENCES Users(UserId),
    CONSTRAINT FK_TeacherEvaluations_Evaluators    FOREIGN KEY (EvaluatorId)    REFERENCES Users(UserId),
    CONSTRAINT FK_TeacherEvaluations_AcademicYears FOREIGN KEY (AcademicYearId) REFERENCES AcademicYears(AcademicYearId)
);
GO


-- ============================================================
-- SECTION 9 — DISCIPLINE & SUPERVISOR MODULE
-- ============================================================

-- ----------------------------------------------------------
-- 9.1  DisciplineRecords  (Ghi nhận vi phạm kỷ luật)
-- ----------------------------------------------------------
CREATE TABLE DisciplineRecords (
    RecordId            INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    StudentId           INT             NOT NULL,
    ReportedByUserId    INT             NOT NULL,       -- Giám thị hoặc GV
    RecordDate          DATE            NOT NULL,
    ViolationType       NVARCHAR(100)   NOT NULL,       -- VD: "Đi học muộn", "Mặc đồng phục sai"
    Severity            VARCHAR(10)     NOT NULL DEFAULT 'MINOR',  -- MINOR | MODERATE | SEVERE
    Description         NVARCHAR(2000)  NULL,
    ActionTaken         NVARCHAR(500)   NULL,
    Status              VARCHAR(20)     NOT NULL DEFAULT 'OPEN',  -- OPEN | RESOLVED | ESCALATED
    EscalatedToUserId   INT             NULL,           -- Ban giám hiệu
    ResolvedAt          DATETIME2(0)    NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_DisciplineRecords PRIMARY KEY (RecordId),
    CONSTRAINT FK_DisciplineRecords_Schools    FOREIGN KEY (SchoolId)           REFERENCES Schools(SchoolId),
    CONSTRAINT FK_DisciplineRecords_Students   FOREIGN KEY (StudentId)          REFERENCES Users(UserId),
    CONSTRAINT FK_DisciplineRecords_Reporter   FOREIGN KEY (ReportedByUserId)   REFERENCES Users(UserId),
    CONSTRAINT FK_DisciplineRecords_Escalated  FOREIGN KEY (EscalatedToUserId)  REFERENCES Users(UserId),
    CONSTRAINT CK_DisciplineRecords_Severity   CHECK (Severity IN ('MINOR','MODERATE','SEVERE')),
    CONSTRAINT CK_DisciplineRecords_Status     CHECK (Status IN ('OPEN','RESOLVED','ESCALATED'))
);
GO

-- ----------------------------------------------------------
-- 9.2  GateCheckLogs  (Giám thị xác nhận HS ra/vào cổng)
-- ----------------------------------------------------------
CREATE TABLE GateCheckLogs (
    LogId               INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    StudentId           INT             NOT NULL,
    CheckType           VARCHAR(10)     NOT NULL,       -- IN | OUT
    CheckedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CheckedByUserId     INT             NULL,           -- NULL nếu tự động (camera)
    Note                NVARCHAR(300)   NULL,
    IsLate              BIT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_GateCheckLogs PRIMARY KEY (LogId),
    CONSTRAINT FK_GateCheckLogs_Schools  FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT FK_GateCheckLogs_Students FOREIGN KEY (StudentId)       REFERENCES Users(UserId),
    CONSTRAINT FK_GateCheckLogs_Checker  FOREIGN KEY (CheckedByUserId) REFERENCES Users(UserId),
    CONSTRAINT CK_GateCheckLogs_Type     CHECK (CheckType IN ('IN','OUT'))
);
GO


-- ============================================================
-- SECTION 10 — COMMUNICATION (Chat & Notifications)
-- ============================================================

-- ----------------------------------------------------------
-- 10.1  Notifications  (Thông báo hệ thống / push / email)
-- ----------------------------------------------------------
CREATE TABLE Notifications (
    NotificationId      INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    Title               NVARCHAR(200)   NOT NULL,
    Body                NVARCHAR(MAX)   NOT NULL,
    NotificationType    VARCHAR(30)     NOT NULL,       -- ATTENDANCE | GRADE | TUITION | DISCIPLINE | GENERAL | SYSTEM
    Channel             VARCHAR(20)     NOT NULL DEFAULT 'IN_APP',  -- IN_APP | EMAIL | SMS | PUSH
    SentByUserId        INT             NULL,           -- NULL = system-generated
    TargetAudience      VARCHAR(20)     NOT NULL DEFAULT 'SPECIFIC', -- SPECIFIC | CLASS | GRADE | SCHOOL_ALL
    TargetClassId       INT             NULL,           -- Nếu TargetAudience = CLASS
    TargetGradeLevelId  INT             NULL,           -- Nếu TargetAudience = GRADE
    IsScheduled         BIT             NOT NULL DEFAULT 0,
    ScheduledAt         DATETIME2(0)    NULL,
    SentAt              DATETIME2(0)    NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notifications_Schools     FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Notifications_Sender      FOREIGN KEY (SentByUserId)      REFERENCES Users(UserId),
    CONSTRAINT FK_Notifications_Class       FOREIGN KEY (TargetClassId)     REFERENCES Classes(ClassId),
    CONSTRAINT FK_Notifications_GradeLevel  FOREIGN KEY (TargetGradeLevelId) REFERENCES GradeLevels(GradeLevelId)
);
GO

-- ----------------------------------------------------------
-- 10.2  NotificationRecipients  (Tracking đọc/nhận per user)
-- ----------------------------------------------------------
CREATE TABLE NotificationRecipients (
    RecipientId         INT             NOT NULL IDENTITY(1,1),
    NotificationId      INT             NOT NULL,
    UserId              INT             NOT NULL,
    IsRead              BIT             NOT NULL DEFAULT 0,
    ReadAt              DATETIME2(0)    NULL,
    DeliveryStatus      VARCHAR(20)     NOT NULL DEFAULT 'PENDING', -- PENDING | DELIVERED | FAILED
    DeliveredAt         DATETIME2(0)    NULL,
    CONSTRAINT PK_NotificationRecipients PRIMARY KEY (RecipientId),
    CONSTRAINT UQ_NotificationRecipients_Notif_User UNIQUE (NotificationId, UserId),
    CONSTRAINT FK_NotificationRecipients_Notifications FOREIGN KEY (NotificationId) REFERENCES Notifications(NotificationId) ON DELETE CASCADE,
    CONSTRAINT FK_NotificationRecipients_Users         FOREIGN KEY (UserId)         REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 10.3  Conversations  (Chat thread — direct message hoặc group)
-- ----------------------------------------------------------
CREATE TABLE Conversations (
    ConversationId      INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    ConversationType    VARCHAR(20)     NOT NULL DEFAULT 'DIRECT', -- DIRECT | GROUP
    ConversationName    NVARCHAR(200)   NULL,           -- Chỉ dùng cho group
    CreatedByUserId     INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    LastMessageAt       DATETIME2(0)    NULL,
    CONSTRAINT PK_Conversations PRIMARY KEY (ConversationId),
    CONSTRAINT FK_Conversations_Schools FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT FK_Conversations_Creator FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 10.4  ConversationParticipants  (Thành viên trong cuộc hội thoại)
-- ----------------------------------------------------------
CREATE TABLE ConversationParticipants (
    ParticipantId       INT             NOT NULL IDENTITY(1,1),
    ConversationId      INT             NOT NULL,
    UserId              INT             NOT NULL,
    JoinedAt            DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    LastReadAt          DATETIME2(0)    NULL,
    IsAdmin             BIT             NOT NULL DEFAULT 0,  -- Admin của group chat
    CONSTRAINT PK_ConversationParticipants PRIMARY KEY (ParticipantId),
    CONSTRAINT UQ_ConversationParticipants UNIQUE (ConversationId, UserId),
    CONSTRAINT FK_ConversationParticipants_Conversations FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId) ON DELETE CASCADE,
    CONSTRAINT FK_ConversationParticipants_Users         FOREIGN KEY (UserId)         REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 10.5  Messages  (Nội dung tin nhắn — direct & group)
-- NOTE: Giám thị có thể nhắn thẳng cho Phụ huynh qua đây
-- ----------------------------------------------------------
CREATE TABLE Messages (
    MessageId           BIGINT          NOT NULL IDENTITY(1,1),
    ConversationId      INT             NOT NULL,
    SenderId            INT             NOT NULL,
    MessageText         NVARCHAR(MAX)   NULL,
    AttachmentUrl       NVARCHAR(1000)  NULL,
    AttachmentType      VARCHAR(20)     NULL,           -- IMAGE | FILE | AUDIO
    IsDeleted           BIT             NOT NULL DEFAULT 0,
    DeletedAt           DATETIME2(0)    NULL,
    SentAt              DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_Messages PRIMARY KEY (MessageId),
    CONSTRAINT FK_Messages_Conversations FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId),
    CONSTRAINT FK_Messages_Senders       FOREIGN KEY (SenderId)       REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 10.6  NewsBoard  (Bảng tin trường / lớp)
-- ----------------------------------------------------------
CREATE TABLE NewsBoard (
    NewsId              INT             NOT NULL IDENTITY(1,1),
    SchoolId            INT             NOT NULL,
    Title               NVARCHAR(300)   NOT NULL,
    ContentHtml         NVARCHAR(MAX)   NOT NULL,
    CoverImageUrl       NVARCHAR(500)   NULL,
    Scope               VARCHAR(20)     NOT NULL DEFAULT 'SCHOOL',  -- SCHOOL | CLASS | GRADE
    TargetClassId       INT             NULL,
    TargetGradeLevelId  INT             NULL,
    IsPinned            BIT             NOT NULL DEFAULT 0,
    IsPublished         BIT             NOT NULL DEFAULT 0,
    PublishedAt         DATETIME2(0)    NULL,
    PublishedByUserId   INT             NOT NULL,
    CreatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_NewsBoard PRIMARY KEY (NewsId),
    CONSTRAINT FK_NewsBoard_Schools     FOREIGN KEY (SchoolId)          REFERENCES Schools(SchoolId),
    CONSTRAINT FK_NewsBoard_Class       FOREIGN KEY (TargetClassId)     REFERENCES Classes(ClassId),
    CONSTRAINT FK_NewsBoard_GradeLevel  FOREIGN KEY (TargetGradeLevelId) REFERENCES GradeLevels(GradeLevelId),
    CONSTRAINT FK_NewsBoard_Publisher   FOREIGN KEY (PublishedByUserId)  REFERENCES Users(UserId)
);
GO


-- ============================================================
-- SECTION 11 — SYSTEM & AUDIT
-- ============================================================

-- ----------------------------------------------------------
-- 11.1  SystemConfigs  (Cấu hình hệ thống per school)
-- ----------------------------------------------------------
CREATE TABLE SystemConfigs (
    ConfigKey           NVARCHAR(100)   NOT NULL,
    SchoolId            INT             NOT NULL,
    ConfigValue         NVARCHAR(2000)  NOT NULL,
    DataType            VARCHAR(20)     NOT NULL DEFAULT 'STRING', -- STRING | INT | BOOL | JSON
    Description         NVARCHAR(500)   NULL,
    UpdatedByUserId     INT             NULL,
    UpdatedAt           DATETIME2(0)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_SystemConfigs PRIMARY KEY (ConfigKey, SchoolId),
    CONSTRAINT FK_SystemConfigs_Schools FOREIGN KEY (SchoolId)        REFERENCES Schools(SchoolId),
    CONSTRAINT FK_SystemConfigs_Users   FOREIGN KEY (UpdatedByUserId) REFERENCES Users(UserId)
);
GO

-- ----------------------------------------------------------
-- 11.2  AuditLogs  (Immutable audit trail)
-- ----------------------------------------------------------
CREATE TABLE AuditLogs (
    LogId               BIGINT          NOT NULL IDENTITY(1,1),
    SchoolId            INT             NULL,
    UserId              INT             NULL,
    Action              NVARCHAR(100)   NOT NULL,       -- VD: "USER_LOGIN", "SCORE_UPDATE", "INVOICE_CREATE"
    EntityType          NVARCHAR(50)    NULL,           -- VD: "ScoreEntry", "User", "TuitionInvoice"
    EntityId            NVARCHAR(50)    NULL,
    OldValues           NVARCHAR(MAX)   NULL,           -- JSON snapshot trước khi thay đổi
    NewValues           NVARCHAR(MAX)   NULL,           -- JSON snapshot sau khi thay đổi
    IPAddress           NVARCHAR(50)    NULL,
    UserAgent           NVARCHAR(500)   NULL,
    Timestamp           DATETIME2(3)    NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT PK_AuditLogs PRIMARY KEY (LogId)
    -- Không có FK → Users để không bị cascade delete xóa log
);
GO


-- ============================================================
-- SECTION 12 — INDEXES (Tối ưu hóa truy vấn phổ biến)
-- ============================================================

-- Users: tra cứu theo email và trường
CREATE INDEX IX_Users_Email          ON Users (Email);
CREATE INDEX IX_Users_SchoolId       ON Users (SchoolId);
CREATE INDEX IX_Users_RoleId_SchoolId ON Users (RoleId, SchoolId);

-- Classes: lọc theo năm học và trường
CREATE INDEX IX_Classes_SchoolId_Year ON Classes (SchoolId, AcademicYearId);

-- ClassEnrollments: tra cứu HS của lớp và lớp của HS
CREATE INDEX IX_ClassEnrollments_ClassId   ON ClassEnrollments (ClassId);
CREATE INDEX IX_ClassEnrollments_StudentId ON ClassEnrollments (StudentId);

-- Schedules: lọc TKB theo lớp và học kỳ
CREATE INDEX IX_Schedules_SemesterId ON Schedules (SemesterId);

-- AttendanceSessions: lấy buổi học theo ngày
CREATE INDEX IX_AttendanceSessions_ScheduleId_Date ON AttendanceSessions (ScheduleId, SessionDate);
CREATE INDEX IX_AttendanceSessions_QRToken          ON AttendanceSessions (QRToken) WHERE SessionStatus = 'OPEN';

-- Attendances: điểm danh theo buổi và học sinh
CREATE INDEX IX_Attendances_SessionId   ON Attendances (SessionId);
CREATE INDEX IX_Attendances_StudentId   ON Attendances (StudentId);
CREATE INDEX IX_Attendances_Status      ON Attendances (Status) WHERE Status = 'ABSENT';

-- ScoreEntries: lấy điểm theo HS + HK
CREATE INDEX IX_ScoreEntries_Student_SubjectAssignment ON ScoreEntries (StudentId, SubjectAssignmentId);
CREATE INDEX IX_ScoreEntries_SubjectAssignment          ON ScoreEntries (SubjectAssignmentId);

-- GradeBook: bảng điểm theo lớp
CREATE INDEX IX_GradeBook_StudentId ON GradeBook (StudentId);

-- Assignments: bài tập theo lớp + môn
CREATE INDEX IX_Assignments_SubjectAssignmentId ON Assignments (SubjectAssignmentId);

-- AssignmentSubmissions: bài nộp theo HS
CREATE INDEX IX_AssignmentSubmissions_StudentId    ON AssignmentSubmissions (StudentId);
CREATE INDEX IX_AssignmentSubmissions_AssignmentId ON AssignmentSubmissions (AssignmentId);

-- TuitionInvoices: công nợ học phí
CREATE INDEX IX_TuitionInvoices_StudentId ON TuitionInvoices (StudentId);
CREATE INDEX IX_TuitionInvoices_Status    ON TuitionInvoices (Status, DueDate) WHERE Status IN ('PENDING','OVERDUE');

-- Messages: lấy tin nhắn theo cuộc hội thoại (mới nhất trước)
CREATE INDEX IX_Messages_ConversationId_SentAt ON Messages (ConversationId, SentAt DESC);

-- Notifications: thông báo chưa đọc
CREATE INDEX IX_NotificationRecipients_UserId_IsRead ON NotificationRecipients (UserId, IsRead) WHERE IsRead = 0;

-- AuditLogs: tra cứu theo thực thể
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs (EntityType, EntityId);
CREATE INDEX IX_AuditLogs_UserId_Timestamp     ON AuditLogs (UserId, Timestamp DESC);
GO


-- ============================================================
-- SECTION 13 — STORED PROCEDURES
-- ============================================================

-- ----------------------------------------------------------
-- SP 1: Tính điểm trung bình môn theo TT22/2021
-- Input : @StudentId, @SubjectAssignmentId
-- Output: ĐTBm = (ΣĐTXi×1 + ĐGK×2 + ĐCK×3) / (n_TX + 2 + 3)
-- ----------------------------------------------------------
CREATE OR ALTER PROCEDURE SP_CalculateSubjectAverage
    @StudentId          INT,
    @SubjectAssignmentId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE
        @SumTX      DECIMAL(10,4) = 0,
        @CountTX    INT           = 0,
        @ScoreGK    DECIMAL(4,2)  = NULL,
        @ScoreCK    DECIMAL(4,2)  = NULL,
        @Average    DECIMAL(4,2)  = NULL,
        @Remark     VARCHAR(20)   = NULL;

    -- Lấy điểm thường xuyên (GradeCategoryId tương ứng 'DTX', coefficient=1)
    SELECT
        @SumTX   = SUM(se.Score),
        @CountTX = COUNT(*)
    FROM ScoreEntries se
    INNER JOIN GradeCategories gc ON se.GradeCategoryId = gc.GradeCategoryId
    WHERE
        se.StudentId           = @StudentId
        AND se.SubjectAssignmentId = @SubjectAssignmentId
        AND gc.CategoryCode    = 'DTX';

    -- Lấy điểm giữa kỳ
    SELECT @ScoreGK = se.Score
    FROM ScoreEntries se
    INNER JOIN GradeCategories gc ON se.GradeCategoryId = gc.GradeCategoryId
    WHERE
        se.StudentId           = @StudentId
        AND se.SubjectAssignmentId = @SubjectAssignmentId
        AND gc.CategoryCode    = 'DGK';

    -- Lấy điểm cuối kỳ
    SELECT @ScoreCK = se.Score
    FROM ScoreEntries se
    INNER JOIN GradeCategories gc ON se.GradeCategoryId = gc.GradeCategoryId
    WHERE
        se.StudentId           = @StudentId
        AND se.SubjectAssignmentId = @SubjectAssignmentId
        AND gc.CategoryCode    = 'DCK';

    -- Tính ĐTBm (chỉ tính nếu có đủ ĐGK và ĐCK)
    IF @ScoreGK IS NOT NULL AND @ScoreCK IS NOT NULL AND @CountTX > 0
    BEGIN
        SET @Average = CAST(
            (@SumTX + @ScoreGK * 2.0 + @ScoreCK * 3.0) /
            (@CountTX + 2.0 + 3.0)
        AS DECIMAL(4,2));

        -- Xếp loại theo TT22
        SET @Remark = CASE
            WHEN @Average >= 9.0 THEN 'EXCELLENT'
            WHEN @Average >= 7.0 THEN 'GOOD'
            WHEN @Average >= 5.0 THEN 'AVERAGE'
            WHEN @Average >= 3.5 THEN 'BELOW_AVG'
            ELSE 'FAIL'
        END;

        -- Cập nhật hoặc chèn vào GradeBook
        MERGE GradeBook AS target
        USING (SELECT @StudentId AS StudentId, @SubjectAssignmentId AS SubjectAssignmentId) AS source
            ON target.StudentId = source.StudentId
            AND target.SubjectAssignmentId = source.SubjectAssignmentId
        WHEN MATCHED AND target.IsLocked = 0 THEN
            UPDATE SET
                AverageScore   = @Average,
                Remark         = @Remark,
                IsCalculated   = 1,
                CalculatedAt   = GETUTCDATE()
        WHEN NOT MATCHED THEN
            INSERT (SchoolId, StudentId, SubjectAssignmentId, AverageScore, Remark, IsCalculated, CalculatedAt)
            SELECT SchoolId, @StudentId, @SubjectAssignmentId, @Average, @Remark, 1, GETUTCDATE()
            FROM SubjectAssignments WHERE AssignmentId = @SubjectAssignmentId;
    END

    -- Trả về kết quả
    SELECT
        @StudentId           AS StudentId,
        @SubjectAssignmentId AS SubjectAssignmentId,
        @CountTX             AS RegularScoreCount,
        @SumTX               AS SumRegularScores,
        @ScoreGK             AS MidTermScore,
        @ScoreCK             AS FinalScore,
        @Average             AS AverageScore,
        @Remark              AS Remark;
END
GO

-- ----------------------------------------------------------
-- SP 2: Gửi cảnh báo vắng mặt cho phụ huynh
-- Dùng để trigger sau khi GV đóng AttendanceSession
-- ----------------------------------------------------------
CREATE OR ALTER PROCEDURE SP_NotifyAbsentStudents
    @SessionId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SchoolId INT;
    SELECT @SchoolId = s.SchoolId FROM AttendanceSessions s WHERE s.SessionId = @SessionId;

    -- Lấy danh sách học sinh vắng
    SELECT
        a.AttendanceId,
        a.StudentId,
        u.FullName          AS StudentName,
        sch.ScheduleId,
        sub.SubjectName,
        sess.SessionDate,
        psr.ParentUserId,
        pu.FullName         AS ParentName,
        pu.PhoneNumber      AS ParentPhone
    INTO #AbsentStudents
    FROM Attendances a
    INNER JOIN AttendanceSessions sess  ON a.SessionId   = sess.SessionId
    INNER JOIN Schedules          sch   ON sess.ScheduleId = sch.ScheduleId
    INNER JOIN SubjectAssignments sa    ON sch.SubjectAssignmentId = sa.AssignmentId
    INNER JOIN Subjects           sub   ON sa.SubjectId  = sub.SubjectId
    INNER JOIN Users              u     ON a.StudentId   = u.UserId
    INNER JOIN ParentStudentRelations psr ON a.StudentId = psr.StudentUserId
    INNER JOIN Users              pu    ON psr.ParentUserId = pu.UserId
    WHERE
        a.SessionId = @SessionId
        AND a.Status IN ('ABSENT', 'LATE')
        AND a.NotifiedParent = 0;

    -- Tạo bản ghi Notification
    INSERT INTO Notifications (SchoolId, Title, Body, NotificationType, Channel, TargetAudience, SentAt)
    SELECT DISTINCT
        @SchoolId,
        N'Thông báo vắng mặt — ' + SubjectName,
        N'Con em ' + StudentName + N' đã vắng mặt trong buổi học ' + SubjectName
            + N' ngày ' + CONVERT(NVARCHAR, SessionDate, 103),
        'ATTENDANCE',
        'IN_APP',
        'SPECIFIC',
        GETUTCDATE()
    FROM #AbsentStudents;

    -- Đánh dấu đã thông báo
    UPDATE a
    SET NotifiedParent = 1, NotifiedAt = GETUTCDATE()
    FROM Attendances a
    INNER JOIN #AbsentStudents abs ON a.AttendanceId = abs.AttendanceId;

    DROP TABLE #AbsentStudents;

    SELECT 'Notifications sent successfully' AS Result;
END
GO

-- ----------------------------------------------------------
-- SP 3: Tạo hóa đơn học phí hàng loạt theo lớp / tháng
-- ----------------------------------------------------------
CREATE OR ALTER PROCEDURE SP_GenerateTuitionInvoices
    @SchoolId       INT,
    @AcademicYearId INT,
    @BillingPeriod  NVARCHAR(20),  -- VD: "2024-09"
    @DueDate        DATE,
    @CreatedByUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InvoiceSeq INT;
    SELECT @InvoiceSeq = ISNULL(MAX(InvoiceId), 0) FROM TuitionInvoices WHERE SchoolId = @SchoolId;

    -- Tạo hóa đơn cho từng học sinh chưa có hóa đơn kỳ này
    INSERT INTO TuitionInvoices (
        SchoolId, StudentId, ConfigId, InvoiceCode,
        BillingPeriod, Amount, DueDate, Status, CreatedByUserId
    )
    SELECT
        @SchoolId,
        ce.StudentId,
        tfc.ConfigId,
        N'INV-' + CAST(@SchoolId AS NVARCHAR) + N'-'
            + FORMAT(@InvoiceSeq + ROW_NUMBER() OVER(ORDER BY ce.StudentId), '000000'),
        @BillingPeriod,
        tfc.Amount,
        @DueDate,
        'PENDING',
        @CreatedByUserId
    FROM ClassEnrollments ce
    INNER JOIN Classes          cl  ON ce.ClassId      = cl.ClassId
    INNER JOIN TuitionFeeConfigs tfc ON (tfc.GradeLevelId = cl.GradeLevelId OR tfc.GradeLevelId IS NULL)
                                     AND tfc.AcademicYearId = @AcademicYearId
                                     AND tfc.SchoolId = @SchoolId
                                     AND tfc.IsActive = 1
                                     AND tfc.BillingCycle = 'MONTHLY'
    WHERE
        cl.SchoolId   = @SchoolId
        AND ce.Status = 'ACTIVE'
        AND NOT EXISTS (
            SELECT 1 FROM TuitionInvoices ti
            WHERE ti.StudentId = ce.StudentId
              AND ti.BillingPeriod = @BillingPeriod
              AND ti.ConfigId = tfc.ConfigId
        );

    SELECT @@ROWCOUNT AS InvoicesCreated;
END
GO

-- ----------------------------------------------------------
-- SP 4: Sắp xếp chỗ ngồi phòng thi ngẫu nhiên
-- ----------------------------------------------------------
CREATE OR ALTER PROCEDURE SP_AssignExamSeatsRandom
    @ExamId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Xóa phân công cũ (nếu có)
    DELETE era
    FROM ExamRoomAssignments era
    INNER JOIN ExamRooms er ON era.ExamRoomId = er.ExamRoomId
    WHERE er.ExamId = @ExamId;

    -- Lấy danh sách học sinh thi và trộn ngẫu nhiên
    WITH RandomizedStudents AS (
        SELECT
            er.ExamRoomId,
            er.Capacity,
            u.UserId AS StudentId,
            ROW_NUMBER() OVER (ORDER BY NEWID()) AS RandomRank
        FROM Exams e
        INNER JOIN ExamRooms er ON e.ExamId = er.ExamId
        INNER JOIN Classes cl   ON cl.GradeLevelId = e.GradeLevelId
                                AND cl.AcademicYearId = (
                                    SELECT AcademicYearId FROM Semesters WHERE SemesterId = e.SemesterId
                                )
        INNER JOIN ClassEnrollments ce ON ce.ClassId = cl.ClassId AND ce.Status = 'ACTIVE'
        INNER JOIN Users u ON ce.StudentId = u.UserId
        WHERE e.ExamId = @ExamId
    ),
    RoomAssignment AS (
        SELECT
            *,
            CEILING(RandomRank * 1.0 /
                NULLIF((SELECT TOP 1 Capacity FROM ExamRooms WHERE ExamId = @ExamId), 0)
            ) AS RoomSequence
        FROM RandomizedStudents
    )
    INSERT INTO ExamRoomAssignments (ExamRoomId, StudentId, SeatNumber)
    SELECT
        er.ExamRoomId,
        ra.StudentId,
        'S' + FORMAT(ROW_NUMBER() OVER (PARTITION BY er.ExamRoomId ORDER BY ra.RandomRank), '00')
    FROM RoomAssignment ra
    INNER JOIN ExamRooms er ON er.ExamId = @ExamId
        AND ROW_NUMBER() OVER (PARTITION BY er.ExamRoomId ORDER BY ra.RandomRank) <= er.Capacity;

    SELECT @@ROWCOUNT AS SeatsAssigned;
END
GO


-- ============================================================
-- SECTION 14 — SEED DATA
-- ============================================================

-- Roles (6 roles cố định)
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName, RoleCode, Description) VALUES
(1, N'Quản trị viên',  'ADMIN',       N'Quản trị toàn bộ hệ thống, người dùng và cấu hình'),
(2, N'Giáo viên',      'TEACHER',     N'Tạo bài giảng, quản lý lớp, chấm điểm'),
(3, N'Học sinh',       'STUDENT',     N'Học trực tuyến, vào phòng 3D, làm bài tập'),
(4, N'Phụ huynh',      'PARENT',      N'Theo dõi học lực, điểm danh, nhận thông báo'),
(5, N'Giám thị',       'SUPERVISOR',  N'Giám sát kỷ luật toàn trường, coi thi, quản lý nề nếp'),
(6, N'Kế toán',        'ACCOUNTANT',  N'Quản lý học phí, hóa đơn và báo cáo tài chính');
SET IDENTITY_INSERT Roles OFF;
GO

-- GradeCategories (Loại điểm theo TT22/2021)
SET IDENTITY_INSERT GradeCategories ON;
INSERT INTO GradeCategories (GradeCategoryId, CategoryCode, CategoryName, Coefficient, MaxCountPerSemester, IsMultipleAllowed) VALUES
(1, 'DTX', N'Điểm thường xuyên', 1, NULL, 1),  -- Nhiều lần/HK
(2, 'DGK', N'Điểm giữa kỳ',      2, 1,    0),  -- 1 lần/HK
(3, 'DCK', N'Điểm cuối kỳ',      3, 1,    0);  -- 1 lần/HK
SET IDENTITY_INSERT GradeCategories OFF;
GO

-- SystemConfigs mặc định
-- (Sẽ INSERT khi khởi tạo School đầu tiên)
PRINT N'✅ LuminaTutorsDB schema created successfully.';
PRINT N'📊 Total tables: 38';
PRINT N'🔍 Total indexes: 17';
PRINT N'⚙️  Total stored procedures: 4';
PRINT N'🌱 Seed data: Roles (6), GradeCategories (3)';
GO
