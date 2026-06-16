using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Recording;

/// <summary>Đầu vào lưu một bản ghi buổi học (controller đã lưu file xuống đĩa trước).</summary>
public sealed class SaveRecordingInput
{
    public RecordingSource Source           { get; set; }
    public int?            OnlineSessionId  { get; set; }
    public string          RoomLabel        { get; set; } = string.Empty;
    public int             TeacherId        { get; set; }
    public string          TeacherName      { get; set; } = string.Empty;
    public DateTime        StartedAt        { get; set; }
    public DateTime        EndedAt          { get; set; }
    public int             ParticipantCount { get; set; }
    public string          FileUrl          { get; set; } = string.Empty;
    public long            FileSizeBytes    { get; set; }
}

/// <summary>Một dòng trong bảng "Bản ghi buổi học" của Admin.</summary>
public sealed class RecordingListItemDto
{
    public int      Id               { get; set; }
    public string   SourceLabel      { get; set; } = string.Empty;   // "Phòng online" / "Phòng 3D"
    public string   RoomLabel        { get; set; } = string.Empty;   // lớp / buổi học
    public string   TeacherName      { get; set; } = string.Empty;
    public DateTime StartedAt        { get; set; }
    public DateTime EndedAt          { get; set; }
    public int      DurationSeconds  { get; set; }
    public int      ParticipantCount { get; set; }
    public string   FileUrl          { get; set; } = string.Empty;
    public long     FileSizeBytes    { get; set; }
}
