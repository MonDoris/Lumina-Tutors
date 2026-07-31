using LuminaTutors.Application.DTOs.Finance;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.Interfaces.Services;

public interface IFinanceService
{
    // Fee configs
    Task<Result<IReadOnlyList<TuitionFeeConfigDto>>> GetFeeConfigsAsync(int schoolId, int academicYearId, CancellationToken ct = default);
    Task<Result<TuitionFeeConfigDto>>                CreateFeeConfigAsync(int schoolId, int createdByUserId, CreateFeeConfigRequest request, CancellationToken ct = default);
    Task<Result>                                     DeactivateFeeConfigAsync(int configId, CancellationToken ct = default);

    // Invoices
    Task<Result<InvoiceDto>>                         GetInvoiceAsync(int invoiceId, CancellationToken ct = default);
    Task<Result<PagedResult<InvoiceDto>>>            GetInvoicesAsync(int schoolId, string? status, int? studentId, string? billingPeriod, int page, int pageSize, CancellationToken ct = default);
    Task<Result<InvoiceDto>>                         CreateInvoiceAsync(int schoolId, int createdByUserId, CreateInvoiceRequest request, CancellationToken ct = default);
    Task<Result<int>>                                GenerateInvoicesAsync(int schoolId, int createdByUserId, GenerateInvoicesRequest request, CancellationToken ct = default);
    Task<Result<IReadOnlyList<StudentDebtDto>>>      GetOutstandingDebtsAsync(int schoolId, CancellationToken ct = default);

    /// <summary>Hóa đơn của các con (dùng cho cổng Phụ huynh) — chưa thanh toán xếp trước.</summary>
    Task<Result<IReadOnlyList<InvoiceDto>>>          GetInvoicesForStudentsAsync(int schoolId, IReadOnlyCollection<int> studentIds, CancellationToken ct = default);

    // Payments
    Task<Result<PaymentSummaryDto>>                  RecordPaymentAsync(int schoolId, int processedByUserId, RecordPaymentRequest request, CancellationToken ct = default);

    /// <summary>Xác nhận thanh toán VNPay từ IPN (không cần đăng nhập). Idempotent — tra theo Id hóa đơn.</summary>
    Task<VnPayConfirmResult>                          ConfirmVnPayPaymentAsync(int invoiceId, decimal gatewayAmount, string? transactionNo, string? gatewayRaw, CancellationToken ct = default);

    // Reports
    Task<Result<MonthlyFinanceReportDto>>            GetMonthlyReportAsync(int schoolId, int month, int year, CancellationToken ct = default);
}
