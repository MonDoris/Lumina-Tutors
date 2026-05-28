using LuminaTutors.Application.DTOs.Quiz;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class QuizService : IQuizService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<QuizService> _logger;

    public QuizService(IUnitOfWork uow, ILogger<QuizService> logger)
    {
        _uow    = uow;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ── Question Bank ──────────────────────────────────────────────────────────
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<Result<PagedResult<QuizQuestionDto>>> GetQuestionsAsync(
        int schoolId, QuizQuestionFilterRequest filter, int page, int pageSize,
        CancellationToken ct = default)
    {
        var paged = await _uow.QuestionBanks.GetPagedAsync(
            page, pageSize,
            filter: q =>
                q.SchoolId == schoolId &&
                (filter.SubjectId    == null || q.SubjectId      == filter.SubjectId) &&
                (filter.GradeLevelId == null || q.GradeLevelId   == filter.GradeLevelId) &&
                (filter.ApprovedOnly != true  || q.IsApproved)  &&
                (filter.Difficulty   == null  || q.DifficultyLevel.ToString() == filter.Difficulty) &&
                (filter.Keyword      == null  || q.QuestionText.Contains(filter.Keyword)),
            orderBy: q => q.OrderByDescending(x => x.CreatedAt),
            include: q => q
                .Include(x => x.Subject)
                .Include(x => x.GradeLevel)
                .Include(x => x.CreatedByTeacher)
                .Include(x => x.Options),
            ct: ct);

        var dtos = paged.Items.Select(MapQuestion).ToList();
        return Result<PagedResult<QuizQuestionDto>>.Success(
            PagedResult<QuizQuestionDto>.Create(dtos, paged.TotalCount, page, pageSize));
    }

    public async Task<Result<QuizQuestionDto>> GetQuestionByIdAsync(
        int schoolId, int questionId, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.GetByIdAsync(questionId,
            include: q => q
                .Include(x => x.Subject)
                .Include(x => x.GradeLevel)
                .Include(x => x.CreatedByTeacher)
                .Include(x => x.Options.OrderBy(o => o.OptionLabel)),
            ct: ct);

        if (q is null || q.SchoolId != schoolId)
            return Result<QuizQuestionDto>.Failure("Không tìm thấy câu hỏi.");

        return Result<QuizQuestionDto>.Success(MapQuestion(q));
    }

    public async Task<Result<QuizQuestionDto>> CreateQuestionAsync(
        int schoolId, int teacherId, CreateQuestionRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.QuestionText))
            return Result<QuizQuestionDto>.Failure("Nội dung câu hỏi không được để trống.");

        var correctCount = request.Options.Count(o => o.IsCorrect);
        if (correctCount != 1)
            return Result<QuizQuestionDto>.Failure("Phải có đúng 1 đáp án đúng.");

        if (request.Options.Count < 2)
            return Result<QuizQuestionDto>.Failure("Phải có ít nhất 2 lựa chọn.");

        if (!Enum.TryParse<QuestionType>(request.QuestionType, out var qType))
            qType = QuestionType.MultipleChoice;

        if (!Enum.TryParse<DifficultyLevel>(request.DifficultyLevel, out var diff))
            diff = DifficultyLevel.Medium;

        var question = new QuestionBank
        {
            SchoolId          = schoolId,
            SubjectId         = request.SubjectId,
            CreatedByTeacherId= teacherId,
            QuestionText      = request.QuestionText.Trim(),
            QuestionType      = qType,
            DifficultyLevel   = diff,
            GradeLevelId      = request.GradeLevelId,
            ChapterTag        = request.ChapterTag?.Trim(),
            ExplanationText   = request.ExplanationText?.Trim(),
            IsApproved        = false,
            Options           = request.Options.Select(o => new QuestionOption
            {
                OptionLabel = char.ToUpper(o.OptionLabel),
                OptionText  = o.OptionText.Trim(),
                IsCorrect   = o.IsCorrect
            }).ToList()
        };

        await _uow.QuestionBanks.AddAsync(question, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.QuestionBanks.GetByIdAsync(question.Id,
            include: q => q
                .Include(x => x.Subject)
                .Include(x => x.GradeLevel)
                .Include(x => x.CreatedByTeacher)
                .Include(x => x.Options.OrderBy(o => o.OptionLabel)),
            ct: ct);

        return Result<QuizQuestionDto>.Success(MapQuestion(created!));
    }

    public async Task<Result<QuizQuestionDto>> UpdateQuestionAsync(
        int schoolId, int questionId, UpdateQuestionRequest request,
        CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.GetByIdAsync(questionId,
            include: x => x.Include(q => q.Options), ct: ct);

        if (q is null || q.SchoolId != schoolId)
            return Result<QuizQuestionDto>.Failure("Không tìm thấy câu hỏi.");

        var correctCount = request.Options.Count(o => o.IsCorrect);
        if (correctCount != 1)
            return Result<QuizQuestionDto>.Failure("Phải có đúng 1 đáp án đúng.");

        if (!Enum.TryParse<DifficultyLevel>(request.DifficultyLevel, out var diff))
            diff = DifficultyLevel.Medium;

        q.QuestionText    = request.QuestionText.Trim();
        q.DifficultyLevel = diff;
        q.ChapterTag      = request.ChapterTag?.Trim();
        q.ExplanationText = request.ExplanationText?.Trim();
        q.IsApproved      = false; // re-review after edit

        // Replace options
        _uow.QuestionOptions.RemoveRange(q.Options);
        q.Options = request.Options.Select(o => new QuestionOption
        {
            QuestionId  = q.Id,
            OptionLabel = char.ToUpper(o.OptionLabel),
            OptionText  = o.OptionText.Trim(),
            IsCorrect   = o.IsCorrect
        }).ToList();

        _uow.QuestionBanks.Update(q);
        await _uow.SaveChangesAsync(ct);

        var updated = await _uow.QuestionBanks.GetByIdAsync(q.Id,
            include: x => x
                .Include(q => q.Subject)
                .Include(q => q.GradeLevel)
                .Include(q => q.CreatedByTeacher)
                .Include(q => q.Options.OrderBy(o => o.OptionLabel)),
            ct: ct);

        return Result<QuizQuestionDto>.Success(MapQuestion(updated!));
    }

    public async Task<Result> DeleteQuestionAsync(
        int schoolId, int questionId, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.GetByIdAsync(questionId, ct: ct);
        if (q is null || q.SchoolId != schoolId)
            return Result.Failure("Không tìm thấy câu hỏi.");

        // Check if used in any exam
        var inUse = await _uow.QuizExamQuestions.AnyAsync(
            eq => eq.QuestionId == questionId, ct);
        if (inUse)
            return Result.Failure("Câu hỏi đang được sử dụng trong đề thi, không thể xóa.");

        _uow.QuestionBanks.Remove(q);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ApproveQuestionAsync(
        int schoolId, int questionId, CancellationToken ct = default)
    {
        var q = await _uow.QuestionBanks.GetByIdAsync(questionId, ct: ct);
        if (q is null || q.SchoolId != schoolId)
            return Result.Failure("Không tìm thấy câu hỏi.");

        q.IsApproved = true;
        _uow.QuestionBanks.Update(q);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ── Quiz Exam ──────────────────────────────────────────────────────────────
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<Result<PagedResult<QuizExamDto>>> GetExamsAsync(
        int schoolId, int? createdByUserId, string? status, int page, int pageSize,
        CancellationToken ct = default)
    {
        QuizExamStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<QuizExamStatus>(status, out var s))
            statusEnum = s;

        var paged = await _uow.QuizExams.GetPagedAsync(
            page, pageSize,
            filter: e =>
                e.SchoolId == schoolId &&
                (createdByUserId == null || e.CreatedByTeacherId == createdByUserId) &&
                (statusEnum == null || e.Status == statusEnum),
            orderBy: q => q.OrderByDescending(x => x.CreatedAt),
            include: q => q
                .Include(x => x.Subject)
                .Include(x => x.GradeLevel)
                .Include(x => x.CreatedByTeacher)
                .Include(x => x.Attempts),
            ct: ct);

        var dtos = paged.Items.Select(MapExam).ToList();
        return Result<PagedResult<QuizExamDto>>.Success(
            PagedResult<QuizExamDto>.Create(dtos, paged.TotalCount, page, pageSize));
    }

    public async Task<Result<QuizExamDto>> GetExamByIdAsync(
        int schoolId, int examId, CancellationToken ct = default)
    {
        var exam = await _uow.QuizExams.GetByIdAsync(examId,
            include: q => q
                .Include(x => x.Subject)
                .Include(x => x.GradeLevel)
                .Include(x => x.CreatedByTeacher)
                .Include(x => x.Attempts),
            ct: ct);

        if (exam is null || exam.SchoolId != schoolId)
            return Result<QuizExamDto>.Failure("Không tìm thấy đề thi.");

        return Result<QuizExamDto>.Success(MapExam(exam));
    }

    public async Task<Result<QuizExamDto>> CreateExamAsync(
        int schoolId, int teacherId, CreateQuizExamRequest request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<QuizExamDto>.Failure("Tên đề thi không được để trống.");

        if (request.QuestionIds.Count == 0)
            return Result<QuizExamDto>.Failure("Phải chọn ít nhất 1 câu hỏi.");

        // Verify questions belong to this school
        var questions = await _uow.QuestionBanks.FindAsync(
            q => request.QuestionIds.Contains(q.Id) && q.SchoolId == schoolId, ct: ct);

        if (questions.Count != request.QuestionIds.Count)
            return Result<QuizExamDto>.Failure("Một số câu hỏi không tồn tại hoặc không thuộc trường.");

        var exam = new QuizExam
        {
            SchoolId          = schoolId,
            SubjectId         = request.SubjectId,
            GradeLevelId      = request.GradeLevelId,
            CreatedByTeacherId= teacherId,
            Title             = request.Title.Trim(),
            Description       = request.Description?.Trim(),
            TimeLimitMinutes  = request.TimeLimitMinutes,
            TotalQuestions    = request.QuestionIds.Count,
            PointsPerQuestion = request.PointsPerQuestion,
            ShuffleQuestions  = request.ShuffleQuestions,
            ShuffleOptions    = request.ShuffleOptions,
            ShowResultAfter   = request.ShowResultAfter,
            StartTime         = request.StartTime,
            EndTime           = request.EndTime,
            Status            = QuizExamStatus.Draft,
            Questions         = request.QuestionIds.Select((qId, idx) => new QuizExamQuestion
            {
                QuestionId = qId,
                OrderIndex = idx
            }).ToList()
        };

        await _uow.QuizExams.AddAsync(exam, ct);
        await _uow.SaveChangesAsync(ct);

        var created = await _uow.QuizExams.GetByIdAsync(exam.Id,
            include: q => q
                .Include(x => x.Subject)
                .Include(x => x.GradeLevel)
                .Include(x => x.CreatedByTeacher)
                .Include(x => x.Attempts),
            ct: ct);

        return Result<QuizExamDto>.Success(MapExam(created!));
    }

    public async Task<Result> PublishExamAsync(
        int schoolId, int examId, CancellationToken ct = default)
    {
        var exam = await _uow.QuizExams.GetByIdAsync(examId, ct: ct);
        if (exam is null || exam.SchoolId != schoolId)
            return Result.Failure("Không tìm thấy đề thi.");
        if (exam.Status == QuizExamStatus.Closed)
            return Result.Failure("Đề thi đã đóng, không thể mở lại.");

        exam.Status = QuizExamStatus.Published;
        _uow.QuizExams.Update(exam);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> CloseExamAsync(
        int schoolId, int examId, CancellationToken ct = default)
    {
        var exam = await _uow.QuizExams.GetByIdAsync(examId, ct: ct);
        if (exam is null || exam.SchoolId != schoolId)
            return Result.Failure("Không tìm thấy đề thi.");

        exam.Status = QuizExamStatus.Closed;
        _uow.QuizExams.Update(exam);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteExamAsync(
        int schoolId, int examId, CancellationToken ct = default)
    {
        var exam = await _uow.QuizExams.GetByIdAsync(examId, ct: ct);
        if (exam is null || exam.SchoolId != schoolId)
            return Result.Failure("Không tìm thấy đề thi.");
        if (exam.Status == QuizExamStatus.Published)
            return Result.Failure("Không thể xóa đề thi đang mở. Hãy đóng đề thi trước.");

        _uow.QuizExams.Remove(exam);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ── Student Attempts ───────────────────────────────────────────────────────
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<Result<StudentAttemptDto>> StartAttemptAsync(
        int schoolId, int studentId, int examId, CancellationToken ct = default)
    {
        // Load exam with questions
        var exam = await _uow.QuizExams.GetByIdAsync(examId,
            include: q => q
                .Include(x => x.Questions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Options),
            ct: ct);

        if (exam is null || exam.SchoolId != schoolId)
            return Result<StudentAttemptDto>.Failure("Không tìm thấy đề thi.");
        if (exam.Status != QuizExamStatus.Published)
            return Result<StudentAttemptDto>.Failure("Đề thi chưa được mở hoặc đã đóng.");

        // Check time window
        var now = DateTime.UtcNow;
        if (exam.StartTime.HasValue && now < exam.StartTime.Value)
            return Result<StudentAttemptDto>.Failure("Đề thi chưa đến giờ mở.");
        if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            return Result<StudentAttemptDto>.Failure("Đề thi đã hết thời gian.");

        // Already has an attempt?
        var existing = await _uow.StudentQuizAttempts.FirstOrDefaultAsync(
            a => a.ExamId == examId && a.StudentId == studentId, ct);

        if (existing != null)
        {
            // Return existing in-progress attempt
            return await BuildAttemptDto(existing.Id, exam, ct);
        }

        // Count existing attempts for exam code generation
        var attemptCount = await _uow.StudentQuizAttempts.CountAsync(
            a => a.ExamId == examId, ct);

        var seed      = (examId * 1_000_003) ^ (studentId * 997);
        var examCode  = $"MĐ{(attemptCount + 1):D3}";

        var attempt = new StudentQuizAttempt
        {
            ExamId      = examId,
            StudentId   = studentId,
            ExamCode    = examCode,
            ShuffleSeed = seed,
            StartedAt   = now,
            Status      = AttemptStatus.InProgress
        };

        await _uow.StudentQuizAttempts.AddAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);

        return await BuildAttemptDto(attempt.Id, exam, ct);
    }

    public async Task<Result<StudentAttemptDto>> GetMyAttemptAsync(
        int studentId, int examId, CancellationToken ct = default)
    {
        var attempt = await _uow.StudentQuizAttempts.FirstOrDefaultAsync(
            a => a.ExamId == examId && a.StudentId == studentId, ct);

        if (attempt is null)
            return Result<StudentAttemptDto>.Failure("Chưa bắt đầu làm bài.");

        var exam = await _uow.QuizExams.GetByIdAsync(examId,
            include: q => q
                .Include(x => x.Questions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Options),
            ct: ct);

        return await BuildAttemptDto(attempt.Id, exam!, ct);
    }

    public async Task<Result> SaveAnswerAsync(
        int attemptId, int studentId, SaveAnswerRequest request,
        CancellationToken ct = default)
    {
        var attempt = await _uow.StudentQuizAttempts.GetByIdAsync(attemptId, ct: ct);
        if (attempt is null || attempt.StudentId != studentId)
            return Result.Failure("Không tìm thấy bài làm.");
        if (attempt.Status != AttemptStatus.InProgress)
            return Result.Failure("Bài làm đã nộp.");

        // Upsert answer
        var existing = await _uow.StudentQuizAnswers.FirstOrDefaultAsync(
            a => a.AttemptId == attemptId && a.QuestionId == request.QuestionId, ct);

        if (existing is null)
        {
            await _uow.StudentQuizAnswers.AddAsync(new StudentQuizAnswer
            {
                AttemptId        = attemptId,
                QuestionId       = request.QuestionId,
                SelectedOptionId = request.SelectedOptionId
            }, ct);
        }
        else
        {
            existing.SelectedOptionId = request.SelectedOptionId;
            _uow.StudentQuizAnswers.Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AttemptResultDto>> SubmitAttemptAsync(
        int attemptId, int studentId, CancellationToken ct = default)
    {
        var attempt = await _uow.StudentQuizAttempts.GetByIdAsync(attemptId,
            include: q => q
                .Include(x => x.Answers)
                .Include(x => x.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Options),
            ct: ct);

        if (attempt is null || attempt.StudentId != studentId)
            return Result<AttemptResultDto>.Failure("Không tìm thấy bài làm.");
        if (attempt.Status != AttemptStatus.InProgress)
            return Result<AttemptResultDto>.Failure("Bài làm đã được nộp trước đó.");

        // Grade answers
        int correct = 0;
        foreach (var answer in attempt.Answers)
        {
            if (answer.SelectedOptionId.HasValue)
            {
                var option = attempt.Exam.Questions
                    .SelectMany(q => q.Question.Options)
                    .FirstOrDefault(o => o.Id == answer.SelectedOptionId.Value);

                answer.IsCorrect = option?.IsCorrect ?? false;
                if (answer.IsCorrect) correct++;
                _uow.StudentQuizAnswers.Update(answer);
            }
        }

        var totalQ   = attempt.Exam.TotalQuestions;
        var score    = totalQ > 0
            ? Math.Round((decimal)correct / totalQ * (totalQ * attempt.Exam.PointsPerQuestion), 2)
            : 0M;

        attempt.TotalCorrect = correct;
        attempt.Score        = score;
        attempt.SubmittedAt  = DateTime.UtcNow;
        attempt.Status       = AttemptStatus.Submitted;
        _uow.StudentQuizAttempts.Update(attempt);

        await _uow.SaveChangesAsync(ct);

        return await GetAttemptResultAsync(attemptId, studentId, ct);
    }

    public async Task<Result<PagedResult<ExamResultSummaryDto>>> GetExamResultsAsync(
        int schoolId, int examId, int page, int pageSize, CancellationToken ct = default)
    {
        // Verify exam belongs to school
        var exam = await _uow.QuizExams.GetByIdAsync(examId, ct: ct);
        if (exam is null || exam.SchoolId != schoolId)
            return Result<PagedResult<ExamResultSummaryDto>>.Failure("Không tìm thấy đề thi.");

        var paged = await _uow.StudentQuizAttempts.GetPagedAsync(
            page, pageSize,
            filter: a => a.ExamId == examId,
            orderBy: q => q.OrderBy(a => a.ExamCode),
            include: q => q
                .Include(a => a.Student)
                    .ThenInclude(u => u.StudentProfile),
            ct: ct);

        var dtos = paged.Items.Select(a => new ExamResultSummaryDto(
            AttemptId:     a.Id,
            StudentId:     a.StudentId,
            StudentName:   a.Student.FullName,
            StudentCode:   a.Student.StudentProfile?.StudentCode ?? "",
            ExamCode:      a.ExamCode,
            Score:         a.Score,
            TotalCorrect:  a.TotalCorrect,
            TotalQuestions:exam.TotalQuestions,
            Status:        a.Status.ToString(),
            StartedAt:     a.StartedAt,
            SubmittedAt:   a.SubmittedAt
        )).ToList();

        return Result<PagedResult<ExamResultSummaryDto>>.Success(
            PagedResult<ExamResultSummaryDto>.Create(dtos, paged.TotalCount, page, pageSize));
    }

    public async Task<Result<AttemptResultDto>> GetAttemptResultAsync(
        int attemptId, int requestingUserId, CancellationToken ct = default)
    {
        var attempt = await _uow.StudentQuizAttempts.GetByIdAsync(attemptId,
            include: q => q
                .Include(a => a.Student)
                    .ThenInclude(u => u.StudentProfile)
                .Include(a => a.Answers)
                .Include(a => a.Exam)
                    .ThenInclude(e => e.Questions.OrderBy(q => q.OrderIndex))
                        .ThenInclude(eq => eq.Question)
                            .ThenInclude(q => q.Options.OrderBy(o => o.OptionLabel)),
            ct: ct);

        if (attempt is null)
            return Result<AttemptResultDto>.Failure("Không tìm thấy kết quả.");

        // Students can only see their own result; teachers can see all
        if (attempt.StudentId != requestingUserId)
        {
            var isTeacher = await _uow.QuizExams.AnyAsync(
                e => e.Id == attempt.ExamId && e.CreatedByTeacherId == requestingUserId, ct);
            if (!isTeacher)
                return Result<AttemptResultDto>.Failure("Không có quyền xem kết quả này.");
        }

        var exam       = attempt.Exam;
        var questions  = ShuffleList(
            exam.Questions.OrderBy(q => q.OrderIndex).ToList(),
            exam.ShuffleQuestions ? attempt.ShuffleSeed : 0);

        var resultQuestions = questions.Select(eq =>
        {
            var q      = eq.Question;
            var answer = attempt.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
            var opts   = ShuffleList(q.Options.ToList(),
                exam.ShuffleOptions ? attempt.ShuffleSeed ^ q.Id : 0);
            var correct= q.Options.First(o => o.IsCorrect);

            return new ResultQuestionDto(
                QuestionId:      q.Id,
                QuestionText:    q.QuestionText,
                ExplanationText: q.ExplanationText,
                SelectedOptionId:answer?.SelectedOptionId,
                CorrectOptionId: correct.Id,
                IsCorrect:       answer?.IsCorrect ?? false,
                Options:         opts.Select(o => new QuizOptionDto(
                    o.Id, o.OptionLabel, o.OptionText, o.IsCorrect)).ToList()
            );
        }).ToList();

        var maxScore = exam.TotalQuestions * exam.PointsPerQuestion;
        return Result<AttemptResultDto>.Success(new AttemptResultDto(
            AttemptId:      attempt.Id,
            ExamId:         exam.Id,
            ExamTitle:      exam.Title,
            ExamCode:       attempt.ExamCode,
            StudentName:    attempt.Student.FullName,
            StudentCode:    attempt.Student.StudentProfile?.StudentCode ?? "",
            StartedAt:      attempt.StartedAt,
            SubmittedAt:    attempt.SubmittedAt ?? DateTime.UtcNow,
            Score:          attempt.Score ?? 0M,
            TotalCorrect:   attempt.TotalCorrect,
            TotalQuestions: exam.TotalQuestions,
            MaxScore:       maxScore,
            Status:         attempt.Status.ToString(),
            Questions:      resultQuestions
        ));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ── Private helpers ────────────────────────────────────────────────────────
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deterministic Fisher-Yates shuffle. seed=0 returns the original order.
    /// </summary>
    private static List<T> ShuffleList<T>(List<T> list, int seed)
    {
        if (seed == 0 || list.Count <= 1) return list;
        var rng    = new Random(seed);
        var result = list.ToList();
        for (int n = result.Count - 1; n > 0; n--)
        {
            int k = rng.Next(n + 1);
            (result[k], result[n]) = (result[n], result[k]);
        }
        return result;
    }

    private async Task<Result<StudentAttemptDto>> BuildAttemptDto(
        int attemptId, QuizExam exam, CancellationToken ct)
    {
        var attempt = await _uow.StudentQuizAttempts.GetByIdAsync(attemptId,
            include: q => q.Include(a => a.Answers), ct: ct);

        if (attempt is null)
            return Result<StudentAttemptDto>.Failure("Không tìm thấy bài làm.");

        var questions = ShuffleList(
            exam.Questions.OrderBy(q => q.OrderIndex).ToList(),
            exam.ShuffleQuestions ? attempt.ShuffleSeed : 0);

        var questionDtos = questions.Select(eq =>
        {
            var q    = eq.Question;
            var opts = ShuffleList(q.Options.ToList(),
                exam.ShuffleOptions ? attempt.ShuffleSeed ^ q.Id : 0);
            return new AttemptQuestionDto(
                QuestionId:      q.Id,
                QuestionText:    q.QuestionText,
                ExplanationText: null,  // hidden during exam
                Options:         opts.Select(o => new AttemptOptionDto(
                    o.Id, o.OptionLabel, o.OptionText)).ToList()
            );
        }).ToList();

        var savedAnswers = attempt.Answers
            .Select(a => new SavedAnswerDto(a.QuestionId, a.SelectedOptionId))
            .ToList();

        return Result<StudentAttemptDto>.Success(new StudentAttemptDto(
            AttemptId:       attempt.Id,
            ExamId:          exam.Id,
            ExamTitle:       exam.Title,
            ExamCode:        attempt.ExamCode,
            ShuffleSeed:     attempt.ShuffleSeed,
            TimeLimitMinutes:exam.TimeLimitMinutes,
            StartedAt:       attempt.StartedAt,
            SubmittedAt:     attempt.SubmittedAt,
            Status:          attempt.Status.ToString(),
            Questions:       questionDtos,
            SavedAnswers:    savedAnswers
        ));
    }

    private static QuizQuestionDto MapQuestion(QuestionBank q) => new(
        QuestionId:     q.Id,
        SubjectId:      q.SubjectId,
        SubjectName:    q.Subject?.SubjectName ?? "",
        QuestionText:   q.QuestionText,
        QuestionType:   q.QuestionType.ToString(),
        DifficultyLevel:q.DifficultyLevel.ToString(),
        GradeLevelId:   q.GradeLevelId,
        GradeLevelName: q.GradeLevel?.GradeName,
        ChapterTag:     q.ChapterTag,
        ExplanationText:q.ExplanationText,
        IsApproved:     q.IsApproved,
        TeacherName:    q.CreatedByTeacher?.FullName ?? "",
        CreatedAt:      q.CreatedAt,
        Options:        q.Options
            .OrderBy(o => o.OptionLabel)
            .Select(o => new QuizOptionDto(o.Id, o.OptionLabel, o.OptionText, o.IsCorrect))
            .ToList()
    );

    private static QuizExamDto MapExam(QuizExam e) => new(
        ExamId:           e.Id,
        SchoolId:         e.SchoolId,
        SubjectId:        e.SubjectId,
        SubjectName:      e.Subject?.SubjectName ?? "",
        GradeLevelId:     e.GradeLevelId,
        GradeLevelName:   e.GradeLevel?.GradeName,
        TeacherName:      e.CreatedByTeacher?.FullName ?? "",
        Title:            e.Title,
        Description:      e.Description,
        TimeLimitMinutes: e.TimeLimitMinutes,
        TotalQuestions:   e.TotalQuestions,
        PointsPerQuestion:e.PointsPerQuestion,
        TotalPoints:      e.TotalQuestions * e.PointsPerQuestion,
        Status:           e.Status.ToString(),
        StartTime:        e.StartTime,
        EndTime:          e.EndTime,
        ShuffleQuestions: e.ShuffleQuestions,
        ShuffleOptions:   e.ShuffleOptions,
        ShowResultAfter:  e.ShowResultAfter,
        CreatedAt:        e.CreatedAt,
        AttemptCount:     e.Attempts?.Count(a => a.Status == AttemptStatus.Submitted) ?? 0
    );
}
