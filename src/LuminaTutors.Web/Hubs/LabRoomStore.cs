using System.Collections.Concurrent;

namespace LuminaTutors.Web.Hubs;

/// <summary>
/// Trạng thái của một phòng "Bảng vẽ Toán 3D" giữ trong bộ nhớ (không đụng DB).
/// Đủ để người vào trễ dựng lại toàn bộ scene và để áp quyền vẽ cho học sinh.
/// </summary>
public sealed class LabRoomState
{
    /// <summary>UserId của giáo viên chủ phòng — xác định một lần khi người đầu tiên JoinLab.</summary>
    public int HostUserId { get; set; }

    /// <summary>Cho phép học sinh được vẽ hay không (giáo viên bật/tắt).</summary>
    public bool StudentDraw { get; set; }

    /// <summary>"free" = mỗi máy một góc nhìn; "follow" = camera học sinh đi theo giáo viên.</summary>
    public string CameraMode { get; set; } = "free";

    /// <summary>objectId → JSON spec mới nhất của đối tượng (để dựng lại scene).</summary>
    public ConcurrentDictionary<string, string> Objects { get; } = new();

    /// <summary>Đã nạp scene từ DB chưa (chỉ nạp một lần cho mỗi phòng trong bộ nhớ).</summary>
    public bool Hydrated { get; set; }

    /// <summary>Mốc thời gian lần lưu DB gần nhất (Environment.TickCount64) — để giãn nhịp lưu.</summary>
    public long LastSaveTicks { get; set; }

    /// <summary>Có thay đổi chưa lưu xuống DB hay không.</summary>
    public bool Dirty { get; set; }
}

/// <summary>
/// Kho trạng thái mọi phòng lab đang mở, theo sessionId. Đăng ký dạng Singleton.
/// </summary>
public sealed class LabRoomStore
{
    private readonly ConcurrentDictionary<int, LabRoomState> _rooms = new();

    public LabRoomState Get(int sessionId) =>
        _rooms.GetOrAdd(sessionId, _ => new LabRoomState());

    public void Remove(int sessionId) => _rooms.TryRemove(sessionId, out _);
}
