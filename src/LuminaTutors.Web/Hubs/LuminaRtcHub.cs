using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LuminaTutors.Web.Hubs;

/// <summary>
/// Signaling Hub cho "Lumina Holographic Nexus" (phòng học 3D real-time).
///
///   • Điều phối SDP/ICE giữa client và SFU thuần C# (<see cref="ILuminaSfuService"/>).
///   • Phát luồng trạng thái 3D (SyncTransform) đã được client throttle 20 Hz.
///   • Chuyển chế độ Lecture &lt;-&gt; Lab và đồng bộ vị trí Avatar (Spatial Audio).
///
/// Media KHÔNG đi qua Hub. Hub chỉ là control-plane + signaling.
/// </summary>
[Authorize(Policy = "LabAccess")]
public sealed class LuminaRtcHub : Hub
{
    private readonly ILuminaSfuService _sfu;
    private readonly ILogger<LuminaRtcHub> _logger;

    // connectionId -> participant ; roomId -> set<connectionId>
    private static readonly ConcurrentDictionary<string, Participant> Participants = new();
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> Rooms = new();
    // roomId -> thí nghiệm hiện tại (để người vào sau đồng bộ đúng học cụ)
    private static readonly ConcurrentDictionary<string, string> RoomScenes = new();
    // roomId -> hóa chất đã cho vào cốc (bàn phản ứng) — người vào sau phát lại
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> RoomChems = new();
    // roomId -> tham số thí nghiệm tương tác (Lý/Sinh/Toán) — người vào sau khôi phục
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, double>> RoomSims = new();

    public LuminaRtcHub(ILuminaSfuService sfu, ILogger<LuminaRtcHub> logger)
    {
        _sfu = sfu;
        _logger = logger;
    }

    private string DisplayName => Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Học viên";
    private bool IsTeacher => Context.User?.IsInRole("TEACHER") == true || Context.User?.IsInRole("ADMIN") == true;

    // ── JOIN ──────────────────────────────────────────────────────────────────
    public async Task JoinRoom(string roomId)
    {
        var p = new Participant
        {
            ConnectionId = Context.ConnectionId,
            RoomId = roomId,
            DisplayName = DisplayName,
            Role = IsTeacher ? "teacher" : "student"
        };
        Participants[Context.ConnectionId] = p;
        Rooms.GetOrAdd(roomId, _ => new()).TryAdd(Context.ConnectionId, 0);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Danh sách publisher hiện có để client mới subscribe.
        var peers = Rooms[roomId].Keys
            .Where(id => id != Context.ConnectionId
                         && Participants.TryGetValue(id, out var x) && x.IsPublishing)
            .Select(id => ToPeer(Participants[id]))
            .ToList();

        await Clients.Caller.SendAsync("RoomJoined", new
        {
            roomId,
            selfId = Context.ConnectionId,
            role = p.Role,
            scene = RoomScenes.GetValueOrDefault(roomId),
            chem = RoomChems.TryGetValue(roomId, out var cq) ? cq.ToArray() : [],
            sims = RoomSims.TryGetValue(roomId, out var sq)
                ? sq.ToDictionary(x => x.Key, x => x.Value)
                : new Dictionary<string, double>(),
            peers
        });
        await Clients.OthersInGroup(roomId).SendAsync("PeerJoined", ToPeer(p));

        _logger.LogInformation("NEXUS join {Conn} room={Room} role={Role}", Context.ConnectionId, roomId, p.Role);
    }

