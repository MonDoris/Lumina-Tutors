using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Discipline;

public record DisciplineRecordDto(
    int      RecordId,
    int      StudentId,
    string   StudentCode,
    string   StudentName,
    string   ClassName,
    string   ReportedByName,
    DateOnly RecordDate,
    string   ViolationType,
    string   Severity,
    string?  Description,
    string?  ActionTaken,
    string   Status,
    DateTime CreatedAt
);

public record CreateDisciplineRecordRequest(
    [Required] int    StudentId,
    [Required] DateOnly RecordDate,
    [Required, MaxLength(100)] string ViolationType,
    ViolationSeverity Severity    = ViolationSeverity.Minor,
    string?           Description = null,
    string?           ActionTaken = null
);

public record GateCheckLogDto(
    int      LogId,
    string   StudentName,
    string   StudentCode,
    string   ClassName,
    string   CheckType,
    DateTime CheckedAt,
    bool     IsLate,
    string?  Note
);

public record DailyDisciplineReportDto(
    DateOnly  ReportDate,
    int       TotalViolations,
    int       MinorCount,
    int       ModerateCount,
    int       SevereCount,
    int       GateChecksIn,
    int       GateChecksOut,
    int       LateArrivalsCount,
    List<DisciplineRecordDto> Records
);
