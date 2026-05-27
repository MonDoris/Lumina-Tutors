using AutoMapper;
using LuminaTutors.Application.DTOs.Grading;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Grading;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class GradingService : IGradingService
{
    private readonly IUnitOfWork           _uow;
    private readonly IMapper               _mapper;
    private readonly ILogger<GradingService> _logger;

    public GradingService(IUnitOfWork uow, IMapper mapper, ILogger<GradingService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── EnterScore ───────────────────────────────────────────────────────────

    public async Task<Result<ScoreEntryDto>> EnterScoreAsync(
        int schoolId, int teacherId, EnterScoreRequest request, CancellationToken ct = default)
    {
        var assignment = await _uow.SubjectAssignments.GetByIdAsync(request.SubjectAssignmentId, ct);
        if (assignment is null || assignment.SchoolId != schoolId || assignment.TeacherId != teacherId)
            return Result<ScoreEntryDto>.Failure("FORBIDDEN", "Bạn không có quyền nhập điểm cho môn học này.");

        if (request.Score < 0 || request.Score > 10)
            return Result<ScoreEntryDto>.Failure("INVALID_SCORE", "Điểm phải trong khoảng 0–10.");

        var gradeBooks = await _uow.GradeBooks.FindAsync(
            gb => gb.StudentId == request.StudentId &&
                  gb.SubjectAssignmentId == request.SubjectAssignmentId,
            ct: ct);

        if (gradeBooks.Any(gb => gb.IsLocked))
            return Result<ScoreEntryDto>.Failure("GRADEBOOK_LOCKED", "Sổ điểm đã bị khóa.");

        var category = await _uow.GradeCategories.GetByIdAsync(request.GradeCategoryId, ct);
        if (category is null)
            return Result<ScoreEntryDto>.Failure("NOT_FOUND", "Loại điểm không tồn tại.");

        if (!category.IsMultipleAllowed)
        {
            var existingEntry = await _uow.ScoreEntries.FindAsync(
                se => se.StudentId           == request.StudentId &&
                      se.SubjectAssignmentId == request.SubjectAssignmentId &&
                      se.GradeCategoryId     == request.GradeCategoryId &&
                      !se.IsLocked,
                ct: ct);

            if (existingEntry.Any())
                return Result<ScoreEntryDto>.Failure("SCORE_EXISTS",
                    $"Học sinh đã có điểm {category.CategoryName}. Chỉ được nhập 1 lần.");
        }

        var existingDtx = await _uow.ScoreEntries.FindAsync(
            se => se.StudentId           == request.StudentId &&
                  se.SubjectAssignmentId == request.SubjectAssignmentId &&
                  se.GradeCategoryId     == request.GradeCategoryId,
            ct: ct);

        byte order = (byte)(existingDtx.Count + 1);

        if (category.MaxCountPerSemester.HasValue && order > category.MaxCountPerSemester.Value)
            return Result<ScoreEntryDto>.Failure("MAX_EXCEEDED",
                $"Đã đạt số lượng điểm tối đa cho {category.CategoryName} ({category.MaxCountPerSemester.Value}).");

        var entry = new ScoreEntry
        {
            SubjectAssignmentId  = request.SubjectAssignmentId,
            StudentId            = request.StudentId,
            GradeCategoryId      = request.GradeCategoryId,
            Score                = request.Score,
            EntryOrder           = order,
            ExamDate             = request.ExamDate,
            Note                 = request.Note?.Trim(),
            EnteredByTeacherId   = teacherId,
            IsLocked             = false
        };

        await _uow.ScoreEntries.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Score entered: Student={StudentId}, Assignment={AssignmentId}, Category={Cat}, Score={Score}",
            request.StudentId, request.SubjectAssignmentId, category.CategoryCode, request.Score);

        var entries = await _uow.ScoreEntries.FindAsync(
            se => se.Id == entry.Id,
            include: q => q.Include(se => se.GradeCategory),
            ct: ct);

        return Result<ScoreEntryDto>.Success(_mapper.Map<ScoreEntryDto>(entries.First()));
    }

    // ─── BulkEnterScores ──────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ScoreEntryDto>>> BulkEnterScoresAsync(
        int schoolId, int teacherId, BulkEnterScoreRequest request, CancellationToken ct = default)
    {
        var results = new List<ScoreEntryDto>();

        foreach (var item in request.Scores)
        {
            var singleRequest = new EnterScoreRequest(
                SubjectAssignmentId: request.SubjectAssignmentId,
                StudentId:           item.StudentId,
                GradeCategoryId:     request.GradeCategoryId,
                EntryOrder:          request.EntryOrder,
                Score:               item.Score,
                ExamDate:            null,
                Note:                item.Note);

            var result = await EnterScoreAsync(schoolId, teacherId, singleRequest, ct);
            if (result.IsSuccess)
                results.Add(result.Data!);
        }

        return Result<IReadOnlyList<ScoreEntryDto>>.Success(results);
    }

    // ─── DeleteScore ──────────────────────────────────────────────────────────

    public async Task<Result> DeleteScoreAsync(int scoreEntryId, int teacherId, CancellationToken ct = default)
    {
        var entries = await _uow.ScoreEntries.FindAsync(
            se => se.Id == scoreEntryId,
            include: q => q.Include(se => se.SubjectAssignment),
            ct: ct);

        var entry = entries.FirstOrDefault();
        if (entry is null)
            return Result.Failure("NOT_FOUND", "Điểm không tồn tại.");

        if (entry.SubjectAssignment.TeacherId != teacherId)
            return Result.Failure("FORBIDDEN", "Bạn không có quyền xóa điểm này.");

        if (entry.IsLocked)
            return Result.Failure("LOCKED", "Điểm đã bị khóa, không thể xóa.");

        _uow.ScoreEntries.Remove(entry);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── GetSubjectGradeBook ──────────────────────────────────────────────────

    public async Task<Result<SubjectGradeBookDto>> GetSubjectGradeBookAsync(
        int subjectAssignmentId, CancellationToken ct = default)
    {
        var assignments = await _uow.SubjectAssignments.FindAsync(
            sa => sa.Id == subjectAssignmentId,
            include: q => q
                .Include(sa => sa.Subject)
                .Include(sa => sa.Semester)
                .Include(sa => sa.Teacher)
                .Include(sa => sa.Class)
                .Include(sa => sa.ScoreEntries).ThenInclude(se => se.GradeCategory)
                .Include(sa => sa.GradeBooks),
            ct: ct);

        var assignment = assignments.FirstOrDefault();
        if (assignment is null)
            return Result<SubjectGradeBookDto>.Failure("NOT_FOUND", "Phân công môn học không tồn tại.");

        var studentIds = assignment.ScoreEntries.Select(se => se.StudentId).Distinct().ToList();

        // Load StudentProfiles to get StudentCodes
        var studentProfiles = await _uow.StudentProfiles.FindAsync(
            sp => studentIds.Contains(sp.UserId),
            ct: ct);
        var profileByUserId = studentProfiles.ToDictionary(sp => sp.UserId);

        // Load Users for names
        var users = await _uow.Users.FindAsync(
            u => studentIds.Contains(u.Id),
            ct: ct);
        var userById = users.ToDictionary(u => u.Id);

        var rows = new List<GradeBookRowDto>();
        foreach (var studentId in studentIds)
        {
            var gb             = assignment.GradeBooks.FirstOrDefault(g => g.StudentId == studentId);
            var studentEntries = assignment.ScoreEntries.Where(se => se.StudentId == studentId).ToList();

            var regularScores = studentEntries
                .Where(se => se.GradeCategory?.CategoryCode == "DTX")
                .OrderBy(se => se.EntryOrder)
                .Select(se => (decimal?)se.Score)
                .ToList();

            var midTermScore = studentEntries
                .FirstOrDefault(se => se.GradeCategory?.CategoryCode == "DGK")?.Score;

            var finalScore = studentEntries
                .FirstOrDefault(se => se.GradeCategory?.CategoryCode == "DCK")?.Score;

            profileByUserId.TryGetValue(studentId, out var profile);
            userById.TryGetValue(studentId, out var user);

            rows.Add(new GradeBookRowDto(
                StudentId:    studentId,
                StudentCode:  profile?.StudentCode ?? string.Empty,
                StudentName:  user?.FullName ?? string.Empty,
                RegularScores: regularScores,
                MidTermScore: midTermScore,
                FinalScore:   finalScore,
                AverageScore: gb?.AverageScore,
                Remark:       gb?.Remark?.ToString(),
                IsCalculated: gb?.IsCalculated ?? false,
                IsLocked:     gb?.IsLocked ?? false));
        }

        var regularScoreSlots = rows.Any() ? rows.Max(r => r.RegularScores.Count) : 3;

        var dto = new SubjectGradeBookDto(
            SubjectAssignmentId: subjectAssignmentId,
            SubjectName:         assignment.Subject.SubjectName,
            TeacherName:         assignment.Teacher.FullName,
            ClassName:           assignment.Class.ClassName,
            SemesterName:        assignment.Semester?.SemesterName ?? string.Empty,
            RegularScoreSlots:   regularScoreSlots,
            Rows:                rows);

        return Result<SubjectGradeBookDto>.Success(dto);
    }

    // ─── GetStudentSemesterSummary ────────────────────────────────────────────

    public async Task<Result<StudentSemesterSummaryDto>> GetStudentSemesterSummaryAsync(
        int studentId, int semesterId, CancellationToken ct = default)
    {
        var user     = await _uow.Users.GetByIdAsync(studentId, ct);
        var semester = await _uow.Semesters.GetByIdAsync(semesterId, ct);

        var gradeBooks = await _uow.GradeBooks.FindAsync(
            gb => gb.StudentId == studentId &&
                  gb.SubjectAssignment.SemesterId == semesterId,
            include: q => q.Include(gb => gb.SubjectAssignment)
                           .ThenInclude(sa => sa.Subject),
            ct: ct);

        var subjectAverages = gradeBooks.Select(gb => new SubjectAverageDto(
            SubjectName:  gb.SubjectAssignment.Subject.SubjectName,
            AverageScore: gb.AverageScore,
            Remark:       gb.Remark?.ToString())).ToList();

        decimal? semesterGpa = subjectAverages.Any(r => r.AverageScore.HasValue)
            ? Math.Round(subjectAverages.Where(r => r.AverageScore.HasValue)
                                        .Average(r => r.AverageScore!.Value), 1)
            : null;

        string? semesterRemark = semesterGpa switch
        {
            >= 9.0m => "Xuất sắc",
            >= 7.0m => "Giỏi",
            >= 5.0m => "Trung bình",
            >= 3.5m => "Yếu",
            not null => "Kém",
            _        => null
        };

        var dto = new StudentSemesterSummaryDto(
            StudentId:      studentId,
            StudentName:    user?.FullName ?? string.Empty,
            SemesterId:     semesterId,
            SemesterName:   semester?.SemesterName ?? string.Empty,
            SubjectAverages: subjectAverages,
            SemesterGpa:    semesterGpa,
            SemesterRemark: semesterRemark,
            AbsenceCount:   0);

        return Result<StudentSemesterSummaryDto>.Success(dto);
    }

    // ─── CalculateAverage (calls SP_CalculateSubjectAverage) ─────────────────

    public async Task<Result<GradeBookRowDto>> CalculateAverageAsync(
        int studentId, int subjectAssignmentId, CancellationToken ct = default)
    {
        await _uow.ExecuteStoredProcedureAsync(
            "SP_CalculateSubjectAverage",
            new { SubjectAssignmentId = subjectAssignmentId, StudentId = studentId },
            ct);

        var gradeBooks = await _uow.GradeBooks.FindAsync(
            gb => gb.StudentId == studentId && gb.SubjectAssignmentId == subjectAssignmentId,
            ct: ct);

        var gb = gradeBooks.FirstOrDefault();
        if (gb is null)
            return Result<GradeBookRowDto>.Failure("NOT_FOUND", "Không tìm thấy sổ điểm.");

        var entries = await _uow.ScoreEntries.FindAsync(
            se => se.StudentId == studentId && se.SubjectAssignmentId == subjectAssignmentId,
            include: q => q.Include(se => se.GradeCategory),
            ct: ct);

        var profiles = await _uow.StudentProfiles.FindAsync(sp => sp.UserId == studentId, ct: ct);
        var user     = await _uow.Users.GetByIdAsync(studentId, ct);

        var regularScores = entries
            .Where(se => se.GradeCategory?.CategoryCode == "DTX")
            .OrderBy(se => se.EntryOrder)
            .Select(se => (decimal?)se.Score)
            .ToList();

        var midTermScore = entries
            .FirstOrDefault(se => se.GradeCategory?.CategoryCode == "DGK")?.Score;

        var finalScore = entries
            .FirstOrDefault(se => se.GradeCategory?.CategoryCode == "DCK")?.Score;

        var row = new GradeBookRowDto(
            StudentId:    studentId,
            StudentCode:  profiles.FirstOrDefault()?.StudentCode ?? string.Empty,
            StudentName:  user?.FullName ?? string.Empty,
            RegularScores: regularScores,
            MidTermScore: midTermScore,
            FinalScore:   finalScore,
            AverageScore: gb.AverageScore,
            Remark:       gb.Remark?.ToString(),
            IsCalculated: gb.IsCalculated,
            IsLocked:     gb.IsLocked);

        return Result<GradeBookRowDto>.Success(row);
    }

    // ─── CalculateAllAverages ─────────────────────────────────────────────────

    public async Task<Result<int>> CalculateAllAveragesAsync(
        int subjectAssignmentId, CancellationToken ct = default)
    {
        var students = await _uow.ScoreEntries.FindAsync(
            se => se.SubjectAssignmentId == subjectAssignmentId,
            ct: ct);

        var distinctStudents = students.Select(s => s.StudentId).Distinct().ToList();

        foreach (var studentId in distinctStudents)
        {
            await _uow.ExecuteStoredProcedureAsync(
                "SP_CalculateSubjectAverage",
                new { SubjectAssignmentId = subjectAssignmentId, StudentId = studentId },
                ct);
        }

        _logger.LogInformation(
            "CalculateAllAverages: processed {Count} students for assignment {Id}",
            distinctStudents.Count, subjectAssignmentId);

        return Result<int>.Success(distinctStudents.Count);
    }

    // ─── LockGradeBook ────────────────────────────────────────────────────────

    public async Task<Result> LockGradeBookAsync(
        int subjectAssignmentId, int approvedByUserId, CancellationToken ct = default)
    {
        var gradeBooks = await _uow.GradeBooks.FindAsync(
            gb => gb.SubjectAssignmentId == subjectAssignmentId,
            ct: ct);

        if (!gradeBooks.Any())
            return Result.Failure("NOT_FOUND", "Không có dữ liệu điểm để khóa.");

        var entries = await _uow.ScoreEntries.FindAsync(
            se => se.SubjectAssignmentId == subjectAssignmentId,
            ct: ct);

        foreach (var gb in gradeBooks)
        {
            gb.IsLocked         = true;
            gb.ApprovedByUserId = approvedByUserId;
            gb.ApprovedAt       = DateTime.UtcNow;
        }

        foreach (var se in entries)
            se.IsLocked = true;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "GradeBook locked for assignment {Id} by user {UserId}",
            subjectAssignmentId, approvedByUserId);

        return Result.Success();
    }

    // ─── CreateExam ───────────────────────────────────────────────────────────

    public async Task<Result<ExamDto>> CreateExamAsync(
        int schoolId, int createdByUserId, CreateExamRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ExamType>(request.ExamType, out var examType))
            return Result<ExamDto>.Failure("INVALID_TYPE", "Loại kỳ thi không hợp lệ.");

        var exam = new Exam
        {
            SchoolId        = schoolId,
            SemesterId      = request.SemesterId,
            SubjectId       = request.SubjectId,
            GradeLevelId    = request.GradeLevelId,
            ExamName        = request.ExamName.Trim(),
            ExamType        = examType,
            ExamDate        = request.ExamDate,
            StartTime       = request.StartTime,
            DurationMinutes = (short)request.DurationMinutes,
            MaxScore        = request.MaxScore,
            CreatedByUserId = createdByUserId
        };

        await _uow.Exams.AddAsync(exam, ct);
        await _uow.SaveChangesAsync(ct);

        var dto = _mapper.Map<ExamDto>(exam);
        return Result<ExamDto>.Success(dto);
    }

    // ─── GetExams ─────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ExamDto>>> GetExamsAsync(
        int schoolId, int semesterId, CancellationToken ct = default)
    {
        var exams = await _uow.Exams.FindAsync(
            e => e.SchoolId == schoolId && e.SemesterId == semesterId,
            ct: ct);

        var dtos = _mapper.Map<List<ExamDto>>(exams);
        return Result<IReadOnlyList<ExamDto>>.Success(dtos);
    }

    // ─── GetExamRooms ─────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ExamRoomDto>>> GetExamRoomsAsync(
        int examId, CancellationToken ct = default)
    {
        var rooms = await _uow.ExamRooms.FindAsync(
            r => r.ExamId == examId,
            include: q => q.Include(r => r.SeatAssignments)
                           .ThenInclude(s => s.Student),
            ct: ct);

        var dtos = _mapper.Map<List<ExamRoomDto>>(rooms);
        return Result<IReadOnlyList<ExamRoomDto>>.Success(dtos);
    }

    // ─── AssignSeatsRandom ────────────────────────────────────────────────────

    public async Task<Result<int>> AssignSeatsRandomAsync(int examId, CancellationToken ct = default)
    {
        await _uow.ExecuteStoredProcedureAsync(
            "SP_AssignExamSeatsRandom",
            new { ExamId = examId },
            ct);

        _logger.LogInformation("SP_AssignExamSeatsRandom executed for exam {ExamId}", examId);

        var seats = await _uow.ExamRoomAssignments.FindAsync(
            s => s.ExamRoom.ExamId == examId,
            include: q => q.Include(s => s.ExamRoom),
            ct: ct);

        return Result<int>.Success(seats.Count);
    }
}
