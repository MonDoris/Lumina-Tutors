using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IAttendanceService
{
    // Session lifecycle
    Task<Result<AttendanceSessionDto>> CreateSessionAsync(int schoolId, int teacherId, CreateSessionRequest request, CancellationToken ct = default);
    Task<Result<AttendanceSessionDto>> GetSessionAsync(int sessionId, CancellationToken ct = default);
    Task<Result<AttendanceSessionDto>> GetActiveSessionAsync(int scheduleId, DateOnly date, CancellationToken ct = default);
    Task<Result>                       CloseSessionAsync(int sessionId, int teacherId, CancellationToken ct = default);

    // QR scanning (student side)
    Task<Result<ScanQRResponse>> ScanQRAsync(ScanQRRequest request, CancellationToken ct = default);

    // Teacher overrides
    Task<Result> UpdateAttendanceAsync(int sessionId, int teacherId, UpdateAttendanceRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AttendanceRecordDto>>> GetSessionRecordsAsync(int sessionId, CancellationToken ct = default);

    // Reports
    Task<Result<DailyAttendanceReportDto>>      GetDailyReportAsync(int classId, DateOnly date, CancellationToken ct = default);
    Task<Result<StudentAttendanceSummaryDto>>   GetStudentSummaryAsync(int studentId, int semesterId, CancellationToken ct = default);

    // Notify parents of absent students
    Task<Result<int>> NotifyAbsentParentsAsync(int sessionId, CancellationToken ct = default);
}
