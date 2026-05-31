using LuminaTutors.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Finance;

public class TuitionFeeConfigConfiguration : IEntityTypeConfiguration<TuitionFeeConfig>
{
    public void Configure(EntityTypeBuilder<TuitionFeeConfig> b)
    {
        b.ToTable("TuitionFeeConfigs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ConfigId").UseIdentityColumn();

        b.Property(x => x.FeeType).IsRequired().HasMaxLength(100);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AcademicYear).WithMany()
            .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradeLevel).WithMany()
            .HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TuitionInvoiceConfiguration : IEntityTypeConfiguration<TuitionInvoice>
{
    public void Configure(EntityTypeBuilder<TuitionInvoice> b)
    {
        b.ToTable("TuitionInvoices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("InvoiceId").UseIdentityColumn();

        b.Property(x => x.InvoiceCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.BillingPeriod).IsRequired().HasMaxLength(20);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.Discount).HasColumnType("decimal(18,2)").HasDefaultValue(0M);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.QRCodeData).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        // FinalAmount is computed in app layer — do not persist
        b.Ignore(x => x.FinalAmount);

        b.HasIndex(x => new { x.SchoolId, x.InvoiceCode })
            .IsUnique().HasDatabaseName("UQ_TuitionInvoices_Code_School");
        b.HasIndex(x => new { x.StudentId, x.BillingPeriod, x.ConfigId })
            .IsUnique().HasDatabaseName("UQ_TuitionInvoices_Student_Period_Config");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_TuitionInvoices_Status");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Student navigation (User playing student role)
        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: CreatedBy navigation (User who created the invoice)
        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Config).WithMany(c => c.Invoices)
            .HasForeignKey(x => x.ConfigId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TuitionPaymentConfiguration : IEntityTypeConfiguration<TuitionPayment>
{
    public void Configure(EntityTypeBuilder<TuitionPayment> b)
    {
        b.ToTable("TuitionPayments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("PaymentId").UseIdentityColumn();

        b.Property(x => x.AmountPaid).HasColumnType("decimal(18,2)");
        b.Property(x => x.TransactionCode).HasMaxLength(100);
        b.Property(x => x.GatewayResponse).HasMaxLength(2000);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => x.InvoiceId).HasDatabaseName("IX_TuitionPayments_InvoiceId");

        b.HasOne(x => x.Invoice).WithMany(i => i.Payments)
            .HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ProcessedBy).WithMany()
            .HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
