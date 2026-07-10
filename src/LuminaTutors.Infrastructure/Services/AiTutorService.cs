using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LuminaTutors.Application.DTOs.AI;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.AI;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Infrastructure.Services;

public class AiTutorService : IAiTutorService
{
    private readonly IUnitOfWork             _uow;
    private readonly ILogger<AiTutorService> _logger;
    private readonly HttpClient              _http;
    private readonly string                  _ollamaUrl;
    private readonly string                  _model;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private const string SystemPrompt = """
        Bạn là Gia Sư AI của hệ thống Lumina Tutors — một trợ lý học tập thông minh và thân thiện dành cho học sinh Việt Nam.

        ## Vai trò
        - Giải thích bài học, khái niệm, bài tập ở TẤT CẢ các môn học: Toán, Lý, Hóa, Sinh, Văn, Sử, Địa, Tiếng Anh, Tin học, GDCD và các môn khác.
        - Hướng dẫn từng bước một, sử dụng ví dụ thực tế và dễ hiểu.
        - Động viên và khích lệ học sinh khi gặp khó khăn.
        - Đặt câu hỏi gợi ý để giúp học sinh tự tìm ra câu trả lời.

        ## Quy tắc
        - Chỉ trả lời các câu hỏi liên quan đến học tập và giáo dục.
        - Không cung cấp đáp án hoàn chỉnh của bài kiểm tra — hãy hướng dẫn cách làm.
        - Không thảo luận các chủ đề không phù hợp, bạo lực, chính trị, hoặc nội dung người lớn.
        - Nếu câu hỏi không liên quan đến học tập, lịch sự từ chối và hướng dẫn học sinh quay lại chủ đề học tập.
        - Trả lời bằng tiếng Việt trừ khi học sinh hỏi bằng ngôn ngữ khác.
        - Giữ phong thái thân thiện, kiên nhẫn, và chuyên nghiệp.

        ## Định dạng
        - Sử dụng dấu đầu dòng và số thứ tự khi liệt kê.
        - Viết công thức toán học rõ ràng (ví dụ: x² + y² = r²).
        - Chia nhỏ giải thích phức tạp thành từng bước.
        """;

