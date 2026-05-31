using AutoMapper;
using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Discipline;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class DisciplineService : IDisciplineService
{
    private readonly IUnitOfWork              _uow;
    private readonly IMapper                  _mapper;
    private readonly ILogger<DisciplineService> _logger;

    public DisciplineService(IUnitOfWork uow, IMapper mapper, ILogger<DisciplineService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── CreateRecord ─────────────────────────────────────────────────────────

    public async Task<Result<DisciplineRecordDto>> CreateRecordAsync(
        int schoolId, int reportedByUserId, CreateDisciplineRecordRequest request, CancellationToken ct = default)
    {
        var record = new DisciplineRecord
        {
            SchoolId         = schoolId,
            StudentId        = request.StudentId,
            RecordDate       = request.RecordDate,
            ViolationType    = request.ViolationType,
            Description      = request.Description?.Trim(),
            Severity         = request.Severity,
            Status           = DisciplineStatus.Open,
            ReportedByUserId = reportedByUserId
        };

        await _uow.DisciplineRecords.AddAsync(record, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "DisciplineRecord created: Student={StudentId}, Severity={Severity}, By={UserId}",
            request.StudentId, request.Severity, reportedByUserId);

        var records = await _uow.DisciplineRecords.FindAsync(
            r => r.Id == record.Id,
            include: q => q
                .Include(r => r.Student)
                .Include(r => r.ReportedBy),
            ct: ct);

        return Result<DisciplineRecordDto>.Success(_mapper.Map<DisciplineRecordDto>(records.First()));
    }

    // ─── GetRecords ───────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<DisciplineRecordDto>>> GetRecordsAsync(
        int schoolId, int? studentId, string? status, DateOnly? from, DateOnly? to,
        int page, int pageSize, CancellationToken ct = default)
    {
        DisciplineStatus? statusEnum = Enum.TryParse<DisciplineStatus>(status, out var s) ? s : null;

        var paged = await _uow.DisciplineRecords.GetPagedAsync(
            pageNumber: page,
            pageSize:   pageSize,
            filter: r =>
                r.SchoolId == schoolId &&
                (!studentId.HasValue  || r.StudentId == studentId.Value) &&
                (!statusEnum.HasValue || r.Status == statusEnum.Value) &&
                (!from.HasValue       || r.RecordDate >= from.Value) &&
                (!to.HasValue         || r.RecordDate <= to.Value),
            orderBy: q => q.OrderByDescending(r => r.RecordDate),
            include: q => q
                .Include(r => r.Student)
                .Include(r => r.ReportedBy),
            ct: ct);

        var dtos   = _mapper.Map<List<DisciplineRecordDto>>(paged.Items);
        var result = PagedResult<DisciplineRecordDto>.Create(dtos, paged.TotalCount, page, pageSize);
        return Result<PagedResult<DisciplineRecordDto>>.Success(result);
    }

    // ─── ResolveRecord ────────────────────────────────────────────────────────

    public async Task<Result> ResolveRecordAsync(
        int recordId, string actionTaken, int resolvedByUserId, CancellationToken ct = default)
    {
        var record = await _uow.DisciplineRecords.GetByIdAsync(recordId, ct);
        if (record is null)
            return Result.Failure("NOT_FOUND", "Hồ sơ kỷ luật không tồn tại.");

        if (record.Status == DisciplineStatus.Resolved)
            return Result.Failure("ALREADY_RESOLVED", "Hồ sơ đã được xử lý.");

        record.Status      = DisciplineStatus.Resolved;
        record.ActionTaken = actionTaken.Trim();
        record.ResolvedAt  = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("DisciplineRecord {Id} resolved by user {UserId}", recordId, resolvedByUserId);
        return Result.Success();
    }

    // ─── EscalateRecord ───────────────────────────────────────────────────────

    public async Task<Result> EscalateRecordAsync(
        int recordId, int escalateToUserId, CancellationToken ct = default)
    {
        var record = await _uow.DisciplineRecords.GetByIdAsync(recordId, ct);
        if (record is null)
            return Result.Failure("NOT_FOUND", "Hồ sơ kỷ luật không tồn tại.");

        record.Status           = DisciplineStatus.Escalated;
        record.EscalatedToUserId = escalateToUserId;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "DisciplineRecord {Id} escalated to user {UserId}", recordId, escalateToUserId);
        return Result.Success();
    }

    // ─── RecordGateCheck ──────────────────────────────────────────────────────

    public async Task<Result> RecordGateCheckAsync(
        int schoolId, int studentId, string checkType, int? checkedByUserId,
        bool isLate, string? note, CancellationToken ct = default)
    {
        if (!Enum.TryParse<GateCheckType>(checkType, out var gateCheckType))
            return Result.Failure("INVALID_TYPE", "Loại kiểm tra cổng không hợp lệ.");

        var gateCheck = new GateCheckLog
        {
            SchoolId        = schoolId,
            StudentId       = studentId,
            CheckType       = gateCheckType,
            CheckedAt       = DateTime.UtcNow,
            CheckedByUserId = checkedByUserId,
            IsLate          = isLate,
            Note            = note?.Trim()
        };

        await _uow.GateCheckLogs.AddAsync(gateCheck, ct);

        if (isLate)
        {
            var lateRecord = new DisciplineRecord
            {
                SchoolId         = schoolId,
                StudentId        = studentId,
                RecordDate       = DateOnly.FromDateTime(DateTime.Today),
                ViolationType    = "Đi muộn",
                Description      = $"Đi muộn lúc {DateTime.Now:HH:mm} - {gateCheckType}",
                Severity         = ViolationSeverity.Minor,
                Status           = DisciplineStatus.Open,
                ReportedByUserId = checkedByUserId ?? 0
            };
            await _uow.DisciplineRecords.AddAsync(lateRecord, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── GetDailyReport ───────────────────────────────────────────────────────

    public async Task<Result<DailyDisciplineReportDto>> GetDailyReportAsync(
        int schoolId, DateOnly date, CancellationToken ct = default)
    {
        var records = await _uow.DisciplineRecords.FindAsync(
            r => r.SchoolId == schoolId && r.RecordDate == date,
            include: q => q.Include(r => r.Student),
            ct: ct);

        var gateChecks = await _uow.GateCheckLogs.FindAsync(
            g => g.SchoolId == schoolId &&
                 DateOnly.FromDateTime(g.CheckedAt.Date) == date,
            ct: ct);

        var dto = new DailyDisciplineReportDto(
            ReportDate:      date,
            TotalViolations: records.Count,
            MinorCount:      records.Count(r => r.Severity == ViolationSeverity.Minor),
            ModerateCount:   records.Count(r => r.Severity == ViolationSeverity.Moderate),
            SevereCount:     records.Count(r => r.Severity == ViolationSeverity.Severe),
            GateChecksIn:    gateChecks.Count(g => g.CheckType == GateCheckType.In),
            GateChecksOut:   gateChecks.Count(g => g.CheckType == GateCheckType.Out),
            LateArrivalsCount: gateChecks.Count(g => g.IsLate),
            Records:         _mapper.Map<List<DisciplineRecordDto>>(records));

        return Result<DailyDisciplineReportDto>.Success(dto);
    }
}
