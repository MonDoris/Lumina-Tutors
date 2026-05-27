using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.DTOs.HR;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

// ─── HR Service ───────────────────────────────────────────────────────────────

public interface IHRService
{
    Task<Result<PagedResult<TeacherDetailDto>>> GetTeachersAsync(int schoolId, string? keyword, int page, int pageSize, CancellationToken ct = default);
    Task<Result<TeacherDetailDto>>              GetTeacherByIdAsync(int schoolId, int teacherId, CancellationToken ct = default);
    Task<Result<TeacherDetailDto>>              CreateTeacherAsync(int schoolId, CreateTeacherRequest request, CancellationToken ct = default);
    Task<Result<TeacherDetailDto>>              UpdateTeacherAsync(int schoolId, int teacherId, CancellationToken ct = default);
    Task<Result>                                DeactivateTeacherAsync(int schoolId, int teacherId, CancellationToken ct = default);
    Task<Result>                                CreateContractAsync(int schoolId, int createdByUserId, CreateContractRequest request, CancellationToken ct = default);
    Task<Result<PayrollDto>>                    CreatePayrollAsync(int schoolId, int createdByUserId, CreatePayrollRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<PayrollDto>>>     GetPayrollsAsync(int schoolId, byte month, short year, CancellationToken ct = default);
    Task<Result>                                ApprovePayrollAsync(int payrollId, int approvedByUserId, CancellationToken ct = default);
}

// ─── Discipline Service ───────────────────────────────────────────────────────

public interface IDisciplineService
{
    Task<Result<DisciplineRecordDto>>              CreateRecordAsync(int schoolId, int reportedByUserId, CreateDisciplineRecordRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<DisciplineRecordDto>>> GetRecordsAsync(int schoolId, int? studentId, string? status, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
    Task<Result>                                   ResolveRecordAsync(int recordId, string actionTaken, int resolvedByUserId, CancellationToken ct = default);
    Task<Result>                                   EscalateRecordAsync(int recordId, int escalateToUserId, CancellationToken ct = default);
    Task<Result>                                   RecordGateCheckAsync(int schoolId, int studentId, string checkType, int? checkedByUserId, bool isLate, string? note, CancellationToken ct = default);
    Task<Result<DailyDisciplineReportDto>>         GetDailyReportAsync(int schoolId, DateOnly date, CancellationToken ct = default);
}

// ─── Notification & Messaging Service ────────────────────────────────────────

public interface INotificationService
{
    Task<Result>                                    SendAsync(int schoolId, int? sentByUserId, SendNotificationRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<NotificationDto>>>    GetForUserAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<int>>                               GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<Result>                                    MarkReadAsync(int userId, int notificationId, CancellationToken ct = default);
    Task<Result>                                    MarkAllReadAsync(int userId, CancellationToken ct = default);
}

public interface IMessageService
{
    Task<Result<IReadOnlyList<ConversationDto>>>    GetConversationsAsync(int userId, CancellationToken ct = default);
    Task<Result<PagedResult<MessageDto>>>           GetMessagesAsync(int conversationId, int currentUserId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<MessageDto>>                        SendMessageAsync(int senderId, SendMessageRequest request, CancellationToken ct = default);
    Task<Result<ConversationDto>>                   StartConversationAsync(int initiatorId, int schoolId, StartConversationRequest request, CancellationToken ct = default);
    Task<Result>                                    DeleteMessageAsync(int messageId, int requestedByUserId, CancellationToken ct = default);
}

public interface INewsBoardService
{
    Task<Result<IReadOnlyList<NewsBoardDto>>>       GetPublishedAsync(int schoolId, int? classId, CancellationToken ct = default);
    Task<Result<NewsBoardDto>>                      CreateAsync(int schoolId, int publishedByUserId, CreateNewsRequest request, CancellationToken ct = default);
    Task<Result>                                    PublishAsync(int newsId, int publishedByUserId, CancellationToken ct = default);
    Task<Result>                                    DeleteAsync(int newsId, int requestedByUserId, CancellationToken ct = default);
}
