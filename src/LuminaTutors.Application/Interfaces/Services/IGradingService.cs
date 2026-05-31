using LuminaTutors.Application.DTOs.Grading;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IGradingService
{
    // Score entry
    Task<Result<ScoreEntryDto>>               EnterScoreAsync(int schoolId, int teacherId, EnterScoreRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ScoreEntryDto>>> BulkEnterScoresAsync(int schoolId, int teacherId, BulkEnterScoreRequest request, CancellationToken ct = default);
    Task<Result>                               DeleteScoreAsync(int scoreEntryId, int teacherId, CancellationToken ct = default);

    // Grade book
    Task<Result<SubjectGradeBookDto>>          GetSubjectGradeBookAsync(int subjectAssignmentId, CancellationToken ct = default);
    Task<Result<StudentSemesterSummaryDto>>    GetStudentSemesterSummaryAsync(int studentId, int semesterId, CancellationToken ct = default);

    // TT22 calculation (calls SP_CalculateSubjectAverage)
    Task<Result<GradeBookRowDto>>              CalculateAverageAsync(int studentId, int subjectAssignmentId, CancellationToken ct = default);
    Task<Result<int>>                          CalculateAllAveragesAsync(int subjectAssignmentId, CancellationToken ct = default);

    // Lock / approve
    Task<Result> LockGradeBookAsync(int subjectAssignmentId, int approvedByUserId, CancellationToken ct = default);

    // Exams
    Task<Result<ExamDto>>                      CreateExamAsync(int schoolId, int createdByUserId, CreateExamRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ExamDto>>>       GetExamsAsync(int schoolId, int semesterId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ExamRoomDto>>>   GetExamRoomsAsync(int examId, CancellationToken ct = default);
    Task<Result<int>>                          AssignSeatsRandomAsync(int examId, CancellationToken ct = default);
}
