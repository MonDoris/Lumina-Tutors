namespace LuminaTutors.Application.DTOs.Support;

/// <summary>Một tin nhắn trong luồng hỗ trợ giữa Nhà trường và Quản trị hệ thống.</summary>
public record SupportMessageDto(
    int Id, int SenderId, string SenderName, bool FromSysAdmin, string Text, DateTime SentAt);

/// <summary>Luồng hỗ trợ của một trường (ConversationId null = chưa có tin nào).</summary>
public record SupportThreadDto(
    int? ConversationId, int SchoolId, string SchoolName, IReadOnlyList<SupportMessageDto> Messages);

/// <summary>Một dòng trong hộp thư hỗ trợ của Quản trị hệ thống (mỗi trường một luồng).</summary>
public record SupportThreadListItemDto(
    int SchoolId, string SchoolName, int ConversationId, string? LastMessage, DateTime? LastAt, int Unread);
