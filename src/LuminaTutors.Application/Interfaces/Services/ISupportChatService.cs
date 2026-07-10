using LuminaTutors.Application.DTOs.Support;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// Kênh nhắn tin hỗ trợ giữa Nhà trường (ADMIN) và Quản trị hệ thống (SYSADMIN).
/// Mỗi trường có một luồng; SYSADMIN thấy hộp thư gồm tất cả các trường.
/// Dựng trên bảng Conversation/Message sẵn có (ConversationName = "__SUPPORT__").
/// </summary>
public interface ISupportChatService
{
    /// <summary>Lấy luồng hỗ trợ của một trường (kèm tin nhắn). markRead = đánh dấu SYSADMIN đã đọc.</summary>
    Task<Result<SupportThreadDto>> GetSchoolThreadAsync(int schoolId, bool markReadForSysAdmin, CancellationToken ct = default);

    /// <summary>Gửi một tin nhắn vào luồng của trường (tự tạo luồng nếu chưa có).</summary>
    Task<Result> SendAsync(int schoolId, int senderUserId, string text, CancellationToken ct = default);

    /// <summary>Danh sách tất cả luồng hỗ trợ cho SYSADMIN (kèm số chưa đọc).</summary>
    Task<Result<IReadOnlyList<SupportThreadListItemDto>>> GetAllThreadsAsync(CancellationToken ct = default);

    /// <summary>Tổng số tin chưa đọc của SYSADMIN (cho badge).</summary>
    Task<int> GetUnreadForSysAdminAsync(CancellationToken ct = default);
}
