using Microsoft.EntityFrameworkCore;
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
using System.Reflection;

namespace LuminaTutors.Infrastructure.Data;

/// <summary>
/// Central EF Core DbContext for Lumina Tutors.
/// - Applies all IEntityTypeConfiguration classes via Assembly scan.
/// - Uses AuditInterceptor to auto-stamp timestamps.
/// - Converts all enums to strings for readability in DB.
/// </summary>
public class LuminaTutorsDbContext : DbContext
{
    public LuminaTutorsDbContext(DbContextOptions<LuminaTutorsDbContext> options)
        : base(options) { }

    // ── Identity & Auth ───────────────────────────────────────────────────────
    public DbSet<School>       Schools       => Set<School>();
    public DbSet<Role>         Roles         => Set<Role>();
    public DbSet<User>         Users         => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<InviteLink>   InviteLinks   => Set<InviteLink>();

    // ── Academic ──────────────────────────────────────────────────────────────
    public DbSet<AcademicYear>      AcademicYears      => Set<AcademicYear>();
    public DbSet<Semester>          Semesters          => Set<Semester>();
    public DbSet<GradeLevel>        GradeLevels        => Set<GradeLevel>();
    public DbSet<Subject>           Subjects           => Set<Subject>();
    public DbSet<Class>             Classes            => Set<Class>();
    public DbSet<ClassEnrollment>   ClassEnrollments   => Set<ClassEnrollment>();
    public DbSet<SubjectAssignment> SubjectAssignments => Set<SubjectAssignment>();

    // ── Profiles ──────────────────────────────────────────────────────────────
    public DbSet<StudentProfile>        StudentProfiles        => Set<StudentProfile>();
    public DbSet<TeacherProfile>        TeacherProfiles        => Set<TeacherProfile>();
    public DbSet<ParentProfile>         ParentProfiles         => Set<ParentProfile>();
    public DbSet<SupervisorProfile>     SupervisorProfiles     => Set<SupervisorProfile>();
    public DbSet<AccountantProfile>     AccountantProfiles     => Set<AccountantProfile>();
    public DbSet<ParentStudentRelation> ParentStudentRelations => Set<ParentStudentRelation>();

    // ── Attendance ────────────────────────────────────────────────────────────
    public DbSet<Schedule>          Schedules          => Set<Schedule>();
    public DbSet<ScheduleChangeLog> ScheduleChangeLogs => Set<ScheduleChangeLog>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<StudentAttendance> StudentAttendances => Set<StudentAttendance>();
    public DbSet<SchoolEvent>       SchoolEvents       => Set<SchoolEvent>();
    public DbSet<EventAttendance>   EventAttendances   => Set<EventAttendance>();

    // ── Learning ──────────────────────────────────────────────────────────────
    public DbSet<Lesson>               Lessons               => Set<Lesson>();
    public DbSet<LessonMaterial>       LessonMaterials       => Set<LessonMaterial>();
    public DbSet<QuestionBank>         QuestionBanks         => Set<QuestionBank>();
    public DbSet<QuestionOption>       QuestionOptions       => Set<QuestionOption>();
    public DbSet<Assignment>           Assignments           => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();
    public DbSet<QuizExam>             QuizExams             => Set<QuizExam>();
    public DbSet<QuizExamQuestion>     QuizExamQuestions     => Set<QuizExamQuestion>();
    public DbSet<StudentQuizAttempt>   StudentQuizAttempts   => Set<StudentQuizAttempt>();
    public DbSet<StudentQuizAnswer>    StudentQuizAnswers    => Set<StudentQuizAnswer>();
    public DbSet<SubmissionFile>       SubmissionFiles       => Set<SubmissionFile>();
    public DbSet<VirtualLabSession>    VirtualLabSessions    => Set<VirtualLabSession>();
    public DbSet<OnlineSession>        OnlineSessions        => Set<OnlineSession>();
    public DbSet<SessionParticipant>   SessionParticipants   => Set<SessionParticipant>();
    public DbSet<OnlineRoomChat>       OnlineRoomChats       => Set<OnlineRoomChat>();
    public DbSet<OnlineSlide>          OnlineSlides          => Set<OnlineSlide>();
    public DbSet<QuestionImportJob>    QuestionImportJobs    => Set<QuestionImportJob>();

    // ── Grading ───────────────────────────────────────────────────────────────
    public DbSet<GradeCategory>           GradeCategories          => Set<GradeCategory>();
    public DbSet<SubjectGradeRequirement> SubjectGradeRequirements => Set<SubjectGradeRequirement>();
    public DbSet<ScoreEntry>              ScoreEntries             => Set<ScoreEntry>();
    public DbSet<GradeBook>               GradeBooks               => Set<GradeBook>();
    public DbSet<Exam>                    Exams                    => Set<Exam>();
    public DbSet<ExamRoom>                ExamRooms                => Set<ExamRoom>();
    public DbSet<ExamRoomAssignment>      ExamRoomAssignments      => Set<ExamRoomAssignment>();

    // ── Finance ───────────────────────────────────────────────────────────────
    public DbSet<TuitionFeeConfig> TuitionFeeConfigs => Set<TuitionFeeConfig>();
    public DbSet<TuitionInvoice>   TuitionInvoices   => Set<TuitionInvoice>();
    public DbSet<TuitionPayment>   TuitionPayments   => Set<TuitionPayment>();

    // ── HR ────────────────────────────────────────────────────────────────────
    public DbSet<TeacherContract>   TeacherContracts   => Set<TeacherContract>();
    public DbSet<StaffAttendance>   StaffAttendances   => Set<StaffAttendance>();
    public DbSet<Payroll>           Payrolls           => Set<Payroll>();
    public DbSet<TeacherEvaluation> TeacherEvaluations => Set<TeacherEvaluation>();

    // ── Discipline ────────────────────────────────────────────────────────────
    public DbSet<DisciplineRecord> DisciplineRecords => Set<DisciplineRecord>();
    public DbSet<GateCheckLog>     GateCheckLogs     => Set<GateCheckLog>();

    // ── Communication ─────────────────────────────────────────────────────────
    public DbSet<Notification>            Notifications            => Set<Notification>();
    public DbSet<NotificationRecipient>   NotificationRecipients   => Set<NotificationRecipient>();
    public DbSet<Conversation>            Conversations            => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message>                 Messages                 => Set<Message>();
    public DbSet<NewsBoard>               NewsBoards               => Set<NewsBoard>();

    // ── System ────────────────────────────────────────────────────────────────
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<AuditLog>     AuditLogs     => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Store all DateOnly as date columns (no time component)
        configurationBuilder
            .Properties<DateOnly>()
            .HaveConversion<DateOnlyConverter>()
            .HaveColumnType("date");

        // Store all TimeOnly as time columns
        configurationBuilder
            .Properties<TimeOnly>()
            .HaveConversion<TimeOnlyConverter>()
            .HaveColumnType("time(0)");

        // Store enums as strings for DB readability
        configurationBuilder
            .Properties<Enum>()
            .HaveConversion<string>();
    }
}

// ── Value Converters ─────────────────────────────────────────────────────────

internal sealed class DateOnlyConverter()
    : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateOnly, DateTime>(
        d => d.ToDateTime(TimeOnly.MinValue),
        dt => DateOnly.FromDateTime(dt));

internal sealed class TimeOnlyConverter()
    : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TimeOnly, TimeSpan>(
        t => t.ToTimeSpan(),
        ts => TimeOnly.FromTimeSpan(ts));
