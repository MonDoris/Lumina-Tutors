using LuminaTutors.Domain.Entities.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Learning;

// ═════════════════════════════════════════════════════════════════════════════
//  E-Learning: Course / Module / Lesson / Enrollment / Progress
//
//  Chiến lược khóa ngoại (tránh multiple cascade paths trên SQL Server):
//  • Cascade CHỈ trong chuỗi tuyến tính:  Course → Module → Lesson → Material
//                                          Enrollment → LessonProgress
//  • Mọi FK chéo aggregate: Restrict — service chịu trách nhiệm dọn dẹp.
//  • CourseLesson → CourseEnrollment.LastLessonId: SetNull (resume point tự xóa).
// ═════════════════════════════════════════════════════════════════════════════

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("Courses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("CourseId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.IsSequential).HasDefaultValue(false);

        b.HasIndex(x => new { x.SchoolId, x.Status }).HasDatabaseName("IX_Courses_School_Status");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Subject).WithMany()
            .HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.GradeLevel).WithMany()
            .HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CourseModuleConfiguration : IEntityTypeConfiguration<CourseModule>
{
    public void Configure(EntityTypeBuilder<CourseModule> b)
    {
        b.ToTable("CourseModules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("CourseModuleId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(1000);

        b.HasIndex(x => new { x.CourseId, x.SortOrder }).HasDatabaseName("IX_CourseModules_Course_Sort");

        b.HasOne(x => x.Course).WithMany(c => c.Modules)
            .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CourseLessonConfiguration : IEntityTypeConfiguration<CourseLesson>
{
    public void Configure(EntityTypeBuilder<CourseLesson> b)
    {
        b.ToTable("CourseLessons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("CourseLessonId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.ContentType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.VideoUrl).HasMaxLength(500);
        b.Property(x => x.MinWatchPercent).HasDefaultValue((byte)90);
        b.Property(x => x.IsPreviewable).HasDefaultValue(false);
        b.Property(x => x.IsPublished).HasDefaultValue(false);
        b.Property(x => x.Objectives).HasMaxLength(2000);
        b.Property(x => x.CognitiveLevel).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.PeriodCount).HasDefaultValue((byte)1);

        b.HasIndex(x => new { x.ModuleId, x.SortOrder }).HasDatabaseName("IX_CourseLessons_Module_Sort");

        b.HasOne(x => x.Module).WithMany(m => m.Lessons)
            .HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.QuizExam).WithMany()
            .HasForeignKey(x => x.QuizExamId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CourseLessonMaterialConfiguration : IEntityTypeConfiguration<CourseLessonMaterial>
{
    public void Configure(EntityTypeBuilder<CourseLessonMaterial> b)
    {
        b.ToTable("CourseLessonMaterials");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("CourseMaterialId").UseIdentityColumn();

        b.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        b.Property(x => x.FileUrl).IsRequired().HasMaxLength(500);
        b.Property(x => x.FileType).HasConversion<string>().HasMaxLength(30);

        b.HasOne(x => x.CourseLesson).WithMany(l => l.Materials)
            .HasForeignKey(x => x.CourseLessonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClassCourseAssignmentConfiguration : IEntityTypeConfiguration<ClassCourseAssignment>
{
    public void Configure(EntityTypeBuilder<ClassCourseAssignment> b)
    {
        b.ToTable("ClassCourseAssignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ClassCourseAssignmentId").UseIdentityColumn();

        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.CourseId, x.ClassId })
            .IsUnique().HasDatabaseName("UQ_ClassCourseAssignments_Course_Class");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Course).WithMany(c => c.ClassAssignments)
            .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Class).WithMany()
            .HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AssignedBy).WithMany()
            .HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CourseEnrollmentConfiguration : IEntityTypeConfiguration<CourseEnrollment>
{
    public void Configure(EntityTypeBuilder<CourseEnrollment> b)
    {
        b.ToTable("CourseEnrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("CourseEnrollmentId").UseIdentityColumn();

        b.Property(x => x.Source).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProgressPercent).HasColumnType("decimal(5,2)").HasDefaultValue(0M);
        b.Property(x => x.CompletedLessonCount).HasDefaultValue(0);

        b.HasIndex(x => new { x.CourseId, x.StudentId })
            .IsUnique().HasDatabaseName("UQ_CourseEnrollments_Course_Student");
        b.HasIndex(x => new { x.SchoolId, x.StudentId })
            .HasDatabaseName("IX_CourseEnrollments_School_Student");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Course).WithMany(c => c.Enrollments)
            .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ClassAssignment).WithMany(a => a.Enrollments)
            .HasForeignKey(x => x.ClassCourseAssignmentId).OnDelete(DeleteBehavior.Restrict);

        // Bài học bị xóa → resume point tự về null (một cascade path duy nhất tới bảng này)
        b.HasOne(x => x.LastLesson).WithMany()
            .HasForeignKey(x => x.LastLessonId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> b)
    {
        b.ToTable("LessonProgress");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ProgressId").UseIdentityColumn();

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.LastPositionSec).HasDefaultValue(0);
        b.Property(x => x.WatchedSec).HasDefaultValue(0);
        b.Property(x => x.TimeSpentSec).HasDefaultValue(0);

        b.HasIndex(x => new { x.EnrollmentId, x.CourseLessonId })
            .IsUnique().HasDatabaseName("UQ_LessonProgress_Enrollment_Lesson");
        b.HasIndex(x => x.CourseLessonId).HasDatabaseName("IX_LessonProgress_Lesson");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Enrollment).WithMany(e => e.LessonProgresses)
            .HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);

        // Restrict: service phải xóa progress trước khi xóa bài học (tránh multiple cascade paths)
        b.HasOne(x => x.Lesson).WithMany()
            .HasForeignKey(x => x.CourseLessonId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.QuizAttempt).WithMany()
            .HasForeignKey(x => x.QuizAttemptId).OnDelete(DeleteBehavior.Restrict);
    }
}
