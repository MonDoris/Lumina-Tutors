using LuminaTutors.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.HR;

public class TeacherContractConfiguration : IEntityTypeConfiguration<TeacherContract>
{
    public void Configure(EntityTypeBuilder<TeacherContract> b)
    {
        b.ToTable("TeacherContracts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ContractId").UseIdentityColumn();

        b.Property(x => x.ContractCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.BaseSalary).HasColumnType("decimal(18,2)");
        b.Property(x => x.DocumentUrl).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ContractType).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.SchoolId, x.ContractCode })
            .IsUnique().HasDatabaseName("UQ_TeacherContracts_Code_School");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for Teacher navigation
        b.HasOne(x => x.Teacher).WithMany()
            .HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for CreatedBy navigation
        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StaffAttendanceConfiguration : IEntityTypeConfiguration<StaffAttendance>
{
    public void Configure(EntityTypeBuilder<StaffAttendance> b)
    {
        b.ToTable("StaffAttendances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("StaffAttendanceId").UseIdentityColumn();

        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        // Ignore computed property — calculated in service layer
        b.Ignore(x => x.WorkingHours);

        b.HasIndex(x => new { x.SchoolId, x.UserId, x.AttendanceDate })
            .IsUnique().HasDatabaseName("UQ_StaffAttendances_User_Date");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollConfiguration : IEntityTypeConfiguration<Payroll>
{
    public void Configure(EntityTypeBuilder<Payroll> b)
    {
        b.ToTable("Payrolls");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("PayrollId").UseIdentityColumn();

        b.Property(x => x.BaseSalary).HasColumnType("decimal(18,2)");
        b.Property(x => x.TeachingAllowance).HasColumnType("decimal(18,2)");
        b.Property(x => x.PositionAllowance).HasColumnType("decimal(18,2)");
        b.Property(x => x.OvertimePay).HasColumnType("decimal(18,2)");
        b.Property(x => x.Bonus).HasColumnType("decimal(18,2)");
        b.Property(x => x.InsuranceDeduction).HasColumnType("decimal(18,2)");
        b.Property(x => x.TaxDeduction).HasColumnType("decimal(18,2)");
        b.Property(x => x.OtherDeductions).HasColumnType("decimal(18,2)");
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        // Ignore computed properties — calculated in service layer
        b.Ignore(x => x.GrossIncome);
        b.Ignore(x => x.NetSalary);

        b.HasIndex(x => new { x.SchoolId, x.UserId, x.PayrollYear, x.PayrollMonth })
            .IsUnique().HasDatabaseName("UQ_Payrolls_User_Period");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for User navigation (employee being paid)
        b.HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for ApprovedBy navigation (approving manager)
        b.HasOne(x => x.ApprovedBy).WithMany()
            .HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TeacherEvaluationConfiguration : IEntityTypeConfiguration<TeacherEvaluation>
{
    public void Configure(EntityTypeBuilder<TeacherEvaluation> b)
    {
        b.ToTable("TeacherEvaluations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("EvaluationId").UseIdentityColumn();

        b.Property(x => x.Comments).HasMaxLength(2000);
        b.Property(x => x.EvaluatorRole).HasConversion<string>().HasMaxLength(30);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for Teacher navigation
        b.HasOne(x => x.Teacher).WithMany()
            .HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for Evaluator navigation
        b.HasOne(x => x.Evaluator).WithMany()
            .HasForeignKey(x => x.EvaluatorId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AcademicYear).WithMany()
            .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
    }
}
