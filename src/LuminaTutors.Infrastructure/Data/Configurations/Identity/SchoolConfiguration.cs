using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Infrastructure.Data.Configurations.Identity;

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> b)
    {
        b.ToTable("Schools");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("SchoolId").UseIdentityColumn();

        b.Property(x => x.SchoolCode).IsRequired().HasMaxLength(20);
        b.Property(x => x.SchoolName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.Province).HasMaxLength(100);
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.LogoUrl).HasMaxLength(500);
        b.Property(x => x.WebsiteUrl).HasMaxLength(300);
        b.Property(x => x.LicenseCode).HasMaxLength(100);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => x.SchoolCode).IsUnique().HasDatabaseName("UQ_Schools_Code");
    }
}
