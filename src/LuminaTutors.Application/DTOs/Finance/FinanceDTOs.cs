using System.ComponentModel.DataAnnotations;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Application.DTOs.Finance;

// ─── Fee Config ───────────────────────────────────────────────────────────────

public class TuitionFeeConfigDto
{
    public int     ConfigId      { get; init; }
    public string  FeeType       { get; init; } = "";
    public string? GradeName     { get; init; }
    public decimal Amount        { get; init; }
    public byte    DueDayOfMonth { get; init; }
    public string  BillingCycle  { get; init; } = "";
    public string? Description   { get; init; }
    public bool    IsActive      { get; init; }
}

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

public class InvoiceDto
{
    public int      InvoiceId     { get; init; }
    public string   InvoiceCode   { get; init; } = "";
    public int      StudentId     { get; init; }
    public string   StudentName   { get; init; } = "";
    public string   StudentCode   { get; init; } = "";
    public string   ClassName     { get; init; } = "";
    public string   FeeType       { get; init; } = "";
    public string   BillingPeriod { get; init; } = "";
    public decimal  Amount        { get; init; }
    public decimal  Discount      { get; init; }
    public decimal  FinalAmount   { get; init; }
    public DateOnly DueDate       { get; init; }
    public string   Status        { get; init; } = "";
    public bool     IsOverdue     { get; init; }
    public string?  QRCodeData    { get; init; }
    public DateTime CreatedAt     { get; init; }
    public List<PaymentSummaryDto> Payments { get; init; } = new();
}

public class PaymentSummaryDto
{
    public int      PaymentId       { get; init; }
    public decimal  AmountPaid      { get; init; }
    public string   PaymentMethod   { get; init; } = "";
    public string   PaymentStatus   { get; init; } = "";
    public string?  TransactionCode { get; init; }
    public DateTime PaymentDate     { get; init; }
}

public record CreateInvoiceRequest(
    [Required(ErrorMessage = "Vui lòng chọn học sinh.")]
    int      StudentId,

    [Required(ErrorMessage = "Vui lòng chọn loại phí.")]
    int      ConfigId,

    [Required(ErrorMessage = "Vui lòng nhập kỳ thu."), MaxLength(20)]
    string   BillingPeriod,

    [Required(ErrorMessage = "Vui lòng nhập số tiền."),
     Range(1000, 100_000_000, ErrorMessage = "Số tiền phải từ 1.000 đến 100.000.000.")]
    decimal  Amount,

    [Range(0, 100_000_000, ErrorMessage = "Giảm giá không hợp lệ.")]
    decimal  Discount,

    [Required(ErrorMessage = "Vui lòng nhập hạn thanh toán.")]
    DateOnly DueDate,

    string?  Notes = null
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

public class InvoiceSummaryDto
{
    public int      InvoiceId     { get; init; }
    public string   InvoiceCode   { get; init; } = "";
    public string   FeeType       { get; init; } = "";
    public string   BillingPeriod { get; init; } = "";
    public decimal  FinalAmount   { get; init; }
    public DateOnly DueDate       { get; init; }
    public string   Status        { get; init; } = "";
}
