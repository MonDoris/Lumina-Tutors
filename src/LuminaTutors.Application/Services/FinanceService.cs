using AutoMapper;
using LuminaTutors.Application.DTOs.Finance;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Finance;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class FinanceService : IFinanceService
{
    private readonly IUnitOfWork           _uow;
    private readonly IMapper               _mapper;
    private readonly ILogger<FinanceService> _logger;

    public FinanceService(IUnitOfWork uow, IMapper mapper, ILogger<FinanceService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── Fee Configs ──────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<TuitionFeeConfigDto>>> GetFeeConfigsAsync(
        int schoolId, int academicYearId, CancellationToken ct = default)
    {
        var configs = await _uow.TuitionFeeConfigs.FindAsync(
            c => c.SchoolId == schoolId && c.AcademicYearId == academicYearId,
            ct: ct);

        var dtos = _mapper.Map<List<TuitionFeeConfigDto>>(configs);
        return Result<IReadOnlyList<TuitionFeeConfigDto>>.Success(dtos);
    }

    public async Task<Result<TuitionFeeConfigDto>> CreateFeeConfigAsync(
        int schoolId, int createdByUserId, CreateFeeConfigRequest request, CancellationToken ct = default)
    {
        var duplicate = await _uow.TuitionFeeConfigs.FindAsync(
            c => c.SchoolId == schoolId &&
                 c.AcademicYearId == request.AcademicYearId &&
                 c.GradeLevelId == request.GradeLevelId &&
                 c.IsActive,
            ct: ct);

        if (duplicate.Any())
            return Result<TuitionFeeConfigDto>.Failure(
                "DUPLICATE", "Đã tồn tại cấu hình học phí cho khối lớp này trong năm học.");

        var config = new TuitionFeeConfig
        {
            SchoolId        = schoolId,
            AcademicYearId  = request.AcademicYearId,
            GradeLevelId    = request.GradeLevelId,
            FeeType         = request.FeeType,
            Amount          = request.Amount,
            DueDayOfMonth   = request.DueDayOfMonth,
            BillingCycle    = request.BillingCycle,
            Description     = request.Description?.Trim(),
            IsActive        = true,
            CreatedByUserId = createdByUserId
        };

        await _uow.TuitionFeeConfigs.AddAsync(config, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "FeeConfig created: School={SchoolId}, GradeLevel={Grade}, Amount={Amount}",
            schoolId, request.GradeLevelId, request.Amount);

        var dto = _mapper.Map<TuitionFeeConfigDto>(config);
        return Result<TuitionFeeConfigDto>.Success(dto);
    }

    public async Task<Result> DeactivateFeeConfigAsync(int configId, CancellationToken ct = default)
    {
        var config = await _uow.TuitionFeeConfigs.GetByIdAsync(configId, ct);
        if (config is null)
            return Result.Failure("NOT_FOUND", "Cấu hình học phí không tồn tại.");

        config.IsActive = false;
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ─── Invoices ─────────────────────────────────────────────────────────────

    public async Task<Result<InvoiceDto>> GetInvoiceAsync(int invoiceId, CancellationToken ct = default)
    {
        var invoices = await _uow.TuitionInvoices.FindAsync(
            i => i.Id == invoiceId,
            include: q => q
                .Include(i => i.Student)
                .Include(i => i.Payments),
            ct: ct);

        var invoice = invoices.FirstOrDefault();
        if (invoice is null)
            return Result<InvoiceDto>.Failure("NOT_FOUND", "Hóa đơn không tồn tại.");

        return Result<InvoiceDto>.Success(_mapper.Map<InvoiceDto>(invoice));
    }

    public async Task<Result<PagedResult<InvoiceDto>>> GetInvoicesAsync(
        int schoolId, string? status, int? studentId, string? billingPeriod,
        int page, int pageSize, CancellationToken ct = default)
    {
        InvoiceStatus? statusEnum = Enum.TryParse<InvoiceStatus>(status, out var s) ? s : null;

        var paged = await _uow.TuitionInvoices.GetPagedAsync(
            pageNumber: page,
            pageSize:   pageSize,
            filter: i =>
                i.SchoolId == schoolId &&
                (!statusEnum.HasValue  || i.Status == statusEnum.Value) &&
                (!studentId.HasValue   || i.StudentId == studentId.Value) &&
                (billingPeriod == null || i.BillingPeriod == billingPeriod),
            orderBy: q => q.OrderByDescending(i => i.DueDate),
            include: q => q
                .Include(i => i.Student)
                .Include(i => i.Payments),
            ct: ct);

        var dtos   = _mapper.Map<List<InvoiceDto>>(paged.Items);
        var result = PagedResult<InvoiceDto>.Create(dtos, paged.TotalCount, page, pageSize);
        return Result<PagedResult<InvoiceDto>>.Success(result);
    }

    public async Task<Result<int>> GenerateInvoicesAsync(
        int schoolId, int createdByUserId, GenerateInvoicesRequest request, CancellationToken ct = default)
    {
        await _uow.ExecuteStoredProcedureAsync(
            "SP_GenerateTuitionInvoices",
            new
            {
                SchoolId        = schoolId,
                AcademicYearId  = request.AcademicYearId,
                BillingPeriod   = request.BillingPeriod,
                CreatedByUserId = createdByUserId
            },
            ct);

        var invoices = await _uow.TuitionInvoices.FindAsync(
            i => i.SchoolId == schoolId && i.BillingPeriod == request.BillingPeriod,
            ct: ct);

        _logger.LogInformation(
            "Generated {Count} invoices for school {SchoolId}, period {Period}",
            invoices.Count, schoolId, request.BillingPeriod);

        return Result<int>.Success(invoices.Count);
    }

    public async Task<Result<IReadOnlyList<StudentDebtDto>>> GetOutstandingDebtsAsync(
        int schoolId, CancellationToken ct = default)
    {
        var invoices = await _uow.TuitionInvoices.FindAsync(
            i => i.SchoolId == schoolId &&
                 (i.Status == InvoiceStatus.Pending || i.Status == InvoiceStatus.Overdue),
            include: q => q.Include(i => i.Student)
                           .Include(i => i.Payments),
            ct: ct);

        var debts = invoices
            .GroupBy(i => i.StudentId)
            .Select(g =>
            {
                var student   = g.First().Student;
                var totalPaid = g.SelectMany(i => i.Payments).Sum(p => p.AmountPaid);
                var totalOwed = g.Sum(i => i.FinalAmount) - totalPaid;
                return new StudentDebtDto(
                    StudentId:        g.Key,
                    StudentCode:      string.Empty,
                    StudentName:      student?.FullName ?? string.Empty,
                    ClassName:        string.Empty,
                    TotalDebt:        totalOwed,
                    OverdueCount:     g.Count(i => i.Status == InvoiceStatus.Overdue),
                    EarliestDueDate:  g.Min(i => i.DueDate),
                    UnpaidInvoices:   _mapper.Map<List<InvoiceSummaryDto>>(g.ToList()));
            })
            .OrderByDescending(d => d.TotalDebt)
            .ToList();

        return Result<IReadOnlyList<StudentDebtDto>>.Success(debts);
    }

    // ─── Payments ─────────────────────────────────────────────────────────────

    public async Task<Result<PaymentSummaryDto>> RecordPaymentAsync(
        int schoolId, int processedByUserId, RecordPaymentRequest request, CancellationToken ct = default)
    {
        var invoice = await _uow.TuitionInvoices.GetByIdAsync(
            request.InvoiceId,
            include: q => q.Include(i => i.Payments),
            ct: ct);

        if (invoice is null || invoice.SchoolId != schoolId)
            return Result<PaymentSummaryDto>.Failure("NOT_FOUND", "Hóa đơn không tồn tại.");

        if (invoice.Status == InvoiceStatus.Paid)
            return Result<PaymentSummaryDto>.Failure("ALREADY_PAID", "Hóa đơn đã được thanh toán.");

        if (request.AmountPaid <= 0)
            return Result<PaymentSummaryDto>.Failure("INVALID_AMOUNT", "Số tiền thanh toán không hợp lệ.");

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var payment = new TuitionPayment
            {
                InvoiceId         = request.InvoiceId,
                SchoolId          = schoolId,
                AmountPaid        = request.AmountPaid,
                PaymentDate       = DateTime.UtcNow,
                PaymentMethod     = request.PaymentMethod,
                TransactionCode   = request.TransactionCode?.Trim(),
                PaymentStatus     = PaymentStatus.Success,
                Note              = request.Note?.Trim(),
                ProcessedByUserId = processedByUserId
            };

            await _uow.TuitionPayments.AddAsync(payment, ct);

            var totalPaid = invoice.Payments.Sum(p => p.AmountPaid) + request.AmountPaid;

            if (totalPaid >= invoice.FinalAmount)
                invoice.Status = InvoiceStatus.Paid;
            else if (totalPaid > 0)
                invoice.Status = InvoiceStatus.Partial;

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Payment recorded: Invoice={InvoiceId}, Amount={Amount}, By={UserId}",
                request.InvoiceId, request.AmountPaid, processedByUserId);

            var summary = new PaymentSummaryDto(
                PaymentId:       payment.Id,
                AmountPaid:      payment.AmountPaid,
                PaymentMethod:   payment.PaymentMethod.ToString(),
                PaymentStatus:   payment.PaymentStatus.ToString(),
                TransactionCode: payment.TransactionCode,
                PaymentDate:     payment.PaymentDate);

            return Result<PaymentSummaryDto>.Success(summary);
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "RecordPayment failed for invoice {InvoiceId}", request.InvoiceId);
            return Result<PaymentSummaryDto>.Failure("INTERNAL_ERROR", "Có lỗi khi ghi nhận thanh toán.");
        }
    }

    // ─── Reports ──────────────────────────────────────────────────────────────

    public async Task<Result<MonthlyFinanceReportDto>> GetMonthlyReportAsync(
        int schoolId, int month, int year, CancellationToken ct = default)
    {
        var school  = await _uow.Schools.GetByIdAsync(schoolId, ct);
        var period  = $"{year:D4}-{month:D2}";

        var invoices = await _uow.TuitionInvoices.FindAsync(
            i => i.SchoolId == schoolId && i.BillingPeriod == period,
            ct: ct);

        var payments = await _uow.TuitionPayments.FindAsync(
            p => p.SchoolId == schoolId &&
                 p.PaymentDate.Month == month && p.PaymentDate.Year == year,
            ct: ct);

        var totalBilled      = invoices.Sum(i => i.FinalAmount);
        var totalCollected   = payments.Sum(p => p.AmountPaid);
        var totalOutstanding = invoices
            .Where(i => i.Status != InvoiceStatus.Paid)
            .Sum(i => i.FinalAmount);

        var dto = new MonthlyFinanceReportDto(
            Month:            month,
            Year:             year,
            SchoolName:       school?.SchoolName ?? string.Empty,
            TotalBilled:      totalBilled,
            TotalCollected:   totalCollected,
            TotalOutstanding: totalOutstanding,
            TotalInvoices:    invoices.Count,
            PaidInvoices:     invoices.Count(i => i.Status == InvoiceStatus.Paid),
            OverdueInvoices:  invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            CollectionRate:   totalBilled > 0
                ? Math.Round(totalCollected / totalBilled * 100, 1)
                : 0,
            ByGradeLevel:     []);

        return Result<MonthlyFinanceReportDto>.Success(dto);
    }
}
