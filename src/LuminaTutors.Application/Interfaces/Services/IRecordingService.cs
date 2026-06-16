using LuminaTutors.Application.DTOs.Recording;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>Lưu &amp; liệt kê bản ghi (video) các buổi học online / phòng 3D.</summary>
public interface IRecordingService
{
    Task<Result<int>> SaveAsync(int schoolId, SaveRecordingInput input, CancellationToken ct = default);

    Task<Result<IReadOnlyList<RecordingListItemDto>>> GetAllAsync(int schoolId, CancellationToken ct = default);
}
