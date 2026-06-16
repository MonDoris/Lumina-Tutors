using LuminaTutors.Application.DTOs.Recording;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Learning;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;

namespace LuminaTutors.Application.Services;

public sealed class RecordingService : IRecordingService
{
    private readonly IUnitOfWork _uow;

    public RecordingService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<int>> SaveAsync(int schoolId, SaveRecordingInput i, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(i.FileUrl))
            return Result<int>.Failure("Thiếu đường dẫn file bản ghi.");

        var rec = new SessionRecording
        {
            SchoolId         = schoolId,
            Source           = i.Source,
            OnlineSessionId  = i.OnlineSessionId,
            RoomLabel        = string.IsNullOrWhiteSpace(i.RoomLabel) ? "(Không tên)" : i.RoomLabel.Trim(),
            TeacherId        = i.TeacherId,
            TeacherName      = string.IsNullOrWhiteSpace(i.TeacherName) ? "—" : i.TeacherName.Trim(),
            StartedAt        = i.StartedAt,
            EndedAt          = i.EndedAt < i.StartedAt ? i.StartedAt : i.EndedAt,
            ParticipantCount = Math.Max(0, i.ParticipantCount),
            FileUrl          = i.FileUrl,
            FileSizeBytes    = Math.Max(0, i.FileSizeBytes),
        };

        await _uow.SessionRecordings.AddAsync(rec, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<int>.Success(rec.Id);
    }

    public async Task<Result<IReadOnlyList<RecordingListItemDto>>> GetAllAsync(int schoolId, CancellationToken ct = default)
    {
        var list = await _uow.SessionRecordings.FindAsync(r => r.SchoolId == schoolId, ct);

        var dtos = list
            .OrderByDescending(r => r.StartedAt)
            .Select(r => new RecordingListItemDto
            {
                Id               = r.Id,
                SourceLabel      = r.Source == RecordingSource.Online ? "Phòng online" : "Phòng 3D",
                RoomLabel        = r.RoomLabel,
                TeacherName      = r.TeacherName,
                StartedAt        = r.StartedAt,
                EndedAt          = r.EndedAt,
                DurationSeconds  = (int)Math.Max(0, (r.EndedAt - r.StartedAt).TotalSeconds),
                ParticipantCount = r.ParticipantCount,
                FileUrl          = r.FileUrl,
                FileSizeBytes    = r.FileSizeBytes,
            })
            .ToList();

        return Result<IReadOnlyList<RecordingListItemDto>>.Success(dtos);
    }
}
