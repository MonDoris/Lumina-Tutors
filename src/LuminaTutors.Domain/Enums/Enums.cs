namespace LuminaTutors.Domain.Enums;

// ─── Identity & Authorization ────────────────────────────────────────────────

public enum RoleCode
{
    Admin       = 1,
    Teacher     = 2,
    Student     = 3,
    Parent      = 4,
    Supervisor  = 5,
    Accountant  = 6
}

// ─── Education Level ─────────────────────────────────────────────────────────

public enum EducationLevel
{
    TieuHoc = 1,    // Lớp 1-5
    Thcs    = 2,    // Lớp 6-9
    Thpt    = 3     // Lớp 10-12
}

// ─── Subject Category ────────────────────────────────────────────────────────

public enum SubjectCategory
{
    Main            = 1,
    Elective        = 2,
    Extracurricular = 3
}

// ─── Enrollment Status ───────────────────────────────────────────────────────

public enum EnrollmentStatus
{
    Active      = 1,
    Transferred = 2,
    Withdrawn   = 3
}

// ─── Contract Type ───────────────────────────────────────────────────────────

public enum ContractType
{
    FullTime   = 1,
    PartTime   = 2,
    Contract   = 3,
    Probation  = 4,
    FixedTerm  = 5,
    Indefinite = 6
}

// ─── Attendance ──────────────────────────────────────────────────────────────

public enum AttendanceStatus
{
    Present = 1,
    Absent  = 2,
    Late    = 3,
    Excused = 4
}

public enum CheckMethod
{
    QrScan          = 1,
    Manual          = 2,
    FaceRecognition = 3
}

public enum SessionStatus
{
    Open      = 1,
    Closed    = 2,
    Cancelled = 3
}

public enum EventType
{
    Assembly  = 1,
    FieldTrip = 2,
    Award     = 3,
    Sport     = 4,
    Other     = 5
}

// ─── Schedule ────────────────────────────────────────────────────────────────

public enum ScheduleChangeType
{
    Created     = 1,
    Modified    = 2,
    Cancelled   = 3,
    Substituted = 4
}

// ─── Learning ────────────────────────────────────────────────────────────────

public enum LessonType
{
    Lecture  = 1,
    Material = 2,
    Lab3D    = 3
}

public enum MaterialFileType
{
    Pdf   = 1,
    Video = 2,
    Slide = 3,
    Audio = 4,
    Image = 5,
    Other = 6
}

public enum QuestionType
{
    MultipleChoice = 1,
    TrueFalse      = 2,
    ShortAnswer    = 3,
    Essay          = 4,
    FileUpload     = 5
}

public enum DifficultyLevel
{
    Easy   = 1,
    Medium = 2,
    Hard   = 3
}

public enum AssignmentType
{
    Homework = 1,
    Quiz     = 2,
    Midterm  = 3,
    Final    = 4,
    Project  = 5
}

public enum SubmissionStatus
{
    Draft     = 1,
    Submitted = 2,
    Graded    = 3,
    Returned  = 4
}

// ─── Grading ─────────────────────────────────────────────────────────────────

public enum GradeCategoryCode
{
    Dtx = 1,   // Điểm thường xuyên — hệ số 1
    Dgk = 2,   // Điểm giữa kỳ     — hệ số 2
    Dck = 3    // Điểm cuối kỳ     — hệ số 3
}

public enum GradeRemark
{
    Excellent = 1,  // >= 9.0
    Good      = 2,  // >= 7.0
    Average   = 3,  // >= 5.0
    BelowAvg  = 4,  // >= 3.5
    Fail      = 5   // < 3.5
}

public enum ExamType
{
    Midterm = 1,
    Final   = 2,
    Makeup  = 3,
    Regular = 4
}

// ─── Finance ─────────────────────────────────────────────────────────────────

public enum BillingCycle
{
    Monthly   = 1,
    Semester  = 2,
    Yearly    = 3
}

public enum InvoiceStatus
{
    Pending   = 1,
    Paid      = 2,
    Overdue   = 3,
    Cancelled = 4,
    Partial   = 5
}

public enum PaymentMethod
{
    VnPay        = 1,
    Momo         = 2,
    ZaloPay      = 3,
    BankTransfer = 4,
    Cash         = 5
}

public enum PaymentStatus
{
    Pending  = 1,
    Success  = 2,
    Failed   = 3,
    Refunded = 4
}

// ─── HR ──────────────────────────────────────────────────────────────────────

public enum ContractStatus
{
    Active     = 1,
    Expired    = 2,
    Terminated = 3
}

public enum StaffAttendanceStatus
{
    Present  = 1,
    Absent   = 2,
    Late     = 3,
    HalfDay  = 4,
    OnLeave  = 5
}

public enum PayrollStatus
{
    Draft    = 1,
    Approved = 2,
    Paid     = 3
}

public enum EvaluatorRole
{
    Admin   = 1,
    Peer    = 2,
    Student = 3
}

// ─── Discipline ───────────────────────────────────────────────────────────────

public enum ViolationSeverity
{
    Minor    = 1,
    Moderate = 2,
    Severe   = 3
}

public enum DisciplineStatus
{
    Open      = 1,
    Resolved  = 2,
    Escalated = 3
}

public enum GateCheckType
{
    In  = 1,
    Out = 2
}

// ─── Communication ───────────────────────────────────────────────────────────

public enum NotificationType
{
    Attendance  = 1,
    Grade       = 2,
    Tuition     = 3,
    Discipline  = 4,
    General     = 5,
    System      = 6
}

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Sms   = 3,
    Push  = 4
}

public enum NotificationAudience
{
    Specific  = 1,
    Class     = 2,
    Grade     = 3,
    SchoolAll = 4
}

public enum DeliveryStatus
{
    Pending   = 1,
    Delivered = 2,
    Failed    = 3
}

public enum ConversationType
{
    Direct = 1,
    Group  = 2
}

public enum NewsBoardScope
{
    School = 1,
    Class  = 2,
    Grade  = 3
}

// ─── Gender ──────────────────────────────────────────────────────────────────

public enum Gender
{
    Male   = 1,
    Female = 2,
    Other  = 3
}

// ─── Admission ───────────────────────────────────────────────────────────────

public enum AdmissionType
{
    New      = 1,
    Transfer = 2
}

// ─── Quiz / Exam ─────────────────────────────────────────────────────────────

public enum QuizExamStatus
{
    Draft     = 1,   // teacher editing, not visible to students
    Published = 2,   // students can take it (open)
    Closed    = 3    // no more submissions
}

public enum AttemptStatus
{
    InProgress = 1,
    Submitted  = 2,
    TimedOut   = 3
}

// ─── Online Classroom ─────────────────────────────────────────────────────────

public enum OnlineSessionStatus
{
    Scheduled = 1,
    Live      = 2,
    Ended     = 3,
    Cancelled = 4
}

public enum ChatMessageType
{
    Text   = 1,
    System = 2,
    File   = 3,
    Emoji  = 4
}

public enum ImportJobStatus
{
    Pending    = 1,
    Processing = 2,
    Completed  = 3,
    Failed     = 4
}

