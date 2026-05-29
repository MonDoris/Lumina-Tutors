using System.Security.Claims;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LuminaTutors.Web.Hubs;

/// <summary>
/// Real-time hub for Online Classroom:
///   • Chat messaging
///   • Whiteboard stroke sync
///   • Slide page sync
///   • WebRTC signaling (offer / answer / ICE candidate)
///   • Attendance marking
///   • Hand-raise notifications
/// </summary>
[Authorize(Policy = "AnyAuthenticated")]
public sealed class OnlineClassHub : Hub
{
    private readonly IOnlineClassroomService _service;
    private readonly ILogger<OnlineClassHub> _logger;

    public OnlineClassHub(IOnlineClassroomService service, ILogger<OnlineClassHub> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int UserId    => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private int SchoolId  => int.Parse(Context.User!.FindFirstValue("SchoolId") ?? "0");
    private string UserName => Context.User!.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    private static string GroupName(int sessionId) => $"session:{sessionId}";

    // ── Connection events ─────────────────────────────────────────────────────

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("SignalR connected: userId={UserId} connId={ConnId}", UserId, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("SignalR disconnected: userId={UserId} connId={ConnId}", UserId, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ── Room join / leave ─────────────────────────────────────────────────────

    /// <summary>Join a session group and record join in DB.</summary>
    public async Task JoinRoom(int sessionId)
    {
        var group = GroupName(sessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await _service.RecordJoinAsync(sessionId, UserId);

        // Notify others
        await Clients.OthersInGroup(group).SendAsync("UserJoined", new
        {
            UserId   = UserId,
            UserName = UserName,
            JoinedAt = DateTime.UtcNow
        });

        _logger.LogInformation("User {UserId} joined session {SessionId}", UserId, sessionId);
    }

    /// <summary>Leave a session group and record departure in DB.</summary>
    public async Task LeaveRoom(int sessionId)
    {
        var group = GroupName(sessionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        await _service.RecordLeaveAsync(sessionId, UserId);

        await Clients.OthersInGroup(group).SendAsync("UserLeft", new
        {
            UserId   = UserId,
            UserName = UserName,
            LeftAt   = DateTime.UtcNow
        });

        _logger.LogInformation("User {UserId} left session {SessionId}", UserId, sessionId);
    }

    // ── Chat ──────────────────────────────────────────────────────────────────

    /// <summary>Save a chat message to DB and broadcast to all room members.</summary>
    public async Task SendMessage(int sessionId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var result = await _service.SaveChatMessageAsync(sessionId, UserId, content);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        await Clients.Group(GroupName(sessionId)).SendAsync("ReceiveMessage", result.Data);
    }

    // ── Whiteboard ────────────────────────────────────────────────────────────

    /// <summary>
    /// Relay a whiteboard stroke to all participants.
    /// strokeJson: serialized WhiteboardStrokePayload from the client.
    /// </summary>
    public async Task SyncWhiteboard(int sessionId, string strokeJson)
    {
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("WhiteboardStroke", new
        {
            SenderId = UserId,
            Stroke   = strokeJson
        });
    }

    /// <summary>Clear whiteboard for all participants (host only action enforced on client).</summary>
    public async Task ClearWhiteboard(int sessionId)
    {
        await Clients.Group(GroupName(sessionId)).SendAsync("WhiteboardCleared", new
        {
            ByUserId = UserId
        });
    }

    // ── Slides ────────────────────────────────────────────────────────────────

    /// <summary>Sync the current slide page to all participants.</summary>
    public async Task SyncSlide(int sessionId, int slideId, int pageIndex)
    {
        await Clients.OthersInGroup(GroupName(sessionId)).SendAsync("SlideChanged", new
        {
            SlideId   = slideId,
            PageIndex = pageIndex,
            ByUserId  = UserId
        });
    }

    // ── WebRTC signaling ──────────────────────────────────────────────────────

    /// <summary>
    /// Relay a WebRTC signal (offer / answer / ICE candidate) to a specific peer.
    /// type: "offer" | "answer" | "ice-candidate"
    /// data: serialized SDP or ICE candidate JSON string
    /// </summary>
    public async Task SendWebRtcSignal(int sessionId, int targetUserId, string type, string data)
    {
        // We need to map userId → connectionId.
        // Since a user may have multiple connections, we broadcast to the group
        // and let the target client filter by targetUserId on the client side.
        await Clients.Group(GroupName(sessionId)).SendAsync("WebRtcSignal", new
        {
            FromUserId = UserId,
            TargetUserId = targetUserId,
            Type       = type,
            Data       = data
        });
    }

    // ── Attendance ────────────────────────────────────────────────────────────

    /// <summary>Mark attendance for a student and broadcast confirmation.</summary>
    public async Task MarkAttendance(int sessionId, int studentUserId)
    {
        var result = await _service.MarkAttendanceAsync(sessionId, studentUserId, UserId);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        await Clients.Group(GroupName(sessionId)).SendAsync("AttendanceMarked", new
        {
            SessionId     = sessionId,
            StudentUserId = studentUserId,
            MarkedBy      = UserId,
            MarkedAt      = DateTime.UtcNow
        });
    }

    // ── Engagement ────────────────────────────────────────────────────────────

    /// <summary>Broadcast a hand-raise event from a student.</summary>
    public async Task RaiseHand(int sessionId)
    {
        await Clients.Group(GroupName(sessionId)).SendAsync("HandRaised", new
        {
            UserId   = UserId,
            UserName = UserName,
            RaisedAt = DateTime.UtcNow
        });
    }

    /// <summary>Lower hand (student or host acknowledges).</summary>
    public async Task LowerHand(int sessionId, int studentUserId)
    {
        await Clients.Group(GroupName(sessionId)).SendAsync("HandLowered", new
        {
            UserId = studentUserId
        });
    }

    // ── Session lifecycle signals ─────────────────────────────────────────────

    /// <summary>Host signals that session has started — kicks off participant timers.</summary>
    public async Task NotifySessionStarted(int sessionId)
    {
        await Clients.Group(GroupName(sessionId)).SendAsync("SessionStarted", new
        {
            SessionId = sessionId,
            StartedAt = DateTime.UtcNow
        });
    }

    /// <summary>Host signals session end — all participants redirect.</summary>
    public async Task NotifySessionEnded(int sessionId)
    {
        await Clients.Group(GroupName(sessionId)).SendAsync("SessionEnded", new
        {
            SessionId = sessionId,
            EndedAt   = DateTime.UtcNow
        });
    }
}
