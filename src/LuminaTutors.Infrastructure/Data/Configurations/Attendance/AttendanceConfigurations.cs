using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Enums;

namespace LuminaTutors.Infrastructure.Data.Configurations.Attendance;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> b)
    {
        b.ToTable("Schedules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ScheduleId").UseIdentityColumn();
        b.Property(x => x.RoomOverride).HasMaxLength(20);
        b.Property(x => x.IsActive).HasDefaultValue(true);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => x.SemesterId).HasDatabaseName("IX_Schedules_SemesterId");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Semester).WithMany()
            .HasForeignKey(x => x.SemesterId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SubjectAssignment).WithMany(sa => sa.Schedules)
            .HasForeignKey(x => x.SubjectAssignmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> b)
    {
        b.ToTable("AttendanceSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("SessionId").UseIdentityColumn();
        b.Property(x => x.QRToken).HasDefaultValueSql("NEWID()");
        b.Property(x => x.TopicNote).HasMaxLength(300);
        b.Property(x => x.SessionStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        b.HasIndex(x => new { x.ScheduleId, x.SessionDate }).IsUnique()
            .HasDatabaseName("UQ_AttendanceSessions_Schedule_Date");

        b.HasIndex(x => x.QRToken)
            .HasDatabaseName("IX_AttendanceSessions_QRToken")
            .HasFilter("[SessionStatus] = 'Open'");

        b.HasIndex(x => new { x.ScheduleId, x.SessionDate })
            .HasDatabaseName("IX_AttendanceSessions_ScheduleId_Date");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Schedule).WithMany(s => s.AttendanceSessions)
            .HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByTeacher).WithMany()
            .HasForeignKey(x => x.CreatedByTeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StudentAttendanceConfiguration : IEntityTypeConfiguration<StudentAttendance>
{
    public void Configure(EntityTypeBuilder<StudentAttendance> b)
    {
        b.ToTable("Attendances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("AttendanceId").UseIdentityColumn();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CheckMethod).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Note).HasMaxLength(300);
        b.Property(x => x.NotifiedParent).HasDefaultValue(false);

        b.HasIndex(x => new { x.SessionId, x.StudentId }).IsUnique()
            .HasDatabaseName("UQ_Attendances_Session_Student");
        b.HasIndex(x => x.SessionId).HasDatabaseName("IX_Attendances_SessionId");
        b.HasIndex(x => x.StudentId).HasDatabaseName("IX_Attendances_StudentId");
        b.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Attendances_Status")
            .HasFilter("[Status] = 'Absent'");

        b.HasOne(x => x.Session).WithMany(s => s.Attendances)
            .HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.UpdatedByTeacher).WithMany()
            .HasForeignKey(x => x.UpdatedByTeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScheduleChangeLogConfiguration : IEntityTypeConfiguration<ScheduleChangeLog>
{
    public void Configure(EntityTypeBuilder<ScheduleChangeLog> b)
    {
        b.ToTable("ScheduleChangeLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("LogId").UseIdentityColumn();

        b.Property(x => x.ChangeType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.OldRoomNumber).HasMaxLength(20);
        b.Property(x => x.NewRoomNumber).HasMaxLength(20);
        b.Property(x => x.Reason).HasMaxLength(500);

        b.HasOne(x => x.Schedule).WithMany(s => s.ChangeLogs)
            .HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ChangedBy).WithMany()
            .HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SchoolEventConfiguration : IEntityTypeConfiguration<SchoolEvent>
{
    public void Configure(EntityTypeBuilder<SchoolEvent> b)
    {
        b.ToTable("SchoolEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("EventId").UseIdentityColumn();

        b.Property(x => x.EventName).IsRequired().HasMaxLength(200);
        b.Property(x => x.EventType).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.OrganizedBy).WithMany()
            .HasForeignKey(x => x.OrganizedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EventAttendanceConfiguration : IEntityTypeConfiguration<EventAttendance>
{
    public void Configure(EntityTypeBuilder<EventAttendance> b)
    {
        b.ToTable("EventAttendances");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("EventAttendanceId").UseIdentityColumn();

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Note).HasMaxLength(300);

        b.HasIndex(x => new { x.EventId, x.StudentId })
            .IsUnique().HasDatabaseName("UQ_EventAttendances_Event_Student");

        b.HasOne(x => x.Event).WithMany(e => e.EventAttendances)
            .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: Student navigation
        b.HasOne(x => x.Student).WithMany()
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // Explicit FK: CheckedBy navigation (optional)
        b.HasOne(x => x.CheckedBy).WithMany()
            .HasForeignKey(x => x.CheckedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
