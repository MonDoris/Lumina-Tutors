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
                "Đã tồn tại cấu hình học phí cho khối lớp này trong năm học.", "DUPLICATE");

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
            return Result.Failure("Cấu hình học phí không tồn tại.", "NOT_FOUND");

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
            return Result<InvoiceDto>.Failure("Hóa đơn không tồn tại.", "NOT_FOUND");

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

    public async Task<Result<IReadOnlyList<InvoiceDto>>> GetInvoicesForStudentsAsync(
        int schoolId, IReadOnlyCollection<int> studentIds, CancellationToken ct = default)
    {
        if (studentIds.Count == 0)
            return Result<IReadOnlyList<InvoiceDto>>.Success(new List<InvoiceDto>());

        var invoices = await _uow.TuitionInvoices.FindAsync(
            i => i.SchoolId == schoolId && studentIds.Contains(i.StudentId),
            include: q => q.Include(i => i.Student).Include(i => i.Payments),
            ct: ct);

        // Chưa trả (Pending/Partial/Overdue) lên trước, rồi tới hạn gần nhất.
        var ordered = invoices
            .OrderBy(i => i.Status == InvoiceStatus.Paid || i.Status == InvoiceStatus.Cancelled ? 1 : 0)
            .ThenBy(i => i.DueDate)
            .ToList();

        return Result<IReadOnlyList<InvoiceDto>>.Success(_mapper.Map<List<InvoiceDto>>(ordered));
    }

    // ─── CreateInvoice (thủ công 1 học sinh) ─────────────────────────────────

    public async Task<Result<InvoiceDto>> CreateInvoiceAsync(
        int schoolId, int createdByUserId, CreateInvoiceRequest request, CancellationToken ct = default)
    {
        // Validate config belongs to school
        var config = await _uow.TuitionFeeConfigs.GetByIdAsync(request.ConfigId, ct);
        if (config is null || config.SchoolId != schoolId)
            return Result<InvoiceDto>.Failure("Loại phí không tồn tại.", "CFG_NOT_FOUND");

        // Validate student belongs to school
        var student = await _uow.Users.GetByIdAsync(request.StudentId, ct);
        if (student is null || student.SchoolId != schoolId)
            return Result<InvoiceDto>.Failure("Học sinh không tồn tại.", "STU_NOT_FOUND");

        // Check duplicate invoice for same student + period + fee type
        var duplicate = await _uow.TuitionInvoices.AnyAsync(
            i => i.SchoolId == schoolId &&
                 i.StudentId == request.StudentId &&
                 i.ConfigId  == request.ConfigId  &&
                 i.BillingPeriod == request.BillingPeriod, ct);

        if (duplicate)
            return Result<InvoiceDto>.Failure(
                $"Học sinh đã có hóa đơn cho loại phí '{config.FeeType}' kỳ {request.BillingPeriod}.", "DUPLICATE");

        // Generate invoice code
        var count = await _uow.TuitionInvoices.CountAsync(i => i.SchoolId == schoolId, ct);
        var code  = $"INV{schoolId:D3}-{DateTime.UtcNow:yyyyMM}-{count + 1:D4}";

        var invoice = new TuitionInvoice
        {
            SchoolId       = schoolId,
            StudentId      = request.StudentId,
            ConfigId       = request.ConfigId,
            InvoiceCode    = code,
            BillingPeriod  = request.BillingPeriod.Trim(),
            Amount         = request.Amount,
            Discount       = request.Discount,
            DueDate        = request.DueDate,
            Status         = InvoiceStatus.Pending,
            Notes          = request.Notes?.Trim(),
            CreatedByUserId= createdByUserId,
        };

        await _uow.TuitionInvoices.AddAsync(invoice, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Manual invoice {Code} created for student {StudentId} by user {UserId}",
            code, request.StudentId, createdByUserId);

        var created = await GetInvoiceAsync(invoice.Id, ct);
        return created;
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
            return Result<PaymentSummaryDto>.Failure("Hóa đơn không tồn tại.", "NOT_FOUND");

        if (invoice.Status == InvoiceStatus.Paid)
            return Result<PaymentSummaryDto>.Failure("Hóa đơn đã được thanh toán.", "ALREADY_PAID");

        if (request.AmountPaid <= 0)
            return Result<PaymentSummaryDto>.Failure("Số tiền thanh toán không hợp lệ.", "INVALID_AMOUNT");

        var alreadyPaid = invoice.Payments.Sum(p => p.AmountPaid);
        if (alreadyPaid + request.AmountPaid > invoice.FinalAmount)
            return Result<PaymentSummaryDto>.Failure(
                $"Số tiền vượt quá công nợ còn lại ({invoice.FinalAmount - alreadyPaid:N0}đ).", "AMOUNT_EXCEEDS");

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

        try
        {
            // ExecuteInTransactionAsync bọc trong execution strategy — bắt buộc khi bật
            // EnableRetryOnFailure (BeginTransaction thủ công sẽ ném lỗi với retry strategy).
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                await _uow.TuitionPayments.AddAsync(payment, ct);

                var totalPaid = alreadyPaid + request.AmountPaid;
                if (totalPaid >= invoice.FinalAmount)
                    invoice.Status = InvoiceStatus.Paid;
                else if (totalPaid > 0)
                    invoice.Status = InvoiceStatus.Partial;

                await _uow.SaveChangesAsync(ct);
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecordPayment failed for invoice {InvoiceId}", request.InvoiceId);
            return Result<PaymentSummaryDto>.Failure("Có lỗi khi ghi nhận thanh toán.", "INTERNAL_ERROR");
        }

        _logger.LogInformation(
            "Payment recorded: Invoice={InvoiceId}, Amount={Amount}, By={UserId}",
            request.InvoiceId, request.AmountPaid, processedByUserId);

        return Result<PaymentSummaryDto>.Success(new PaymentSummaryDto
        {
            PaymentId       = payment.Id,
            AmountPaid      = payment.AmountPaid,
            PaymentMethod   = payment.PaymentMethod.ToString(),
            PaymentStatus   = payment.PaymentStatus.ToString(),
            TransactionCode = payment.TransactionCode,
            PaymentDate     = payment.PaymentDate
        });
    }

    public async Task<VnPayConfirmResult> ConfirmVnPayPaymentAsync(
        int invoiceId, decimal gatewayAmount, string? transactionNo, string? gatewayRaw, CancellationToken ct = default)
    {
        var invoice = await _uow.TuitionInvoices.GetByIdAsync(
            invoiceId, include: q => q.Include(i => i.Payments), ct: ct);

        if (invoice is null)                       return VnPayConfirmResult.NotFound;
        if (invoice.Status == InvoiceStatus.Paid)  return VnPayConfirmResult.AlreadyConfirmed;
        if (gatewayAmount != invoice.FinalAmount)  return VnPayConfirmResult.InvalidAmount;

        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                await _uow.TuitionPayments.AddAsync(new TuitionPayment
                {
                    InvoiceId         = invoice.Id,
                    SchoolId          = invoice.SchoolId,
                    AmountPaid        = gatewayAmount,
                    PaymentDate       = DateTime.UtcNow,
                    PaymentMethod     = PaymentMethod.VnPay,
                    TransactionCode   = transactionNo,
                    GatewayResponse   = gatewayRaw,
                    PaymentStatus     = PaymentStatus.Success,
                    Note              = "Thanh toán online qua VNPay",
                    ProcessedByUserId = null               // hệ thống tự xác nhận
                }, ct);

                invoice.Status = InvoiceStatus.Paid;

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("VNPay confirmed: Invoice={Id} Amount={Amount} TxnNo={Txn}",
                invoice.Id, gatewayAmount, transactionNo);
            return VnPayConfirmResult.Ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay confirm failed for invoice {Id}", invoiceId);
            return VnPayConfirmResult.Error;
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
