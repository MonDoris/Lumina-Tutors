using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using LuminaTutors.Domain.Common;
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
using LuminaTutors.Domain.Interfaces.Repositories;
using LuminaTutors.Infrastructure.Data;

namespace LuminaTutors.Infrastructure.Repositories;

/// <summary>
/// Unit of Work — wraps DbContext and exposes all repositories.
/// Manages transaction lifecycle for complex business operations.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly LuminaTutorsDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy-initialized backing fields
    private IRepository<School>?       _schools;
    private IRepository<Role>?         _roles;
    private IRepository<User>?         _users;
    private IRepository<RefreshToken>? _refreshTokens;
    private IRepository<InviteLink>?   _inviteLinks;

    private IRepository<AcademicYear>?      _academicYears;
    private IRepository<Semester>?          _semesters;
    private IRepository<GradeLevel>?        _gradeLevels;
    private IRepository<Subject>?           _subjects;
    private IRepository<Class>?             _classes;
    private IRepository<ClassEnrollment>?   _classEnrollments;
    private IRepository<SubjectAssignment>? _subjectAssignments;

    private IRepository<StudentProfile>?        _studentProfiles;
    private IRepository<TeacherProfile>?        _teacherProfiles;
    private IRepository<ParentProfile>?         _parentProfiles;
    private IRepository<SupervisorProfile>?     _supervisorProfiles;
    private IRepository<AccountantProfile>?     _accountantProfiles;
    private IRepository<ParentStudentRelation>? _parentStudentRelations;

    private IRepository<Schedule>?          _schedules;
    private IRepository<ScheduleChangeLog>? _scheduleChangeLogs;
    private IRepository<AttendanceSession>? _attendanceSessions;
    private IRepository<StudentAttendance>? _studentAttendances;
    private IRepository<SchoolEvent>?       _schoolEvents;
    private IRepository<EventAttendance>?   _eventAttendances;

    private IRepository<Lesson>?               _lessons;
    private IRepository<LessonMaterial>?       _lessonMaterials;
    private IRepository<QuestionBank>?         _questionBanks;
    private IRepository<QuestionOption>?       _questionOptions;
    private IRepository<Assignment>?           _assignments;
    private IRepository<AssignmentSubmission>? _assignmentSubmissions;
    private IRepository<SubmissionFile>?       _submissionFiles;
    private IRepository<VirtualLabSession>?    _virtualLabSessions;

    private IRepository<GradeCategory>?           _gradeCategories;
    private IRepository<SubjectGradeRequirement>? _subjectGradeRequirements;
    private IRepository<ScoreEntry>?              _scoreEntries;
    private IRepository<GradeBook>?               _gradeBooks;
    private IRepository<Exam>?                    _exams;
    private IRepository<ExamRoom>?                _examRooms;
    private IRepository<ExamRoomAssignment>?      _examRoomAssignments;

    private IRepository<TuitionFeeConfig>? _tuitionFeeConfigs;
    private IRepository<TuitionInvoice>?   _tuitionInvoices;
    private IRepository<TuitionPayment>?   _tuitionPayments;

    private IRepository<TeacherContract>?   _teacherContracts;
    private IRepository<StaffAttendance>?   _staffAttendances;
    private IRepository<Payroll>?           _payrolls;
    private IRepository<TeacherEvaluation>? _teacherEvaluations;

    private IRepository<DisciplineRecord>? _disciplineRecords;
    private IRepository<GateCheckLog>?     _gateCheckLogs;

    private IRepository<Notification>?            _notifications;
    private IRepository<NotificationRecipient>?   _notificationRecipients;
    private IRepository<Conversation>?            _conversations;
    private IRepository<ConversationParticipant>? _conversationParticipants;
    private IRepository<Message>?                 _messages;
    private IRepository<NewsBoard>?               _newsBoards;

    public UnitOfWork(LuminaTutorsDbContext context) => _context = context;

    // ── Lazy property helpers ─────────────────────────────────────────────────

    private IRepository<T> Get<T>(ref IRepository<T>? field) where T : BaseEntity
        => field ??= new GenericRepository<T>(_context);

    // ── Repository Properties ─────────────────────────────────────────────────

    public IRepository<School>       Schools       => Get(ref _schools);
    public IRepository<Role>         Roles         => Get(ref _roles);
    public IRepository<User>         Users         => Get(ref _users);
    public IRepository<RefreshToken> RefreshTokens => Get(ref _refreshTokens);
    public IRepository<InviteLink>   InviteLinks   => Get(ref _inviteLinks);

    public IRepository<AcademicYear>      AcademicYears      => Get(ref _academicYears);
    public IRepository<Semester>          Semesters          => Get(ref _semesters);
    public IRepository<GradeLevel>        GradeLevels        => Get(ref _gradeLevels);
    public IRepository<Subject>           Subjects           => Get(ref _subjects);
    public IRepository<Class>             Classes            => Get(ref _classes);
    public IRepository<ClassEnrollment>   ClassEnrollments   => Get(ref _classEnrollments);
    public IRepository<SubjectAssignment> SubjectAssignments => Get(ref _subjectAssignments);

    public IRepository<StudentProfile>        StudentProfiles        => Get(ref _studentProfiles);
    public IRepository<TeacherProfile>        TeacherProfiles        => Get(ref _teacherProfiles);
    public IRepository<ParentProfile>         ParentProfiles         => Get(ref _parentProfiles);
    public IRepository<SupervisorProfile>     SupervisorProfiles     => Get(ref _supervisorProfiles);
    public IRepository<AccountantProfile>     AccountantProfiles     => Get(ref _accountantProfiles);
    public IRepository<ParentStudentRelation> ParentStudentRelations => Get(ref _parentStudentRelations);

    public IRepository<Schedule>          Schedules          => Get(ref _schedules);
    public IRepository<ScheduleChangeLog> ScheduleChangeLogs => Get(ref _scheduleChangeLogs);
    public IRepository<AttendanceSession> AttendanceSessions => Get(ref _attendanceSessions);
    public IRepository<StudentAttendance> StudentAttendances => Get(ref _studentAttendances);
    public IRepository<SchoolEvent>       SchoolEvents       => Get(ref _schoolEvents);
    public IRepository<EventAttendance>   EventAttendances   => Get(ref _eventAttendances);

    public IRepository<Lesson>               Lessons               => Get(ref _lessons);
    public IRepository<LessonMaterial>       LessonMaterials       => Get(ref _lessonMaterials);
    public IRepository<QuestionBank>         QuestionBanks         => Get(ref _questionBanks);
    public IRepository<QuestionOption>       QuestionOptions       => Get(ref _questionOptions);
    public IRepository<Assignment>           Assignments           => Get(ref _assignments);
    public IRepository<AssignmentSubmission> AssignmentSubmissions => Get(ref _assignmentSubmissions);
    public IRepository<SubmissionFile>       SubmissionFiles       => Get(ref _submissionFiles);
    public IRepository<VirtualLabSession>    VirtualLabSessions    => Get(ref _virtualLabSessions);

    public IRepository<GradeCategory>           GradeCategories          => Get(ref _gradeCategories);
    public IRepository<SubjectGradeRequirement> SubjectGradeRequirements => Get(ref _subjectGradeRequirements);
    public IRepository<ScoreEntry>              ScoreEntries             => Get(ref _scoreEntries);
    public IRepository<GradeBook>               GradeBooks               => Get(ref _gradeBooks);
    public IRepository<Exam>                    Exams                    => Get(ref _exams);
    public IRepository<ExamRoom>                ExamRooms                => Get(ref _examRooms);
    public IRepository<ExamRoomAssignment>      ExamRoomAssignments      => Get(ref _examRoomAssignments);

    public IRepository<TuitionFeeConfig> TuitionFeeConfigs => Get(ref _tuitionFeeConfigs);
    public IRepository<TuitionInvoice>   TuitionInvoices   => Get(ref _tuitionInvoices);
    public IRepository<TuitionPayment>   TuitionPayments   => Get(ref _tuitionPayments);

    public IRepository<TeacherContract>   TeacherContracts   => Get(ref _teacherContracts);
    public IRepository<StaffAttendance>   StaffAttendances   => Get(ref _staffAttendances);
    public IRepository<Payroll>           Payrolls           => Get(ref _payrolls);
    public IRepository<TeacherEvaluation> TeacherEvaluations => Get(ref _teacherEvaluations);

    public IRepository<DisciplineRecord> DisciplineRecords => Get(ref _disciplineRecords);
    public IRepository<GateCheckLog>     GateCheckLogs     => Get(ref _gateCheckLogs);

    public IRepository<Notification>            Notifications            => Get(ref _notifications);
    public IRepository<NotificationRecipient>   NotificationRecipients   => Get(ref _notificationRecipients);
    public IRepository<Conversation>            Conversations            => Get(ref _conversations);
    public IRepository<ConversationParticipant> ConversationParticipants => Get(ref _conversationParticipants);
    public IRepository<Message>                 Messages                 => Get(ref _messages);
    public IRepository<NewsBoard>               NewsBoards               => Get(ref _newsBoards);

    // ── Transaction Management ────────────────────────────────────────────────

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc/>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        // SqlServerRetryingExecutionStrategy requires the entire operation (including
        // the transaction) to be wrapped inside CreateExecutionStrategy().ExecuteAsync().
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                await action();
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    // ── Stored Procedure Execution ────────────────────────────────────────────

    public async Task<int> ExecuteStoredProcedureAsync(
        string spName,
        object? parameters = null,
        CancellationToken ct = default)
    {
        var sql = $"EXEC {spName}";
        if (parameters is not null)
        {
            var props = parameters.GetType().GetProperties();
            var paramList = props.Select(p => $"@{p.Name}");
            sql += " " + string.Join(", ", paramList);
        }

        return await _context.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
