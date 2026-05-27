using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LuminaTutors.Domain.Entities.Identity;

namespace LuminaTutors.Infrastructure.Data.Configurations.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RoleId").UseIdentityColumn();
        b.Property(x => x.RoleName).IsRequired().HasMaxLength(50);
        b.Property(x => x.RoleCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.Description).HasMaxLength(300);

        b.HasIndex(x => x.RoleCode).IsUnique().HasDatabaseName("UQ_Roles_Code");

        // Seed the 6 fixed roles
        b.HasData(
            new Role { Id = 1, RoleName = "Quản trị viên", RoleCode = "ADMIN",      Description = "Quản trị toàn bộ hệ thống" },
            new Role { Id = 2, RoleName = "Giáo viên",     RoleCode = "TEACHER",    Description = "Tạo bài giảng, quản lý lớp, chấm điểm" },
            new Role { Id = 3, RoleName = "Học sinh",       RoleCode = "STUDENT",    Description = "Học trực tuyến, vào phòng 3D, làm bài tập" },
            new Role { Id = 4, RoleName = "Phụ huynh",     RoleCode = "PARENT",     Description = "Theo dõi học lực, điểm danh, nhận thông báo" },
            new Role { Id = 5, RoleName = "Giám thị",      RoleCode = "SUPERVISOR", Description = "Giám sát kỷ luật toàn trường, coi thi, nề nếp" },
            new Role { Id = 6, RoleName = "Kế toán",       RoleCode = "ACCOUNTANT", Description = "Quản lý học phí, hóa đơn và báo cáo tài chính" }
        );
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("UserId").UseIdentityColumn();

        b.Property(x => x.Email).IsRequired().HasMaxLength(150);
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(150);
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.IsEmailVerified).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        b.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.Email, x.SchoolId }).IsUnique()
            .HasDatabaseName("UQ_Users_Email_School");
        b.HasIndex(x => x.SchoolId).HasDatabaseName("IX_Users_SchoolId");
        b.HasIndex(x => new { x.RoleId, x.SchoolId }).HasDatabaseName("IX_Users_RoleId_SchoolId");

        // Relationships
        b.HasOne(x => x.School).WithMany(s => s.Users)
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Role).WithMany(r => r.Users)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("TokenId").UseIdentityColumn();

        b.Property(x => x.Token).IsRequired().HasMaxLength(500);
        b.Property(x => x.ReplacedByToken).HasMaxLength(500);
        b.Property(x => x.CreatedByIp).HasMaxLength(50);
        b.Property(x => x.RevokedByIp).HasMaxLength(50);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        b.Ignore(x => x.IsExpired);
        b.Ignore(x => x.IsRevoked);
        b.Ignore(x => x.IsActive);
    }
}

public class InviteLinkConfiguration : IEntityTypeConfiguration<InviteLink>
{
    public void Configure(EntityTypeBuilder<InviteLink> b)
    {
        b.ToTable("InviteLinks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("InviteId").UseIdentityColumn();

        b.Property(x => x.Token).HasDefaultValueSql("NEWID()");
        b.Property(x => x.TargetEmail).HasMaxLength(150);
        b.Property(x => x.IsRevoked).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => x.Token).IsUnique().HasDatabaseName("UQ_InviteLinks_Token");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetRole).WithMany()
            .HasForeignKey(x => x.TargetRoleId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.LinkedStudent).WithMany()
            .HasForeignKey(x => x.LinkedStudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.UsedBy).WithMany()
            .HasForeignKey(x => x.UsedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.Ignore(x => x.IsExpired);
        b.Ignore(x => x.IsUsed);
        b.Ignore(x => x.IsValid);
    }
}
