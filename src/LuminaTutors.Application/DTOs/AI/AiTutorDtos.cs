namespace LuminaTutors.Application.DTOs.AI;

public record AiTutorSessionDto(
    int     SessionId,
    int     StudentId,
    string  StudentName,
    string  Title,
    int     MessageCount,
    bool    IsReviewed,
    bool    IsFlagged,
    string? AdminNote,
    DateTime CreatedAt
);

public record AiTutorMessageDto(
    int      MessageId,
    string   Role,        // "user" | "assistant"
    string   Content,
    bool     IsFlagged,
    DateTime CreatedAt
);

public record SendMessageRequest(string Content);

public record FlagMessageRequest(bool IsFlagged);

public record AdminNoteRequest(string? Note, bool IsFlagged);
