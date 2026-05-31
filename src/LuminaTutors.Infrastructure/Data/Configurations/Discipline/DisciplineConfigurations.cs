using LuminaTutors.Domain.Entities.Discipline;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Discipline;

public class DisciplineRecordConfiguration : IEntityTypeConfiguration<DisciplineRecord>
{
    public void Configure(EntityTypeBuilder<DisciplineRecord> b)
    {
        b.ToTable("DisciplineRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RecordId").UseIdentityColumn();

        b.Property(x => x.ViolationType).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.ActionTaken).HasMaxLength(2000);
        b.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.SchoolId, x.RecordDate })
            .HasDatabaseName("IX_DisciplineRecords_School_Date");
        b.HasIndex(x => x.StudentId).HasDatabaseName("IX_DisciplineRecords_StudentId");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Student navigation
        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: ReportedBy navigation
        b.HasOne(x => x.ReportedBy).WithMany()
            .HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: EscalatedTo navigation (optional)
        b.HasOne(x => x.EscalatedTo).WithMany()
            .HasForeignKey(x => x.EscalatedToUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GateCheckLogConfiguration : IEntityTypeConfiguration<GateCheckLog>
{
    public void Configure(EntityTypeBuilder<GateCheckLog> b)
    {
        b.ToTable("GateCheckLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("LogId").UseIdentityColumn();

        b.Property(x => x.Note).HasMaxLength(300);
        b.Property(x => x.CheckType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsLate).HasDefaultValue(false);

        b.HasIndex(x => new { x.SchoolId, x.CheckedAt })
            .HasDatabaseName("IX_GateCheckLogs_School_Date");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Student navigation
        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: CheckedBy navigation (optional)
        b.HasOne(x => x.CheckedBy).WithMany()
            .HasForeignKey(x => x.CheckedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
