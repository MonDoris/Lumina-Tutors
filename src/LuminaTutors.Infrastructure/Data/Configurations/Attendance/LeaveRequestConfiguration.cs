using LuminaTutors.Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Attendance;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        b.ToTable("LeaveRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RequestId").UseIdentityColumn();

        b.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        b.Property(x => x.ReviewNote).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => new { x.SchoolId, x.Status })
            .HasDatabaseName("IX_LeaveRequests_School_Status");
        b.HasIndex(x => x.ParentId).HasDatabaseName("IX_LeaveRequests_ParentId");
        b.HasIndex(x => x.StudentId).HasDatabaseName("IX_LeaveRequests_StudentId");

        b.HasOne(x => x.Parent).WithMany()
            .HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReviewedBy).WithMany()
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
