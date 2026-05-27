using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LuminaTutors.Domain.Entities.Academic;

namespace LuminaTutors.Infrastructure.Data.Configurations.Academic;

public class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> b)
    {
        b.ToTable("AcademicYears");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("AcademicYearId").UseIdentityColumn();
        b.Property(x => x.YearName).IsRequired().HasMaxLength(20);
        b.Property(x => x.IsActive).HasDefaultValue(false);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.SchoolId, x.YearName }).IsUnique()
            .HasDatabaseName("UQ_AcademicYears_Name_School");

        b.HasOne(x => x.School).WithMany(s => s.AcademicYears)
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SemesterConfiguration : IEntityTypeConfiguration<Semester>
{
    public void Configure(EntityTypeBuilder<Semester> b)
    {
        b.ToTable("Semesters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("SemesterId").UseIdentityColumn();
        b.Property(x => x.SemesterName).IsRequired().HasMaxLength(50);
        b.Property(x => x.IsActive).HasDefaultValue(false);

        b.HasIndex(x => new { x.SchoolId, x.AcademicYearId, x.SemesterNumber }).IsUnique()
            .HasDatabaseName("UQ_Semesters_School_Year_Num");

        b.HasOne(x => x.AcademicYear).WithMany(a => a.Semesters)
            .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GradeLevelConfiguration : IEntityTypeConfiguration<GradeLevel>
{
    public void Configure(EntityTypeBuilder<GradeLevel> b)
    {
        b.ToTable("GradeLevels");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("GradeLevelId").UseIdentityColumn();
        b.Property(x => x.GradeName).IsRequired().HasMaxLength(20);
        b.Property(x => x.EducationLevel).IsRequired().HasMaxLength(20)
            .HasConversion<string>();

        b.HasIndex(x => new { x.SchoolId, x.GradeNumber }).IsUnique()
            .HasDatabaseName("UQ_GradeLevels_School_Num");

        b.HasOne(x => x.School).WithMany(s => s.GradeLevels)
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subjects");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("SubjectId").UseIdentityColumn();
        b.Property(x => x.SubjectCode).IsRequired().HasMaxLength(20);
        b.Property(x => x.SubjectName).IsRequired().HasMaxLength(100);
        b.Property(x => x.SubjectCategory).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Has3DLab).HasDefaultValue(false);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.SchoolId, x.SubjectCode }).IsUnique()
            .HasDatabaseName("UQ_Subjects_School_Code");

        b.HasOne(x => x.School).WithMany(s => s.Subjects)
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> b)
    {
        b.ToTable("Classes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ClassId").UseIdentityColumn();
        b.Property(x => x.ClassName).IsRequired().HasMaxLength(20);
        b.Property(x => x.MaxStudents).HasDefaultValue((byte)40);
        b.Property(x => x.RoomNumber).HasMaxLength(20);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.SchoolId, x.AcademicYearId, x.ClassName }).IsUnique()
            .HasDatabaseName("UQ_Classes_Name_Year_School");
        b.HasIndex(x => new { x.SchoolId, x.AcademicYearId })
            .HasDatabaseName("IX_Classes_SchoolId_Year");

        b.HasOne(x => x.School).WithMany(s => s.Classes)
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AcademicYear).WithMany(a => a.Classes)
            .HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradeLevel).WithMany(g => g.Classes)
            .HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.HomeRoomTeacher).WithMany()
            .HasForeignKey(x => x.HomeRoomTeacherId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ClassEnrollmentConfiguration : IEntityTypeConfiguration<ClassEnrollment>
{
    public void Configure(EntityTypeBuilder<ClassEnrollment> b)
    {
        b.ToTable("ClassEnrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("EnrollmentId").UseIdentityColumn();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TransferNote).HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.ClassId, x.StudentId }).IsUnique()
            .HasDatabaseName("UQ_ClassEnrollments_Class_Student");
        b.HasIndex(x => x.ClassId).HasDatabaseName("IX_ClassEnrollments_ClassId");
        b.HasIndex(x => x.StudentId).HasDatabaseName("IX_ClassEnrollments_StudentId");

        b.HasOne(x => x.Class).WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubjectAssignmentConfiguration : IEntityTypeConfiguration<SubjectAssignment>
{
    public void Configure(EntityTypeBuilder<SubjectAssignment> b)
    {
        b.ToTable("SubjectAssignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("AssignmentId").UseIdentityColumn();
        b.Property(x => x.PeriodsPerWeek).HasDefaultValue((byte)2);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.SemesterId, x.ClassId, x.SubjectId }).IsUnique()
            .HasDatabaseName("UQ_SubjectAssignments");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Semester).WithMany(s => s.SubjectAssignments)
            .HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Class).WithMany(c => c.SubjectAssignments)
            .HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Subject).WithMany(s => s.SubjectAssignments)
            .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Teacher).WithMany()
            .HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}
