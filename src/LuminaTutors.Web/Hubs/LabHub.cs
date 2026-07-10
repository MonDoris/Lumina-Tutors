using System.Security.Claims;
using System.Text.Json;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LuminaTutors.Web.Hubs;

/// <summary>Một thao tác đồng bộ trên bảng vẽ Toán 3D.</summary>
/// <param name="Id">ULID/uuid của op — chống lặp.</param>
/// <param name="Kind">"upsert" | "delete" | "clear".</param>
/// <param name="ObjectId">Id đối tượng bị tác động (null với "clear").</param>
/// <param name="Json">Spec đầy đủ của đối tượng (JSON) — chỉ dùng với "upsert".</param>
public sealed record LabOp(string Id, string Kind, string? ObjectId, string? Json);

/// <summary>
/// Hub real-time cho "Bảng vẽ Toán 3D" (collaborative 3D math lab):
///   • Đồng bộ thao tác vẽ/tạo/xóa hình học theo mô hình truyền LỆNH (không truyền mesh).
///   • Người vào trễ nhận snapshot toàn bộ scene.
///   • Camera độc lập từng máy; chế độ "follow" cho phép ép camera học sinh theo giáo viên.
///   • Con trỏ laser của giáo viên.
/// Quyền: giáo viên (chủ phòng) luôn vẽ được; học sinh chỉ vẽ khi được mở khóa.
/// </summary>
[Authorize(Policy = "LabAccess")]
public sealed class LabHub : Hub
{
    private readonly IVirtualLabService _lab;
    private readonly LabRoomStore _store;
    private readonly ILogger<LabHub> _logger;

    public LabHub(IVirtualLabService lab, LabRoomStore store, ILogger<LabHub> logger)
    {
        _lab    = lab;
        _store  = store;
        _logger = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private int UserId   => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId => int.Parse(Context.User!.FindFirstValue("SchoolId") ?? "0");
    private string UserName => Context.User!.FindFirstValue(ClaimTypes.Name) ?? "Người dùng";

    private static string GroupName(int sessionId) => $"lab:{sessionId}";

    // ── Join / Leave ──────────────────────────────────────────────────────────

    /// <summary>Vào phòng: nhận snapshot scene hiện tại + cài đặt + cờ host.</summary>
    public async Task JoinLab(int sessionId)
    {
        var group = GroupName(sessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);

        var state = _store.Get(sessionId);

        // Xác định chủ phòng một lần (so khớp TeacherId của session).
        if (state.HostUserId == 0)
        {
            var sess = await _lab.GetByIdAsync(SchoolId, sessionId);
            if (sess.IsSuccess)
                state.HostUserId = sess.Data!.TeacherId;
        }

        // Nạp scene đã lưu từ DB MỘT LẦN — để mở lại nguyên hình sau khi server khởi động lại.
        if (!state.Hydrated)
        {
            state.Hydrated = true;
            try
            {
                var json = await _lab.GetSceneJsonAsync(sessionId);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (map is not null)
                        foreach (var kv in map) state.Objects[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Không nạp được scene phòng {SessionId}", sessionId); }
        }

        var isHost = state.HostUserId == UserId;

        await Clients.Caller.SendAsync("Snapshot", new
        {
            objects     = state.Objects.Values.ToArray(),   // mảng các JSON spec
            studentDraw = state.StudentDraw,
            cameraMode  = state.CameraMode,
            isHost
        });

        await Clients.OthersInGroup(group).SendAsync("PeerJoined", new { userId = UserId, userName = UserName });
        _logger.LogInformation("Lab {SessionId}: user {UserId} joined (host={IsHost})", sessionId, UserId, isHost);
    }

    public async Task LeaveLab(int sessionId)
    {
        var state = _store.Get(sessionId);
        if (state.HostUserId == UserId && state.Dirty)
            await PersistSceneAsync(sessionId, state, force: true);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(sessionId));
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("PeerLeft", new { userId = UserId });
    }

    // ── Op đồng bộ hình học ────────────────────────────────────────────────────

    /// <summary>Nhận một op, áp vào state phòng rồi phát cho những người khác.</summary>
    public async Task SendOp(int sessionId, LabOp op)
    {
        var state  = _store.Get(sessionId);
        var isHost = state.HostUserId == UserId;

        // Học sinh chỉ được vẽ khi giáo viên mở khóa.
        if (!isHost && !state.StudentDraw)
        {
            await Clients.Caller.SendAsync("OpRejected", op.Id);
            return;
        }

        switch (op.Kind)
        {
            case "upsert":
                if (!string.IsNullOrEmpty(op.ObjectId) && op.Json is not null)
                    state.Objects[op.ObjectId] = op.Json;
                break;
            case "delete":
                if (!string.IsNullOrEmpty(op.ObjectId))
                    state.Objects.TryRemove(op.ObjectId, out _);
                break;
            case "clear":
                if (isHost) state.Objects.Clear();
                else { await Clients.Caller.SendAsync("OpRejected", op.Id); return; }
                break;
        }

        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("Op", op);
        await PersistSceneAsync(sessionId, state, force: false);
    }

    // ── Camera ─────────────────────────────────────────────────────────────────

    /// <summary>Giáo viên đổi chế độ camera (free/follow) cho cả phòng.</summary>
    public async Task SetCameraMode(int sessionId, string mode)
    {
        var state = _store.Get(sessionId);
        if (state.HostUserId != UserId) return;

        state.CameraMode = mode == "follow" ? "follow" : "free";
        await Clients.Group(GroupName(sessionId)).SendAsync("CameraMode", state.CameraMode);
    }

    /// <summary>Giáo viên phát góc nhìn (chỉ khi đang ở chế độ follow).</summary>
    public async Task SyncCamera(int sessionId, string camJson)
    {
        var state = _store.Get(sessionId);
        if (state.HostUserId != UserId || state.CameraMode != "follow") return;
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("Camera", camJson);
    }

    // ── Quyền vẽ của học sinh ───────────────────────────────────────────────────

    public async Task SetStudentDraw(int sessionId, bool allowed)
    {
        var state = _store.Get(sessionId);
        if (state.HostUserId != UserId) return;

        state.StudentDraw = allowed;
        await Clients.Group(GroupName(sessionId)).SendAsync("StudentDraw", allowed);
    }

    // ── Con trỏ laser (giáo viên) ───────────────────────────────────────────────

    public async Task Pointer(int sessionId, double x, double y, double z, bool on)
    {
        var state = _store.Get(sessionId);
        if (state.HostUserId != UserId) return;
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("Pointer", new { x, y, z, on });
    }

    // ── Kết thúc phòng (giáo viên) ──────────────────────────────────────────────

    public async Task EndLab(int sessionId)
    {
        var state = _store.Get(sessionId);
        if (state.HostUserId != UserId) return;

        await PersistSceneAsync(sessionId, state, force: true);
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("LabEnded");
        _store.Remove(sessionId);
    }

    // ── Lưu scene xuống DB (giãn nhịp tối đa ~4s/lần để khỏi ghi DB mỗi nét) ─────
    private async Task PersistSceneAsync(int sessionId, LabRoomState state, bool force)
    {
        var now = Environment.TickCount64;
        if (!force && now - state.LastSaveTicks < 4000) { state.Dirty = true; return; }

        state.LastSaveTicks = now;
        state.Dirty = false;
        try { await _lab.SaveSceneJsonAsync(sessionId, JsonSerializer.Serialize(state.Objects)); }
        catch (Exception ex) { _logger.LogWarning(ex, "Không lưu được scene phòng {SessionId}", sessionId); }
    }
}