    // ── PUBLISH (client offer -> SFU answer) ───────────────────────────────────
    public async Task Publish(string sdpOffer)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p)) return;

        var answer = await _sfu.CreatePublisherAsync(p.RoomId, Context.ConnectionId, sdpOffer);
        p.IsPublishing = true;

        await Clients.Caller.SendAsync("PublishAnswer", answer);
        await Clients.OthersInGroup(p.RoomId).SendAsync("NewPublisher", ToPeer(p));
    }

    // ── SUBSCRIBE (SFU tự đẩy "RtcOffer" xuống caller) ─────────────────────────
    public async Task Subscribe(string targetPeerId)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p)) return;
        await _sfu.CreateSubscriptionAsync(p.RoomId, Context.ConnectionId, targetPeerId);
    }

    // ── ANSWER (client trả lời offer của SFU) ──────────────────────────────────
    public Task Answer(string subscriptionId, string sdpAnswer)
    {
        _sfu.SetSubscriptionAnswer(subscriptionId, sdpAnswer);
        return Task.CompletedTask;
    }

    // ── TRICKLE ICE (client -> SFU) ; pcKey = "publisher:{id}" | "sub:{subId}" ──
    public Task SendIceCandidate(string pcKey, string candidateJson)
    {
        _sfu.AddIceCandidate(pcKey, candidateJson);
        return Task.CompletedTask;
    }

    // ── 3D STATE SYNC (giáo viên -> học sinh) ──────────────────────────────────
    public async Task SyncTransform(TransformPayload payload)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        payload.ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteTransform", payload);
    }

    // ── TEACHING SYNC — Lumina Research Facility ───────────────────────────────
    // Tất cả chỉ giáo viên được phát; payload đã throttle ở client.

    /// <summary>Toạ độ 3D điểm Laser Pointer (local-space của specimen) + hiển thị.</summary>
    public async Task SyncLaser(LaserPayload payload)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        payload.ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteLaser", payload);
    }

    /// <summary>Bật/tắt highlight một bộ phận theo Id.</summary>
    public async Task SyncHighlight(string partId, bool on)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteHighlight", new { partId, on });
    }

    /// <summary>Mức độ bóc tách (explode) 0..1.</summary>
    public async Task SyncExplode(float factor)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        var clamped = Math.Clamp(factor, 0f, 1f);
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteExplode", new { factor = clamped });
    }

    /// <summary>Bật/tắt toàn bộ nhãn (Interactive Labels).</summary>
    public async Task SyncLabels(bool on)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteLabels", new { on });
    }

    /// <summary>Đổi thí nghiệm (môn học) đang chiếu — lưu lại cho người vào sau.</summary>
    public async Task SetScene(string scene)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        scene = (scene ?? "").Trim();
        if (scene.Length == 0) return;
        // Chỉ xóa trạng thái cốc khi đổi sang thí nghiệm KHÁC
        // (giáo viên refresh trang phát lại scene cũ thì giữ nguyên).
        var changed = !RoomScenes.TryGetValue(p.RoomId, out var cur) || cur != scene;
        RoomScenes[p.RoomId] = scene;
        if (changed) { RoomChems.TryRemove(p.RoomId, out _); RoomSims.TryRemove(p.RoomId, out _); }
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteScene", new { scene });
    }

    // ── THÍ NGHIỆM THAM SỐ (Lý/Sinh/Toán) ─────────────────────────────────────

    /// <summary>Giáo viên kéo thanh trượt — lưu lại để người vào sau khôi phục.</summary>
    public async Task SyncSim(string key, double value)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        key = (key ?? "").Trim();
        if (key.Length is 0 or > 20 || !double.IsFinite(value)) return;
        var d = RoomSims.GetOrAdd(p.RoomId, _ => new());
        d[key] = value;
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteSim", new { key, value });
    }

    /// <summary>Giáo viên đặt lại thí nghiệm về tham số mặc định.</summary>
    public async Task SimReset()
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        RoomSims.TryRemove(p.RoomId, out _);
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteSimReset");
    }

    // ── BÀN PHẢN ỨNG HÓA HỌC (scene "reaction") ───────────────────────────────

    /// <summary>Giáo viên cho một hóa chất vào cốc — lưu lại để người vào sau phát lại.</summary>
    public async Task ChemAdd(string chemicalId)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        chemicalId = (chemicalId ?? "").Trim();
        if (chemicalId.Length is 0 or > 20) return;
        var q = RoomChems.GetOrAdd(p.RoomId, _ => new());
        if (q.Count >= 30) return; // chống spam / cốc đầy
        q.Enqueue(chemicalId);
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteChemAdd", new { chemicalId });
    }

    /// <summary>Giáo viên rửa cốc — xóa trạng thái và báo cả phòng.</summary>
    public async Task ChemReset()
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        RoomChems.TryRemove(p.RoomId, out _);
        await Clients.OthersInGroup(p.RoomId).SendAsync("RemoteChemReset");
    }

    // ── CHUYỂN CHẾ ĐỘ Lecture <-> Lab ──────────────────────────────────────────
    public async Task SwitchMode(string mode)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p) || p.Role != "teacher") return;
        await Clients.Group(p.RoomId).SendAsync("ModeChanged", mode);
    }

    // ── Avatar position (Lab Mode, neo Spatial Audio) ──────────────────────────
    public async Task UpdateAvatar(AvatarPayload payload)
    {
        if (!Participants.TryGetValue(Context.ConnectionId, out var p)) return;
        payload.PeerId = Context.ConnectionId;
        await Clients.OthersInGroup(p.RoomId).SendAsync("AvatarMoved", payload);
    }

    // ── DISCONNECT ─────────────────────────────────────────────────────────────
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Participants.TryRemove(Context.ConnectionId, out var p))
        {
            _sfu.RemovePeer(p.RoomId, Context.ConnectionId);
            if (Rooms.TryGetValue(p.RoomId, out var set))
            {
                set.TryRemove(Context.ConnectionId, out _);
                if (set.IsEmpty)
                {
                    Rooms.TryRemove(p.RoomId, out _);
                    RoomScenes.TryRemove(p.RoomId, out _);
                    RoomChems.TryRemove(p.RoomId, out _);
                    RoomSims.TryRemove(p.RoomId, out _);
                }
            }
            await Clients.Group(p.RoomId).SendAsync("PeerLeft", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private static object ToPeer(Participant p) => new
    {
        peerId = p.ConnectionId,
        displayName = p.DisplayName,
        role = p.Role
    };

    private sealed class Participant
    {
        public string ConnectionId = "";
        public string RoomId = "";
        public string DisplayName = "";
        public string Role = "student";
        public bool IsPublishing;
    }
}

/// <summary>Tải trọng 3D transform (đã throttle 20 Hz + dead-band ở client).</summary>
public sealed class TransformPayload
{
    public int Seq { get; set; }
    public string ObjectId { get; set; } = "model";
    public float[] Position { get; set; } = new float[3];     // x, y, z
    public float[] Quaternion { get; set; } = new float[4];   // x, y, z, w
    public float Scale { get; set; } = 1f;
    public long ServerTime { get; set; }
}

/// <summary>Vị trí avatar của một học viên (Lab Mode).</summary>
public sealed class AvatarPayload
{
    public string PeerId { get; set; } = "";
    public float[] Position { get; set; } = new float[3];
}

/// <summary>Toạ độ điểm Laser Pointer 3D (local-space của specimen) + cờ hiển thị.</summary>
public sealed class LaserPayload
{
    public float[] Point { get; set; } = new float[3]; // x, y, z (local)
    public bool Visible { get; set; }
    public long ServerTime { get; set; }
}
