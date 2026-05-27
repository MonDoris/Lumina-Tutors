using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Domain.Entities.Finance;

// ─── TuitionFeeConfig ─────────────────────────────────────────────────────────

public class TuitionFeeConfig : AuditableEntity
{
    public int SchoolId { get; set; }
    public int AcademicYearId { get; set; }
    public int? GradeLevelId { get; set; }     // null = applies to entire school
    public string FeeType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public byte DueDayOfMonth { get; set; } = 15;
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int CreatedByUserId { get; set; }

    public School School { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public GradeLevel? GradeLevel { get; set; }
    public User CreatedBy { get; set; } = null!;
    public ICollection<TuitionInvoice> Invoices { get; set; } = [];
}

// ─── TuitionInvoice ───────────────────────────────────────────────────────────

public class TuitionInvoice : AuditableEntity
{
    public int SchoolId { get; set; }
    public int StudentId { get; set; }
    public int ConfigId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Discount { get; set; } = 0M;
    public decimal FinalAmount => Amount - Discount;         // Computed in app layer
    public DateOnly DueDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    public string? Notes { get; set; }
    public string? QRCodeData { get; set; }
    public int CreatedByUserId { get; set; }

    public School School { get; set; } = null!;
    public User Student { get; set; } = null!;
    public TuitionFeeConfig Config { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public ICollection<TuitionPayment> Payments { get; set; } = [];
}

// ─── TuitionPayment ───────────────────────────────────────────────────────────

public class TuitionPayment : BaseEntity
{
    public int InvoiceId { get; set; }
    public int SchoolId { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod PaymentMethod { get; set; }
    public string? TransactionCode { get; set; }
    public string? GatewayResponse { get; set; }   // Raw JSON from gateway
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? Note { get; set; }
    public int? ProcessedByUserId { get; set; }    // Accountant confirms offline

    public TuitionInvoice Invoice { get; set; } = null!;
    public School School { get; set; } = null!;
    public User? ProcessedBy { get; set; }
}
