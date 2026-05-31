using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.QuestionBank;

// ─── Question ─────────────────────────────────────────────────────────────────

public record QuestionDto(
    int            Id,
    int            SubjectId,
    string         SubjectName,
    string         QuestionText,
    QuestionType   QuestionType,
    DifficultyLevel Difficulty,
    int?           GradeLevelId,
    string?        GradeLevelName,
    string?        ChapterTag,
    string?        Tags,
    string?        ExplanationText,
    string?        CorrectAnswer,     // FillBlank / Essay model answer
    string?        SourceUrl,
    bool           IsApproved,
    string         CreatedByTeacherName,
    DateTime       CreatedAt,
    IReadOnlyList<QuestionOptionDto> Options
);

public record QuestionOptionDto(
    int    Id,
    char   OptionLabel,   // A, B, C, D
    string OptionText,
    bool   IsCorrect
);

public record QuestionListItemDto(
    int            Id,
    string         SubjectName,
    QuestionType   QuestionType,
    DifficultyLevel Difficulty,
    string         QuestionText,   // truncated
    string?        ChapterTag,
    bool           IsApproved,
    DateTime       CreatedAt
);

// ─── Create / Update ──────────────────────────────────────────────────────────

public record CreateQuestionRequest(
    int            SubjectId,
    string         QuestionText,
    QuestionType   QuestionType,
    DifficultyLevel Difficulty,
    int?           GradeLevelId,
    string?        ChapterTag,
    string?        Tags,
    string?        ExplanationText,
    string?        CorrectAnswer,
    string?        SourceUrl,
    List<CreateOptionRequest> Options
);

public record CreateOptionRequest(
    char   OptionLabel,
    string OptionText,
    bool   IsCorrect
);

public record UpdateQuestionRequest(
    string         QuestionText,
    QuestionType   QuestionType,
    DifficultyLevel Difficulty,
    int?           GradeLevelId,
    string?        ChapterTag,
    string?        Tags,
    string?        ExplanationText,
    string?        CorrectAnswer,
    string?        SourceUrl,
    List<CreateOptionRequest> Options
);

// ─── Filter ───────────────────────────────────────────────────────────────────

public record QuestionFilterRequest(
    int?           SubjectId    = null,
    QuestionType?  QuestionType = null,
    DifficultyLevel? Difficulty = null,
    string?        ChapterTag   = null,
    string?        Keyword      = null,
    bool?          IsApproved   = null,
    int            Page         = 1,
    int            PageSize     = 20
);

// ─── Import ───────────────────────────────────────────────────────────────────

public record ImportFromUrlRequest(
    string SourceUrl,
    int    TargetSubjectId,
    DifficultyLevel DefaultDifficulty = DifficultyLevel.Medium
);

public record ImportJobDto(
    int    Id,
    string SourceUrl,
    string SubjectName,
    ImportJobStatus Status,
    int    ImportedCount,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);

public record ImportPreviewDto(
    string SourceUrl,
    int    ParsedCount,
    IReadOnlyList<QuestionDto> Preview   // first 5 for review
);

// ─── Statistics ───────────────────────────────────────────────────────────────

public record QuestionBankStatsDto(
    int TotalQuestions,
    int ApprovedQuestions,
    int PendingApproval,
    Dictionary<string, int> BySubject,
    Dictionary<string, int> ByType,
    Dictionary<string, int> ByDifficulty
);

// ─── Subject dropdown option ───────────────────────────────────────────────────

public record SubjectOptionDto(int Id, string Name);
