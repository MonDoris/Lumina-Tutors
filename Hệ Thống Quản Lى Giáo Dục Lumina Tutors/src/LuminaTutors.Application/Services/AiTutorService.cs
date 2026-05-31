using Anthropic;
using Anthropic.Models.Messages;
using LuminaTutors.Application.DTOs.AI;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.AI;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public class AiTutorService : IAiTutorService
{
    private readonly IUnitOfWork       _uow;
    private readonly ILogger<AiTutorService> _logger;
    private readonly AnthropicClient   _claude;

    // System prompt cho Gia Sư AI
    private const string SystemPrompt = """
        Bạn là Gia Sư AI của hệ thống Lumina Tutors — một trợ lý học tập thông minh và thân thiện dành cho học sinh Việt Nam.

        ## Vai trò
        - Giải thích bài học, khái niệm, bài tập ở TẤT CẢ các môn học: Toán, Lý, Hóa, Sinh, Văn, Sử, Địa, Tiếng Anh, Tin học, GDCD và các môn khác.
        - Hướng dẫn từng bước một, sử dụng ví dụ thực tế và dễ hiểu.
        - Động viên và khích lệ học sinh khi gặp khó khăn.
        - Đặt câu hỏi gợi ý để giúp học sinh tự tìm ra câu trả lời.

        ## Quy tắc
        - Chỉ trả lời các câu hỏi liên quan đến học tập và giáo dục.
        - Không cung cấp câu trả lời đầy đủ của bài kiểm tra hoặc bài thi — hãy hướng dẫn cách làm.
        - Không thảo luận các chủ đề không phù hợp, bạo lực, chính trị, hoặc nội dung người lớn.
        - Nếu câu hỏi không liên quan đến học tập, lịch sự từ chối và hướng dẫn học sinh quay lại chủ đề học tập.
        - Trả lời bằng tiếng Việt trừ khi học sinh hỏi bằng ngôn ngữ khác.
        - Giữ phong thái thân thiện, kiên nhẫn, và chuyên nghiệp.

        ## Định dạng
        - Sử dụng các dấu đầu dòng và số thứ tự khi cần liệt kê.
        - Sử dụng công thức toán học rõ ràng (ví dụ: x² + y² = r²).
        - Chia nhỏ các giải thích phức tạp thành từng bước.
        """;

    public AiTutorService(
        IUnitOfWork uow,
        ILogger<AiTutorService> logger,
        IConfiguration config)
    {
        _uow    = uow;
        _logger = logger;

        var apiKey = config["Anthropic:ApiKey"] ?? string.Empty;
        _claude = new AnthropicClient { ApiKey = apiKey };
    }

    // ── Student ────────────────────────────────────────────────────────────────

    public async Task<Result<AiTutorSessionDto>> CreateSessionAsync(
        int schoolId, int studentId, string title, CancellationToken ct = default)
    {
        var session = new AiTutorSession
        {
            SchoolId  = schoolId,
            StudentId = studentId,
            Title     = string.IsNullOrWhiteSpace(title) ? "Phiên học mới" : title.Trim()
        };
        await _uow.AiTutorSessions.AddAsync(session, ct);
        await _uow.SaveChangesAsync(ct);

        var user = await _uow.Users.GetByIdAsync(studentId, ct);
        return Result<AiTutorSessionDto>.Success(ToDto(session, user?.FullName ?? "", 0));
    }

    public async Task<Result<IReadOnlyList<AiTutorSessionDto>>> GetStudentSessionsAsync(
        int schoolId, int studentId, CancellationToken ct = default)
    {
        var sessions = await _uow.AiTutorSessions.FindAsync(
            s => s.SchoolId == schoolId && s.StudentId == studentId,
            q => q.Include(s => s.Student).Include(s => s.Messages),
            ct);

        var dtos = sessions
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => ToDto(s, s.Student.FullName, s.Messages.Count))
            .ToList() as IReadOnlyList<AiTutorSessionDto>;

        return Result<IReadOnlyList<AiTutorSessionDto>>.Success(dtos);
    }

    public async Task<Result<AiTutorSessionDto>> GetSessionAsync(
        int schoolId, int sessionId, int requestingUserId, CancellationToken ct = default)
    {
        var session = await _uow.AiTutorSessions.FindOneAsync(
            s => s.Id == sessionId && s.SchoolId == schoolId,
            q => q.Include(s => s.Student).Include(s => s.Messages),
            ct);

        if (session is null)
            return Result<AiTutorSessionDto>.Failure("NOT_FOUND", "Phiên học không tồn tại.");

        // Học sinh chỉ xem phiên của mình; Admin xem tất cả
        var user = await _uow.Users.FindOneAsync(u => u.Id == requestingUserId, ct);
        if (user?.Role?.RoleCode != "ADMIN" && session.StudentId != requestingUserId)
            return Result<AiTutorSessionDto>.Failure("FORBIDDEN", "Bạn không có quyền xem phiên này.");

        return Result<AiTutorSessionDto>.Success(ToDto(session, session.Student.FullName, session.Messages.Count));
    }

    public async Task<Result<IReadOnlyList<AiTutorMessageDto>>> GetMessagesAsync(
        int sessionId, CancellationToken ct = default)
    {
        var messages = await _uow.AiTutorMessages.FindAsync(
            m => m.SessionId == sessionId, ct: ct);

        var dtos = messages
            .OrderBy(m => m.CreatedAt)
            .Select(ToMessageDto)
            .ToList() as IReadOnlyList<AiTutorMessageDto>;

        return Result<IReadOnlyList<AiTutorMessageDto>>.Success(dtos);
    }

    public async Task<Result<AiTutorMessageDto>> SendMessageAsync(
        int schoolId, int sessionId, int studentId, string content, CancellationToken ct = default)
    {
        // Validate session belongs to student
        var session = await _uow.AiTutorSessions.FindOneAsync(
            s => s.Id == sessionId && s.SchoolId == schoolId && s.StudentId == studentId, ct);

        if (session is null)
            return Result<AiTutorMessageDto>.Failure("NOT_FOUND", "Phiên học không tồn tại.");

        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            return Result<AiTutorMessageDto>.Failure("INVALID", "Nội dung tin nhắn không hợp lệ (tối đa 2000 ký tự).");

        // Lưu tin nhắn của học sinh
        var userMsg = new AiTutorMessage
        {
            SessionId = sessionId,
            Role      = AiMessageRole.User,
            Content   = content.Trim()
        };
        await _uow.AiTutorMessages.AddAsync(userMsg, ct);
        await _uow.SaveChangesAsync(ct);

        // Load lịch sử hội thoại (tối đa 20 tin nhắn gần nhất)
        var history = await _uow.AiTutorMessages.FindAsync(
            m => m.SessionId == sessionId, ct: ct);

        var conversationMessages = history
            .OrderBy(m => m.CreatedAt)
            .TakeLast(20)
            .Select(m => new MessageParam
            {
                Role = m.Role == AiMessageRole.User ? Role.User : Role.Assistant,
                Content = m.Content
            })
            .ToList();

        // Gọi Claude API
        string aiReply;
        try
        {
            var response = await _claude.Messages.CreateAsync(new MessageCreateParams
            {
                Model     = "claude-haiku-4-5",
                MaxTokens = 1024,
                System    = SystemPrompt,
                Messages  = conversationMessages
            });

            aiReply = response.Content
                .OfType<TextBlock>()
                .FirstOrDefault()?.Text ?? "Xin lỗi, tôi không thể trả lời lúc này.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Claude API cho session {SessionId}", sessionId);
            // Xóa tin nhắn vừa lưu nếu AI lỗi
            _uow.AiTutorMessages.Remove(userMsg);
            await _uow.SaveChangesAsync(ct);
            return Result<AiTutorMessageDto>.Failure("AI_ERROR", "Gia Sư AI tạm thời không khả dụng. Vui lòng thử lại sau.");
        }

        // Lưu phản hồi AI
        var aiMsg = new AiTutorMessage
        {
            SessionId = sessionId,
            Role      = AiMessageRole.Assistant,
            Content   = aiReply
        };
        await _uow.AiTutorMessages.AddAsync(aiMsg, ct);

        // Cập nhật tiêu đề phiên nếu chưa có tiêu đề tốt (dùng câu hỏi đầu tiên)
        if (session.Title == "Phiên học mới" && history.Count <= 1)
        {
            session.Title = content.Length > 60
                ? content[..57] + "..."
                : content;
        }

        await _uow.SaveChangesAsync(ct);

        return Result<AiTutorMessageDto>.Success(ToMessageDto(aiMsg));
    }

    public async Task<Result> DeleteSessionAsync(
        int schoolId, int sessionId, int studentId, CancellationToken ct = default)
    {
        var session = await _uow.AiTutorSessions.FindOneAsync(
            s => s.Id == sessionId && s.SchoolId == schoolId && s.StudentId == studentId, ct);

        if (session is null)
            return Result.Failure("NOT_FOUND", "Phiên học không tồn tại.");

        var messages = await _uow.AiTutorMessages.FindAsync(m => m.SessionId == sessionId, ct: ct);
        foreach (var m in messages) _uow.AiTutorMessages.Remove(m);
        _uow.AiTutorSessions.Remove(session);
        await _uow.SaveChangesAsync(ct);

        return Result.Success();
    }

    // ── Admin ──────────────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<AiTutorSessionDto>>> GetAllSessionsAsync(
        int schoolId, bool? flaggedOnly = null, CancellationToken ct = default)
    {
        var sessions = await _uow.AiTutorSessions.FindAsync(
            s => s.SchoolId == schoolId && (flaggedOnly == null || s.IsFlagged == flaggedOnly),
            q => q.Include(s => s.Student).Include(s => s.Messages),
            ct);

        var dtos = sessions
            .OrderByDescending(s => s.IsFlagged)
            .ThenByDescending(s => s.CreatedAt)
            .Select(s => ToDto(s, s.Student.FullName, s.Messages.Count))
            .ToList() as IReadOnlyList<AiTutorSessionDto>;

        return Result<IReadOnlyList<AiTutorSessionDto>>.Success(dtos);
    }

    public async Task<Result> UpdateAdminNoteAsync(
        int sessionId, string? note, bool isFlagged, CancellationToken ct = default)
    {
        var session = await _uow.AiTutorSessions.FindOneAsync(s => s.Id == sessionId, ct);
        if (session is null)
            return Result.Failure("NOT_FOUND", "Phiên học không tồn tại.");

        session.AdminNote  = note?.Trim();
        session.IsFlagged  = isFlagged;
        session.IsReviewed = true;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> FlagMessageAsync(
        int messageId, bool isFlagged, CancellationToken ct = default)
    {
        var msg = await _uow.AiTutorMessages.FindOneAsync(m => m.Id == messageId, ct);
        if (msg is null)
            return Result.Failure("NOT_FOUND", "Tin nhắn không tồn tại.");

        msg.IsFlagged = isFlagged;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkSessionReviewedAsync(
        int sessionId, CancellationToken ct = default)
    {
        var session = await _uow.AiTutorSessions.FindOneAsync(s => s.Id == sessionId, ct);
        if (session is null)
            return Result.Failure("NOT_FOUND", "Phiên học không tồn tại.");

        session.IsReviewed = true;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static AiTutorSessionDto ToDto(AiTutorSession s, string studentName, int msgCount) =>
        new(s.Id, s.StudentId, studentName, s.Title, msgCount,
            s.IsReviewed, s.IsFlagged, s.AdminNote, s.CreatedAt);

    private static AiTutorMessageDto ToMessageDto(AiTutorMessage m) =>
        new(m.Id,
            m.Role == AiMessageRole.User ? "user" : "assistant",
            m.Content, m.IsFlagged, m.CreatedAt);
}