    public AiTutorService(
        IUnitOfWork uow,
        ILogger<AiTutorService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _uow       = uow;
        _logger    = logger;
        _http      = httpClientFactory.CreateClient("Ollama");
        _ollamaUrl = config["Ollama:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:11434";
        _model     = config["Ollama:Model"] ?? "qwen2.5:7b";
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
            return Result<AiTutorSessionDto>.Failure("Phiên học không tồn tại.", "NOT_FOUND");

        var user = await _uow.Users.FindOneAsync(
            u => u.Id == requestingUserId,
            q => q.Include(u => u.Role), ct);

        if (user?.Role?.RoleCode != "ADMIN" && session.StudentId != requestingUserId)
            return Result<AiTutorSessionDto>.Failure("Bạn không có quyền xem phiên này.", "FORBIDDEN");

        return Result<AiTutorSessionDto>.Success(
            ToDto(session, session.Student.FullName, session.Messages.Count));
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
        var session = await _uow.AiTutorSessions.FindOneAsync(
            s => s.Id == sessionId && s.SchoolId == schoolId && s.StudentId == studentId, ct);

        if (session is null)
            return Result<AiTutorMessageDto>.Failure("Phiên học không tồn tại.", "NOT_FOUND");

        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            return Result<AiTutorMessageDto>.Failure("Nội dung không hợp lệ (tối đa 2000 ký tự).", "INVALID");

        // Lưu tin nhắn học sinh
        var userMsg = new AiTutorMessage
        {
            SessionId = sessionId,
            Role      = AiMessageRole.User,
            Content   = content.Trim()
        };
        await _uow.AiTutorMessages.AddAsync(userMsg, ct);
        await _uow.SaveChangesAsync(ct);

        // Lấy lịch sử (20 tin nhắn gần nhất)
        var history = await _uow.AiTutorMessages.FindAsync(
            m => m.SessionId == sessionId, ct: ct);

        var chatMessages = new List<OllamaMessage>
        {
            new("system", SystemPrompt)
        };
        chatMessages.AddRange(
            history.OrderBy(m => m.CreatedAt).TakeLast(8)   // giảm lịch sử gửi mỗi lượt → nhẹ prompt, nhanh hơn
                .Select(m => new OllamaMessage(
                    m.Role == AiMessageRole.User ? "user" : "assistant",
                    m.Content)));

        // Gọi Ollama API — dùng token riêng với timeout 10 phút
        // (lần đầu load model vào RAM có thể mất 2-3 phút)
        string aiReply;
        try
        {
            using var ollamaCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            var requestBody = new OllamaChatRequest(_model, chatMessages, false,
                KeepAlive: "30m",                               // giữ model warm 30 phút → tránh cold-load ở lượt sau
                Options:   new OllamaOptions(NumPredict: 400));  // giới hạn ~400 token để không phải chờ quá lâu
            var response = await _http.PostAsJsonAsync(
                $"{_ollamaUrl}/api/chat", requestBody, JsonOpts, ollamaCts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ollamaCts.Token);
                _logger.LogError("Ollama trả về lỗi {Status}: {Body}", response.StatusCode, body);
                throw new Exception($"Ollama HTTP {(int)response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOpts, ollamaCts.Token);
            aiReply = result?.Message?.Content?.Trim()
                      ?? "Xin lỗi, tôi không thể trả lời lúc này.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi Ollama API cho session {SessionId}", sessionId);
            _uow.AiTutorMessages.Remove(userMsg);
            await _uow.SaveChangesAsync(ct);
            return Result<AiTutorMessageDto>.Failure(
                "Gia Sư AI tạm thời không khả dụng. Vui lòng kiểm tra Ollama đang chạy.", "AI_ERROR");
        }

        // Lưu phản hồi AI
        var aiMsg = new AiTutorMessage
        {
            SessionId = sessionId,
            Role      = AiMessageRole.Assistant,
            Content   = aiReply
        };
        await _uow.AiTutorMessages.AddAsync(aiMsg, ct);

        // Tự động đặt tiêu đề từ câu hỏi đầu tiên
        if (session.Title == "Phiên học mới" && history.Count <= 1)
            session.Title = content.Length > 60 ? content[..57] + "..." : content;

        await _uow.SaveChangesAsync(ct);
        return Result<AiTutorMessageDto>.Success(ToMessageDto(aiMsg));
    }

    public async Task<Result> DeleteSessionAsync(
        int schoolId, int sessionId, int studentId, CancellationToken ct = default)
    {
        var session = await _uow.AiTutorSessions.FindOneAsync(
            s => s.Id == sessionId && s.SchoolId == schoolId && s.StudentId == studentId, ct);

        if (session is null)
            return Result.Failure("Phiên học không tồn tại.", "NOT_FOUND");

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
        if (session is null) return Result.Failure("Phiên học không tồn tại.", "NOT_FOUND");
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
        if (msg is null) return Result.Failure("Tin nhắn không tồn tại.", "NOT_FOUND");
        msg.IsFlagged = isFlagged;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> MarkSessionReviewedAsync(
        int sessionId, CancellationToken ct = default)
    {
        var session = await _uow.AiTutorSessions.FindOneAsync(s => s.Id == sessionId, ct);
        if (session is null) return Result.Failure("Phiên học không tồn tại.", "NOT_FOUND");
        session.IsReviewed = true;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static AiTutorSessionDto ToDto(AiTutorSession s, string studentName, int msgCount) =>
        new(s.Id, s.StudentId, studentName, s.Title, msgCount,
            s.IsReviewed, s.IsFlagged, s.AdminNote, s.CreatedAt);

    private static AiTutorMessageDto ToMessageDto(AiTutorMessage m) =>
        new(m.Id, m.Role == AiMessageRole.User ? "user" : "assistant",
            m.Content, m.IsFlagged, m.CreatedAt);
}

// ── Ollama request / response models ──────────────────────────────────────────

internal record OllamaMessage(string Role, string Content);

internal record OllamaChatRequest(
    string Model,
    List<OllamaMessage> Messages,
    bool Stream,
    string? KeepAlive = null,          // giữ model trong RAM (→ "keep_alive")
    OllamaOptions? Options = null);    // tham số sinh (→ "options")

internal record OllamaOptions(int NumPredict);   // giới hạn độ dài câu trả lời (→ "num_predict")

internal record OllamaChatResponse(
    string Model,
    OllamaMessage? Message,
    bool Done);
