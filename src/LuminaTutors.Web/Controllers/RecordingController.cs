using System.Security.Claims;
using LuminaTutors.Application.DTOs.Recording;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuminaTutors.Web.Controllers;

/// <summary>
/// Bản ghi (video) các buổi học — phòng online &amp; phòng 3D.
/// • POST /Recording/Save : client (phòng học) tải bản ghi .webm + metadata lên.
/// • GET  /Recording      : trang Admin liệt kê toàn bộ bản ghi.
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class RecordingController : Controller
{
    private readonly IRecordingService _service;
    private readonly ILogger<RecordingController> _logger;

    public RecordingController(IRecordingService service, ILogger<RecordingController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── GET /Recording — bảng bản ghi (chỉ Admin) ────────────────────────────
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Index()
    {
        var result = await _service.GetAllAsync(SchoolId);
        ViewBag.Recordings = result.IsSuccess
            ? result.Data!
            : new List<RecordingListItemDto>();
        return View();
    }

    // ── POST /Recording/Save — phòng học tải bản ghi lên ─────────────────────
    [HttpPost]
    [RequestSizeLimit(629_145_600)]                                   // ~600 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 629_145_600)]
    public async Task<IActionResult> Save(
        IFormFile file, string source, int? onlineSessionId,
        string? roomLabel, long startedAtMs, long endedAtMs, int participantCount)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Không có dữ liệu bản ghi." });

        // Lưu file xuống wwwroot/uploads/recordings
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "recordings");
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid():N}.webm";
        var filePath = Path.Combine(dir, fileName);
        await using (var fs = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(fs);

        var input = new SaveRecordingInput
        {
            Source           = string.Equals(source, "Lab3D", StringComparison.OrdinalIgnoreCase)
                                   ? RecordingSource.Lab3D : RecordingSource.Online,
            OnlineSessionId  = onlineSessionId,
            RoomLabel        = roomLabel ?? "",
            TeacherId        = UserId,
            TeacherName      = User.FindFirstValue(ClaimTypes.Name) ?? "—",
            StartedAt        = FromMs(startedAtMs),
            EndedAt          = FromMs(endedAtMs),
            ParticipantCount = participantCount,
            FileUrl          = $"/uploads/recordings/{fileName}",
            FileSizeBytes    = file.Length,
        };

        var result = await _service.SaveAsync(SchoolId, input);
        if (!result.IsSuccess)
        {
            _logger.LogWarning("Lưu bản ghi thất bại: {Err}", result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Đã lưu bản ghi {Id} ({Src}) cho phòng {Room}", result.Data, input.Source, input.RoomLabel);
        return Ok(new { id = result.Data });
    }

    private static DateTime FromMs(long ms) =>
        ms > 0 ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime : DateTime.UtcNow;

    private int UserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId => int.Parse(User.FindFirstValue("SchoolId") ?? "0");
}
