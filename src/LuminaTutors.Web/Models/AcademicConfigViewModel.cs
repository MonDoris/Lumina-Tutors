using LuminaTutors.Application.DTOs.Class;

namespace LuminaTutors.Web.Models;

public sealed class AcademicConfigViewModel
{
    public List<AcademicYearConfigDto> AcademicYears { get; init; } = [];
    public List<GradeLevelConfigDto> GradeLevels { get; init; } = [];

    public CreateAcademicYearRequest NewAcademicYear { get; init; }
        = new("", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today), false);

    public CreateGradeLevelRequest NewGradeLevel { get; init; }
        = new(1, "Khối 1", "TieuHoc");
}

