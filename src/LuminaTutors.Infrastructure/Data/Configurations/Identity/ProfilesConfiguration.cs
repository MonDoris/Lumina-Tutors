using LuminaTutors.Domain.Entities.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Identity;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> b)
    {
        b.ToTable("StudentProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ProfileId").UseIdentityColumn();

        b.Property(x => x.StudentCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.PlaceOfBirth).HasMaxLength(200);
        b.Property(x => x.PermanentAddress).HasMaxLength(500);
        b.Property(x => x.EthnicGroup).HasMaxLength(50);
        b.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.AdmissionType).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.SchoolId, x.StudentCode })
            .IsUnique().HasDatabaseName("UQ_StudentProfiles_Code_School");

        b.HasOne(x => x.User).WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TeacherProfileConfiguration : IEntityTypeConfiguration<TeacherProfile>
{
    public void Configure(EntityTypeBuilder<TeacherProfile> b)
    {
        b.ToTable("TeacherProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ProfileId").UseIdentityColumn();

        b.Property(x => x.TeacherCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.Qualification).HasMaxLength(200);
        b.Property(x => x.SpecializationSubject).HasMaxLength(100);
        b.Property(x => x.TaxCode).HasMaxLength(30);
        b.Property(x => x.BankAccountNumber).HasMaxLength(30);
        b.Property(x => x.BankName).HasMaxLength(100);
        b.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ContractType).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.SchoolId, x.TeacherCode })
            .IsUnique().HasDatabaseName("UQ_TeacherProfiles_Code_School");

        b.HasOne(x => x.User).WithOne(u => u.TeacherProfile)
            .HasForeignKey<TeacherProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParentProfileConfiguration : IEntityTypeConfiguration<ParentProfile>
{
    public void Configure(EntityTypeBuilder<ParentProfile> b)
    {
        b.ToTable("ParentProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ProfileId").UseIdentityColumn();

        b.Property(x => x.Occupation).HasMaxLength(100);
        b.Property(x => x.WorkAddress).HasMaxLength(300);

        b.HasOne(x => x.User).WithOne(u => u.ParentProfile)
            .HasForeignKey<ParentProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupervisorProfileConfiguration : IEntityTypeConfiguration<SupervisorProfile>
{
    public void Configure(EntityTypeBuilder<SupervisorProfile> b)
    {
        b.ToTable("SupervisorProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ProfileId").UseIdentityColumn();

        b.Property(x => x.SupervisorCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => new { x.SchoolId, x.SupervisorCode })
            .IsUnique().HasDatabaseName("UQ_SupervisorProfiles_Code_School");

        b.HasOne(x => x.User).WithOne(u => u.SupervisorProfile)
            .HasForeignKey<SupervisorProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AccountantProfileConfiguration : IEntityTypeConfiguration<AccountantProfile>
{
    public void Configure(EntityTypeBuilder<AccountantProfile> b)
    {
        b.ToTable("AccountantProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ProfileId").UseIdentityColumn();

        b.Property(x => x.AccountantCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => new { x.SchoolId, x.AccountantCode })
            .IsUnique().HasDatabaseName("UQ_AccountantProfiles_Code_School");

        b.HasOne(x => x.User).WithOne(u => u.AccountantProfile)
            .HasForeignKey<AccountantProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ParentStudentRelationConfiguration : IEntityTypeConfiguration<ParentStudentRelation>
{
    public void Configure(EntityTypeBuilder<ParentStudentRelation> b)
    {
        b.ToTable("ParentStudentRelations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RelationId").UseIdentityColumn();

        b.Property(x => x.Relationship).IsRequired().HasMaxLength(50);
        b.Property(x => x.IsPrimaryContact).HasDefaultValue(false);

        b.HasIndex(x => new { x.ParentUserId, x.StudentUserId })
            .IsUnique().HasDatabaseName("UQ_ParentStudentRelations");

        // Explicit FK for Parent navigation (inverse = User.ParentRelations)
        b.HasOne(x => x.Parent)
            .WithMany(u => u.ParentRelations)
            .HasForeignKey(x => x.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicit FK for Student navigation (inverse = User.StudentRelations)
        b.HasOne(x => x.Student)
            .WithMany(u => u.StudentRelations)
            .HasForeignKey(x => x.StudentUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
