using LuminaTutors.Domain.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Learning;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.ToTable("Lessons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("LessonId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.LessonType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Lab3DConfig).HasMaxLength(2000);
        b.Property(x => x.IsPublished).HasDefaultValue(false);
        b.Property(x => x.Is3DEnabled).HasDefaultValue(false);

        b.HasIndex(x => x.SubjectAssignmentId).HasDatabaseName("IX_Lessons_SubjectAssignmentId");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SubjectAssignment).WithMany(sa => sa.Lessons)
            .HasForeignKey(x => x.SubjectAssignmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LessonMaterialConfiguration : IEntityTypeConfiguration<LessonMaterial>
{
    public void Configure(EntityTypeBuilder<LessonMaterial> b)
    {
        b.ToTable("LessonMaterials");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("MaterialId").UseIdentityColumn();

        b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        b.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.FileType).HasConversion<string>().HasMaxLength(30);

        b.HasOne(x => x.Lesson).WithMany(l => l.Materials)
            .HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class QuestionBankConfiguration : IEntityTypeConfiguration<QuestionBank>
{
    public void Configure(EntityTypeBuilder<QuestionBank> b)
    {
        b.ToTable("QuestionBanks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("QuestionId").UseIdentityColumn();

        b.Property(x => x.QuestionText).IsRequired();
        b.Property(x => x.ExplanationText).HasMaxLength(2000);
        b.Property(x => x.ChapterTag).HasMaxLength(100);
        b.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.DifficultyLevel).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsApproved).HasDefaultValue(false);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Subject).WithMany()
            .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradeLevel).WithMany()
            .HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByTeacher).WithMany()
            .HasForeignKey(x => x.CreatedByTeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> b)
    {
        b.ToTable("QuestionOptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("OptionId").UseIdentityColumn();

        b.Property(x => x.OptionText).IsRequired().HasMaxLength(500);
        b.Property(x => x.IsCorrect).HasDefaultValue(false);

        // char is not natively supported — store as nvarchar(1)
        b.Property(x => x.OptionLabel)
            .HasConversion(c => c.ToString(), s => s[0])
            .HasMaxLength(1);

        b.HasIndex(x => new { x.QuestionId, x.OptionLabel })
            .IsUnique().HasDatabaseName("UQ_QuestionOptions_Question_Label");

        b.HasOne(x => x.Question).WithMany(q => q.Options)
            .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("Assignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("AssignmentId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.MaxScore).HasColumnType("decimal(5,2)").HasDefaultValue(10M);
        b.Property(x => x.AssignmentType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.IsPublished).HasDefaultValue(false);
        b.Property(x => x.AllowLateSubmission).HasDefaultValue(false);
        b.Property(x => x.LatePenaltyPercent).HasDefaultValue((byte)0);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SubjectAssignment).WithMany(sa => sa.Assignments)
            .HasForeignKey(x => x.SubjectAssignmentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradeCategory).WithMany()
            .HasForeignKey(x => x.GradeCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> b)
    {
        b.ToTable("AssignmentSubmissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("SubmissionId").UseIdentityColumn();

        b.Property(x => x.Score).HasColumnType("decimal(5,2)");
        b.Property(x => x.Feedback).HasMaxLength(2000);
        b.Property(x => x.IsLate).HasDefaultValue(false);
        b.Property(x => x.SubmissionStatus).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.AssignmentId, x.StudentId })
            .IsUnique().HasDatabaseName("UQ_AssignmentSubmissions_Assignment_Student");

        b.HasOne(x => x.Assignment).WithMany(a => a.Submissions)
            .HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Student navigation
        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: GradedBy navigation
        b.HasOne(x => x.GradedBy).WithMany()
            .HasForeignKey(x => x.GradedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> b)
    {
        b.ToTable("SubmissionFiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("FileId").UseIdentityColumn();

        b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        b.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.FileType).IsRequired().HasMaxLength(50);

        b.HasOne(x => x.Submission).WithMany(s => s.Files)
            .HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VirtualLabSessionConfiguration : IEntityTypeConfiguration<VirtualLabSession>
{
    public void Configure(EntityTypeBuilder<VirtualLabSession> b)
    {
        b.ToTable("VirtualLabSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("SessionId").UseIdentityColumn();

        b.Property(x => x.SessionName).IsRequired().HasMaxLength(100);
        b.Property(x => x.SessionCode).IsRequired().HasMaxLength(6).IsFixedLength();
        b.Property(x => x.SubjectTag).IsRequired().HasMaxLength(30);
        b.Property(x => x.SceneType).IsRequired().HasMaxLength(30);
        b.Property(x => x.MaxParticipants).HasDefaultValue(40);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.SchoolId, x.SessionCode })
            .HasDatabaseName("IX_VirtualLabSessions_School_Code");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Teacher).WithMany()
            .HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}
