namespace LuminaTutors.Application.DTOs.Quiz;

// ─── Question Bank ────────────────────────────────────────────────────────────

public record QuizOptionDto(
    int    OptionId,
    char   OptionLabel,
    string OptionText,
    bool   IsCorrect      // only sent to teacher/after-submission views
);

public record QuizQuestionDto(
    int    QuestionId,
    int    SubjectId,
    string SubjectName,
    string QuestionText,
    string QuestionType,
    string DifficultyLevel,
    int?   GradeLevelId,
    string? GradeLevelName,
    string? ChapterTag,
    string? ExplanationText,
    bool   IsApproved,
    string TeacherName,
    DateTime CreatedAt,
    IReadOnlyList<QuizOptionDto> Options
);

public record CreateQuestionRequest(
    int    SubjectId,
    string QuestionText,
    string QuestionType,        // "MultipleChoice" | "TrueFalse"
    string DifficultyLevel,     // "Easy" | "Medium" | "Hard"
    int?   GradeLevelId,
    string? ChapterTag,
    string? ExplanationText,
    IReadOnlyList<CreateOptionRequest> Options
);

public record CreateOptionRequest(
    char   OptionLabel,
    string OptionText,
    bool   IsCorrect
);

public record UpdateQuestionRequest(
    string QuestionText,
    string DifficultyLevel,
    string? ChapterTag,
    string? ExplanationText,
    IReadOnlyList<CreateOptionRequest> Options
);

public record QuizQuestionFilterRequest(
    int?   SubjectId     = null,
    int?   GradeLevelId  = null,
    string? Difficulty   = null,
    string? Keyword      = null,
    bool?  ApprovedOnly  = null
);

// ─── Quiz Exam ────────────────────────────────────────────────────────────────

public record QuizExamDto(
    int    ExamId,
    int    SchoolId,
    int    SubjectId,
    string SubjectName,
    int?   GradeLevelId,
    string? GradeLevelName,
    string TeacherName,
    string Title,
    string? Description,
    int    TimeLimitMinutes,
    int    TotalQuestions,
    decimal PointsPerQuestion,
    decimal TotalPoints,
    string Status,
    DateTime? StartTime,
    DateTime? EndTime,
    bool   ShuffleQuestions,
    bool   ShuffleOptions,
    bool   ShowResultAfter,
    DateTime CreatedAt,
    int    AttemptCount             // how many students have taken it
);

public record CreateQuizExamRequest(
    int    SubjectId,
    int?   GradeLevelId,
    string Title,
    string? Description,
    int    TimeLimitMinutes,
    decimal PointsPerQuestion,
    bool   ShuffleQuestions,
    bool   ShuffleOptions,
    bool   ShowResultAfter,
    DateTime? StartTime,
    DateTime? EndTime,
    IReadOnlyList<int> QuestionIds   // ordered list from question bank
);

// ─── Student Attempt ──────────────────────────────────────────────────────────

/// <summary>
/// Returned when a student starts or resumes an attempt.
/// Questions are already shuffled according to the student's ShuffleSeed.
/// CorrectOptionId is NOT included — only returned in AttemptResultDto after submit.
/// </summary>
public record StudentAttemptDto(
    int    AttemptId,
    int    ExamId,
    string ExamTitle,
    string ExamCode,
    int    ShuffleSeed,
    int    TimeLimitMinutes,
    DateTime StartedAt,
    DateTime? SubmittedAt,
    string Status,
    IReadOnlyList<AttemptQuestionDto> Questions,
    IReadOnlyList<SavedAnswerDto>     SavedAnswers
);

public record AttemptQuestionDto(
    int    QuestionId,
    string QuestionText,
    string? ExplanationText,           // null while in progress, revealed after submit
    IReadOnlyList<AttemptOptionDto> Options
);

public record AttemptOptionDto(
    int    OptionId,
    char   OptionLabel,
    string OptionText
    // IsCorrect NOT included
);

public record SavedAnswerDto(
    int  QuestionId,
    int? SelectedOptionId
);

public record SaveAnswerRequest(
    int QuestionId,
    int? SelectedOptionId   // null = clear answer (skip)
);

/// <summary>Full result shown after submission.</summary>
public record AttemptResultDto(
    int    AttemptId,
    int    ExamId,
    string ExamTitle,
    string ExamCode,
    string StudentName,
    string StudentCode,
    DateTime StartedAt,
    DateTime SubmittedAt,
    decimal Score,
    int    TotalCorrect,
    int    TotalQuestions,
    decimal MaxScore,
    string Status,
    IReadOnlyList<ResultQuestionDto> Questions
);

public record ResultQuestionDto(
    int    QuestionId,
    string QuestionText,
    string? ExplanationText,
    int?   SelectedOptionId,
    int    CorrectOptionId,
    bool   IsCorrect,
    IReadOnlyList<QuizOptionDto> Options   // all options with IsCorrect revealed
);

// ─── Exam results list (teacher view) ────────────────────────────────────────

public record ExamResultSummaryDto(
    int    AttemptId,
    int    StudentId,
    string StudentName,
    string StudentCode,
    string ExamCode,
    decimal? Score,
    int    TotalCorrect,
    int    TotalQuestions,
    string Status,
    DateTime StartedAt,
    DateTime? SubmittedAt
);
