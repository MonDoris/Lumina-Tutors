using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Entities.Discipline;
using LuminaTutors.Domain.Entities.Finance;
using LuminaTutors.Domain.Entities.Grading;
using LuminaTutors.Domain.Entities.HR;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Entities.System;

namespace LuminaTutors.Domain.Interfaces.Repositories;

/// <summary>
/// Unit of Work — single transaction boundary across multiple repositories.
/// Inject IUnitOfWork in Service classes; never inject DbContext directly.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // ── Identity & Auth ───────────────────────────────────────────────────────
    IRepository<School>      Schools      { get; }
    IRepository<Role>        Roles        { get; }
    IRepository<User>        Users        { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<InviteLink>  InviteLinks  { get; }

    // ── Academic ──────────────────────────────────────────────────────────────
    IRepository<AcademicYear>      AcademicYears      { get; }
    IRepository<Semester>          Semesters          { get; }
    IRepository<GradeLevel>        GradeLevels        { get; }
    IRepository<Subject>           Subjects           { get; }
    IRepository<Class>             Classes            { get; }
    IRepository<ClassEnrollment>   ClassEnrollments   { get; }
    IRepository<SubjectAssignment> SubjectAssignments { get; }

    // ── Profiles ──────────────────────────────────────────────────────────────
    IRepository<StudentProfile>       StudentProfiles       { get; }
    IRepository<TeacherProfile>       TeacherProfiles       { get; }
    IRepository<ParentProfile>        ParentProfiles        { get; }
    IRepository<SupervisorProfile>    SupervisorProfiles    { get; }
    IRepository<AccountantProfile>    AccountantProfiles    { get; }
    IRepository<ParentStudentRelation> ParentStudentRelations { get; }

    // ── Attendance ────────────────────────────────────────────────────────────
    IRepository<Schedule>          Schedules          { get; }
    IRepository<ScheduleChangeLog> ScheduleChangeLogs { get; }
    IRepository<AttendanceSession> AttendanceSessions { get; }
    IRepository<StudentAttendance> StudentAttendances { get; }
    IRepository<SchoolEvent>       SchoolEvents       { get; }
    IRepository<EventAttendance>   EventAttendances   { get; }

    // ── Learning ──────────────────────────────────────────────────────────────
    IRepository<Lesson>               Lessons               { get; }
    IRepository<LessonMaterial>       LessonMaterials       { get; }
    IRepository<QuestionBank>         QuestionBanks         { get; }
    IRepository<QuestionOption>       QuestionOptions       { get; }
    IRepository<Assignment>           Assignments           { get; }
    IRepository<AssignmentAttachment> AssignmentAttachments { get; }
    IRepository<AssignmentSubmission> AssignmentSubmissions { get; }
    IRepository<SubmissionFile>       SubmissionFiles       { get; }
    IRepository<VirtualLabSession>    VirtualLabSessions    { get; }
    IRepository<QuizExam>             QuizExams             { get; }
    IRepository<OnlineSession>        OnlineSessions        { get; }
    IRepository<SessionParticipant>   SessionParticipants   { get; }
    IRepository<OnlineRoomChat>       OnlineRoomChats       { get; }
    IRepository<OnlineSlide>          OnlineSlides          { get; }
    IRepository<QuizExamQuestion>     QuizExamQuestions     { get; }
    IRepository<StudentQuizAttempt>   StudentQuizAttempts   { get; }
    IRepository<StudentQuizAnswer>    StudentQuizAnswers    { get; }
    IRepository<QuestionImportJob>    QuestionImportJobs    { get; }

    // ── Grading ───────────────────────────────────────────────────────────────
    IRepository<GradeCategory>           GradeCategories           { get; }
    IRepository<SubjectGradeRequirement> SubjectGradeRequirements  { get; }
    IRepository<ScoreEntry>              ScoreEntries              { get; }
    IRepository<GradeBook>               GradeBooks                { get; }
    IRepository<Exam>                    Exams                     { get; }
    IRepository<ExamRoom>                ExamRooms                 { get; }
    IRepository<ExamRoomAssignment>      ExamRoomAssignments       { get; }

    // ── Finance ───────────────────────────────────────────────────────────────
    IRepository<TuitionFeeConfig> TuitionFeeConfigs { get; }
    IRepository<TuitionInvoice>   TuitionInvoices   { get; }
    IRepository<TuitionPayment>   TuitionPayments   { get; }

    // ── HR ────────────────────────────────────────────────────────────────────
    IRepository<TeacherContract>    TeacherContracts    { get; }
    IRepository<StaffAttendance>    StaffAttendances    { get; }
    IRepository<Payroll>            Payrolls            { get; }
    IRepository<TeacherEvaluation>  TeacherEvaluations  { get; }

    // ── Discipline ────────────────────────────────────────────────────────────
    IRepository<DisciplineRecord> DisciplineRecords { get; }
    IRepository<GateCheckLog>     GateCheckLogs     { get; }

    // ── Communication ─────────────────────────────────────────────────────────
    IRepository<Notification>         Notifications         { get; }
    IRepository<NotificationRecipient> NotificationRecipients { get; }
    IRepository<Conversation>         Conversations         { get; }
    IRepository<ConversationParticipant> ConversationParticipants { get; }
    IRepository<Message>              Messages              { get; }
    IRepository<NewsBoard>            NewsBoards            { get; }

    // ── Transaction ───────────────────────────────────────────────────────────
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes <paramref name="action"/> inside a database transaction that is
    /// compatible with EF Core's retry execution strategy (EnableRetryOnFailure).
    /// Commits on success; rolls back and rethrows on failure.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);

    // ── Stored Procedures ─────────────────────────────────────────────────────
    Task<int> ExecuteStoredProcedureAsync(string spName, object? parameters = null, CancellationToken ct = default);
}
