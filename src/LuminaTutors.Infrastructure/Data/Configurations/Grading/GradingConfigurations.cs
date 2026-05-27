using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LuminaTutors.Domain.Entities.Grading;

namespace LuminaTutors.Infrastructure.Data.Configurations.Grading;

public class GradeCategoryConfiguration : IEntityTypeConfiguration<GradeCategory>
{
    public void Configure(EntityTypeBuilder<GradeCategory> b)
    {
        b.ToTable("GradeCategories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("GradeCategoryId").UseIdentityColumn();
        b.Property(x => x.CategoryCode).IsRequired().HasMaxLength(10);
        b.Property(x => x.CategoryName).IsRequired().HasMaxLength(50);

        b.HasIndex(x => x.CategoryCode).IsUnique().HasDatabaseName("UQ_GradeCategories_Code");

        // Seed TT22 grade types
        b.HasData(
            new GradeCategory { Id = 1, CategoryCode = "DTX", CategoryName = "Điểm thường xuyên", Coefficient = 1, MaxCountPerSemester = null, IsMultipleAllowed = true },
            new GradeCategory { Id = 2, CategoryCode = "DGK", CategoryName = "Điểm giữa kỳ",      Coefficient = 2, MaxCountPerSemester = 1,    IsMultipleAllowed = false },
            new GradeCategory { Id = 3, CategoryCode = "DCK", CategoryName = "Điểm cuối kỳ",      Coefficient = 3, MaxCountPerSemester = 1,    IsMultipleAllowed = false }
        );
    }
}

public class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
{
    public void Configure(EntityTypeBuilder<ScoreEntry> b)
    {
        b.ToTable("ScoreEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ScoreEntryId").UseIdentityColumn();
        b.Property(x => x.Score).HasColumnType("decimal(4,2)");
        b.Property(x => x.Note).HasMaxLength(200);
        b.Property(x => x.IsLocked).HasDefaultValue(false);
        b.Property(x => x.EntryOrder).HasDefaultValue((byte)1);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.StudentId, x.SubjectAssignmentId, x.GradeCategoryId, x.EntryOrder })
            .IsUnique().HasDatabaseName("UQ_ScoreEntries");

        b.HasIndex(x => new { x.StudentId, x.SubjectAssignmentId })
            .HasDatabaseName("IX_ScoreEntries_Student_SubjectAssignment");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SubjectAssignment).WithMany(sa => sa.ScoreEntries)
            .HasForeignKey(x => x.SubjectAssignmentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradeCategory).WithMany(gc => gc.ScoreEntries)
            .HasForeignKey(x => x.GradeCategoryId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.EnteredByTeacher).WithMany()
            .HasForeignKey(x => x.EnteredByTeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GradeBookConfiguration : IEntityTypeConfiguration<GradeBook>
{
    public void Configure(EntityTypeBuilder<GradeBook> b)
    {
        b.ToTable("GradeBook");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("GradeBookId").UseIdentityColumn();
        b.Property(x => x.AverageScore).HasColumnType("decimal(4,2)");
        b.Property(x => x.LetterGrade).HasMaxLength(10);
        b.Property(x => x.Remark).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsCalculated).HasDefaultValue(false);
        b.Property(x => x.IsLocked).HasDefaultValue(false);

        b.HasIndex(x => new { x.StudentId, x.SubjectAssignmentId }).IsUnique()
            .HasDatabaseName("UQ_GradeBook_Student_Subject");
        b.HasIndex(x => x.StudentId).HasDatabaseName("IX_GradeBook_StudentId");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SubjectAssignment).WithMany(sa => sa.GradeBooks)
            .HasForeignKey(x => x.SubjectAssignmentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ApprovedBy).WithMany()
            .HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubjectGradeRequirementConfiguration : IEntityTypeConfiguration<SubjectGradeRequirement>
{
    public void Configure(EntityTypeBuilder<SubjectGradeRequirement> b)
    {
        b.ToTable("SubjectGradeRequirements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RequirementId").UseIdentityColumn();

        b.HasIndex(x => new { x.SchoolId, x.SubjectId, x.GradeLevelId, x.GradeCategoryId })
            .IsUnique().HasDatabaseName("UQ_SubjectGradeRequirements");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany()
            .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GradeLevel).WithMany()
            .HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GradeCategory).WithMany()
            .HasForeignKey(x => x.GradeCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> b)
    {
        b.ToTable("Exams");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ExamId").UseIdentityColumn();

        b.Property(x => x.ExamName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ExamType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.MaxScore).HasColumnType("decimal(5,2)");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Semester).WithMany()
            .HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany()
            .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GradeLevel).WithMany()
            .HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExamRoomConfiguration : IEntityTypeConfiguration<ExamRoom>
{
    public void Configure(EntityTypeBuilder<ExamRoom> b)
    {
        b.ToTable("ExamRooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ExamRoomId").UseIdentityColumn();

        b.Property(x => x.RoomName).IsRequired().HasMaxLength(50);

        b.HasOne(x => x.Exam).WithMany(e => e.ExamRooms)
            .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Supervisor navigation
        b.HasOne(x => x.Supervisor).WithMany()
            .HasForeignKey(x => x.SupervisorId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Assistant navigation (optional)
        b.HasOne(x => x.Assistant).WithMany()
            .HasForeignKey(x => x.AssistantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ExamRoomAssignmentConfiguration : IEntityTypeConfiguration<ExamRoomAssignment>
{
    public void Configure(EntityTypeBuilder<ExamRoomAssignment> b)
    {
        b.ToTable("ExamRoomAssignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("AssignmentId").UseIdentityColumn();

        b.Property(x => x.SeatNumber).IsRequired().HasMaxLength(10);

        b.HasIndex(x => new { x.ExamRoomId, x.StudentId })
            .IsUnique().HasDatabaseName("UQ_ExamRoomAssignments_Room_Student");

        b.HasOne(x => x.ExamRoom).WithMany(r => r.SeatAssignments)
            .HasForeignKey(x => x.ExamRoomId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}
