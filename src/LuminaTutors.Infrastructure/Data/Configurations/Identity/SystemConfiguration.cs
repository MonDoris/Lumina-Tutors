using LuminaTutors.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Identity;

public class SystemConfigConfiguration : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> b)
    {
        b.ToTable("SystemConfigs");
        b.HasKey(x => new { x.SchoolId, x.ConfigKey });

        b.Property(x => x.ConfigKey).IsRequired().HasMaxLength(100);
        b.Property(x => x.ConfigValue).IsRequired().HasMaxLength(2000);
        b.Property(x => x.DataType).IsRequired().HasMaxLength(20).HasDefaultValue("STRING");
        b.Property(x => x.Description).HasMaxLength(300);

        b.HasOne(x => x.School).WithMany(s => s.SystemConfigs)
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.UpdatedBy).WithMany()
            .HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.HasKey(x => x.LogId);
        b.Property(x => x.LogId).UseIdentityColumn();

        b.Property(x => x.Action).IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityType).HasMaxLength(100);
        b.Property(x => x.EntityId).HasMaxLength(50);
        b.Property(x => x.IPAddress).HasMaxLength(50);
        b.Property(x => x.UserAgent).HasMaxLength(500);

        b.HasIndex(x => new { x.SchoolId, x.Timestamp })
            .HasDatabaseName("IX_AuditLogs_School_Timestamp");
        b.HasIndex(x => x.UserId).HasDatabaseName("IX_AuditLogs_UserId");

        // No cascade — AuditLog is immutable, no FK navigation needed
    }
}
