using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Finance;

// ─── Fee Config ───────────────────────────────────────────────────────────────

public record TuitionFeeConfigDto(
    int     ConfigId,
    string  FeeType,
    string? GradeName,
    decimal Amount,
    byte    DueDayOfMonth,
    string  BillingCycle,
    string? Description,
    bool    IsActive
);

public record CreateFeeConfigRequest(
    [Required(ErrorMessage = "Vui lòng nhập loại phí."), MaxLength(100, ErrorMessage = "Loại phí tối đa 100 ký tự.")] string FeeType,
    [Required(ErrorMessage = "Vui lòng chọn năm học.")] int    AcademicYearId,
    int?              GradeLevelId,
    [Required(ErrorMessage = "Vui lòng nhập số tiền."), Range(1000, 100_000_000, ErrorMessage = "Số tiền phải từ 1.000 đến 100.000.000.")] decimal Amount,
    byte              DueDayOfMonth = 15,
    BillingCycle      BillingCycle  = BillingCycle.Monthly,
    string?           Description   = null
);

// ─── Invoice ──────────────────────────────────────────────────────────────────

public record InvoiceDto(
    int      InvoiceId,
    string   InvoiceCode,
    int      StudentId,
    string   StudentName,
    string   StudentCode,
    string   ClassName,
    string   FeeType,
    string   BillingPeriod,
    decimal  Amount,
    decimal  Discount,
    decimal  FinalAmount,
    DateOnly DueDate,
    string   Status,
    bool     IsOverdue,
    string?  QRCodeData,
    DateTime CreatedAt,
    List<PaymentSummaryDto> Payments
);

public record PaymentSummaryDto(
    int      PaymentId,
    decimal  AmountPaid,
    string   PaymentMethod,
    string   PaymentStatus,
    string?  TransactionCode,
    DateTime PaymentDate
);

public record GenerateInvoicesRequest(
    [Required(ErrorMessage = "Vui lòng chọn năm học.")] int    AcademicYearId,
    [Required(ErrorMessage = "Vui lòng nhập kỳ thanh toán.")] string BillingPeriod,    // "2024-09" or "HK1-2024-2025"
    [Required(ErrorMessage = "Vui lòng nhập ngày đến hạn.")] DateOnly DueDate,
    int?  TargetGradeLevelId = null,    // null = all grades
    int?  TargetClassId      = null     // null = all classes
);

public record RecordPaymentRequest(
    [Required(ErrorMessage = "Vui lòng chọn hóa đơn.")] int           InvoiceId,
    [Required(ErrorMessage = "Vui lòng nhập số tiền thanh toán."), Range(1000, 100_000_000, ErrorMessage = "Số tiền phải từ 1.000 đến 100.000.000.")] decimal AmountPaid,
    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")] PaymentMethod PaymentMethod,
    string? TransactionCode  = null,
    string? Note             = null
);

// ─── Finance Reports ──────────────────────────────────────────────────────────

public record MonthlyFinanceReportDto(
    int     Month,
    int     Year,
    string  SchoolName,
    decimal TotalBilled,
    decimal TotalCollected,
    decimal TotalOutstanding,
    int     TotalInvoices,
    int     PaidInvoices,
    int     OverdueInvoices,
    decimal CollectionRate,
    List<GradeLevelCollectionDto> ByGradeLevel
);

public record GradeLevelCollectionDto(
    string  GradeName,
    decimal Billed,
    decimal Collected,
    decimal Outstanding,
    int     StudentCount
);

// ─── Student Debt Summary ─────────────────────────────────────────────────────

public record StudentDebtDto(
    int      StudentId,
    string   StudentCode,
    string   StudentName,
    string   ClassName,
    decimal  TotalDebt,
    int      OverdueCount,
    DateOnly EarliestDueDate,
    List<InvoiceSummaryDto> UnpaidInvoices
);

public record InvoiceSummaryDto(
    int      InvoiceId,
    string   InvoiceCode,
    string   FeeType,
    string   BillingPeriod,
    decimal  FinalAmount,
    DateOnly DueDate,
    string   Status
);
