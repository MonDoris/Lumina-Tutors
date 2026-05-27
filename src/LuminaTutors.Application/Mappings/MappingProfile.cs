using AutoMapper;
using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Application.DTOs.Attendance;
using LuminaTutors.Application.DTOs.Class;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.DTOs.Discipline;
using LuminaTutors.Application.DTOs.Finance;
using LuminaTutors.Application.DTOs.Grading;
using LuminaTutors.Application.DTOs.HR;
using LuminaTutors.Application.DTOs.Student;
using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Attendance;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Entities.Discipline;
using LuminaTutors.Domain.Entities.Finance;
using LuminaTutors.Domain.Entities.Grading;
using LuminaTutors.Domain.Entities.HR;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Profiles;

namespace LuminaTutors.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── Auth ─────────────────────────────────────────────────────────────
        CreateMap<User, LoginResponse>()
            .ForMember(d => d.UserId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RoleCode,  o => o.MapFrom(s => s.Role.RoleCode))
            .ForMember(d => d.RoleName,  o => o.MapFrom(s => s.Role.RoleName))
            .ForMember(d => d.SchoolName,o => o.MapFrom(s => s.School.SchoolName));

        CreateMap<User, CurrentUserDto>()
            .ForMember(d => d.UserId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.RoleCode,  o => o.MapFrom(s => s.Role.RoleCode))
            .ForMember(d => d.RoleName,  o => o.MapFrom(s => s.Role.RoleName))
            .ForMember(d => d.SchoolName,o => o.MapFrom(s => s.School.SchoolName));

        CreateMap<InviteLink, InviteLinkDto>()
            .ForMember(d => d.InviteId,          o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TargetRoleName,    o => o.MapFrom(s => s.TargetRole.RoleName))
            .ForMember(d => d.LinkedStudentName, o => o.MapFrom(s => s.LinkedStudent != null ? s.LinkedStudent.FullName : null));

        // ── Student ──────────────────────────────────────────────────────────
        CreateMap<User, StudentSummaryDto>()
            .ForMember(d => d.UserId,      o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StudentCode, o => o.MapFrom(s => s.StudentProfile != null ? s.StudentProfile.StudentCode : ""))
            .ForMember(d => d.ClassName,   o => o.Ignore())
            .ForMember(d => d.GradeName,   o => o.Ignore());

        // StudentService.SearchAsync queries StudentProfile and maps to StudentSummaryDto.
        // Must use ConstructUsing because StudentSummaryDto is a positional record
        // (no parameterless constructor), so ForMember + property-setter approach won't work.
        CreateMap<StudentProfile, StudentSummaryDto>()
            .ConstructUsing((s, _) => new StudentSummaryDto(
                UserId:      s.UserId,
                StudentCode: s.StudentCode,
                FullName:    s.User != null ? s.User.FullName    : string.Empty,
                PhoneNumber: s.User?.PhoneNumber,
                AvatarUrl:   s.User?.AvatarUrl,
                ClassName:   null,
                GradeName:   null,
                IsActive:    s.User != null && s.User.IsActive
            ))
            .ForAllMembers(o => o.Ignore()); // all members set via ConstructUsing

        CreateMap<ParentStudentRelation, ParentInfoDto>()
            .ForMember(d => d.FullName,    o => o.MapFrom(s => s.Parent.FullName))
            .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.Parent.PhoneNumber));

        // ── Class ────────────────────────────────────────────────────────────
        CreateMap<Class, ClassSummaryDto>()
            .ForMember(d => d.ClassId,             o => o.MapFrom(s => s.Id))
            .ForMember(d => d.GradeName,           o => o.MapFrom(s => s.GradeLevel.GradeName))
            .ForMember(d => d.AcademicYearName,    o => o.MapFrom(s => s.AcademicYear.YearName))
            .ForMember(d => d.HomeRoomTeacherName, o => o.MapFrom(s => s.HomeRoomTeacher != null ? s.HomeRoomTeacher.FullName : null))
            .ForMember(d => d.EnrolledCount,       o => o.MapFrom(s => s.Enrollments.Count(e => e.Status == Domain.Enums.EnrollmentStatus.Active)));

        CreateMap<SubjectAssignment, SubjectAssignmentDto>()
            .ForMember(d => d.AssignmentId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.SubjectName,  o => o.MapFrom(s => s.Subject.SubjectName))
            .ForMember(d => d.SubjectCode,  o => o.MapFrom(s => s.Subject.SubjectCode))
            .ForMember(d => d.TeacherName,  o => o.MapFrom(s => s.Teacher.FullName));

        CreateMap<Schedule, ScheduleSlotDto>()
            .ForMember(d => d.ScheduleId,  o => o.MapFrom(s => s.Id))
            .ForMember(d => d.SubjectName, o => o.MapFrom(s => s.SubjectAssignment.Subject.SubjectName))
            .ForMember(d => d.TeacherName, o => o.MapFrom(s => s.SubjectAssignment.Teacher.FullName))
            .ForMember(d => d.DayName,     o => o.MapFrom(s => GetDayName(s.DayOfWeek)))
            .ForMember(d => d.StartTime,   o => o.MapFrom(s => s.StartTime.ToString("HH:mm")))
            .ForMember(d => d.EndTime,     o => o.MapFrom(s => s.EndTime.ToString("HH:mm")));

        CreateMap<User, TeacherSummaryDto>()
            .ForMember(d => d.UserId,                o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TeacherCode,           o => o.MapFrom(s => s.TeacherProfile != null ? s.TeacherProfile.TeacherCode : ""))
            .ForMember(d => d.SpecializationSubject, o => o.MapFrom(s => s.TeacherProfile != null ? s.TeacherProfile.SpecializationSubject : null))
            .ForMember(d => d.Qualification,         o => o.MapFrom(s => s.TeacherProfile != null ? s.TeacherProfile.Qualification : null))
            .ForMember(d => d.ContractType,          o => o.MapFrom(s => s.TeacherProfile != null && s.TeacherProfile.ContractType.HasValue ? s.TeacherProfile.ContractType.Value.ToString() : null));

        // ── Attendance ────────────────────────────────────────────────────────
        CreateMap<AttendanceSession, AttendanceSessionDto>()
            .ForMember(d => d.SessionId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.ClassName,    o => o.MapFrom(s => s.Schedule.SubjectAssignment.Class.ClassName))
            .ForMember(d => d.SubjectName,  o => o.MapFrom(s => s.Schedule.SubjectAssignment.Subject.SubjectName))
            .ForMember(d => d.SessionStatus,o => o.MapFrom(s => s.SessionStatus.ToString()))
            .ForMember(d => d.IsQRExpired,  o => o.MapFrom(s => DateTime.UtcNow > s.QRExpiresAt))
            .ForMember(d => d.TotalStudents,o => o.MapFrom(s => s.Attendances.Count))
            .ForMember(d => d.PresentCount, o => o.MapFrom(s => s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Present)))
            .ForMember(d => d.AbsentCount,  o => o.MapFrom(s => s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Absent)))
            .ForMember(d => d.LateCount,    o => o.MapFrom(s => s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Late)))
            .ForMember(d => d.ExcusedCount, o => o.MapFrom(s => s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Excused)));

        CreateMap<StudentAttendance, AttendanceRecordDto>()
            .ForMember(d => d.AttendanceId, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StudentCode,  o => o.MapFrom(s => s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : ""))
            .ForMember(d => d.StudentName,  o => o.MapFrom(s => s.Student.FullName))
            .ForMember(d => d.Status,       o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.CheckMethod,  o => o.MapFrom(s => s.CheckMethod.HasValue ? s.CheckMethod.Value.ToString() : null));

        // ── Grading ───────────────────────────────────────────────────────────
        CreateMap<ScoreEntry, ScoreEntryDto>()
            .ForMember(d => d.ScoreEntryId,  o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StudentCode,   o => o.MapFrom(s => s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : ""))
            .ForMember(d => d.StudentName,   o => o.MapFrom(s => s.Student.FullName))
            .ForMember(d => d.CategoryCode,  o => o.MapFrom(s => s.GradeCategory.CategoryCode))
            .ForMember(d => d.CategoryName,  o => o.MapFrom(s => s.GradeCategory.CategoryName))
            .ForMember(d => d.Coefficient,   o => o.MapFrom(s => s.GradeCategory.Coefficient));

        CreateMap<Exam, ExamDto>()
            .ForMember(d => d.ExamId,          o => o.MapFrom(s => s.Id))
            .ForMember(d => d.SubjectName,     o => o.MapFrom(s => s.Subject.SubjectName))
            .ForMember(d => d.GradeName,       o => o.MapFrom(s => s.GradeLevel.GradeName))
            .ForMember(d => d.SemesterName,    o => o.MapFrom(s => s.Semester.SemesterName))
            .ForMember(d => d.StartTime,       o => o.MapFrom(s => s.StartTime.ToString("HH:mm")))
            .ForMember(d => d.RoomCount,       o => o.MapFrom(s => s.ExamRooms.Count))
            .ForMember(d => d.TotalStudents,   o => o.MapFrom(s => s.ExamRooms.Sum(r => r.SeatAssignments.Count)));

        // ── Finance ───────────────────────────────────────────────────────────
        CreateMap<TuitionFeeConfig, TuitionFeeConfigDto>()
            .ForMember(d => d.ConfigId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.GradeName,   o => o.MapFrom(s => s.GradeLevel != null ? s.GradeLevel.GradeName : null))
            .ForMember(d => d.BillingCycle,o => o.MapFrom(s => s.BillingCycle.ToString()));

        CreateMap<TuitionInvoice, InvoiceDto>()
            .ForMember(d => d.InvoiceId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StudentName,  o => o.MapFrom(s => s.Student.FullName))
            .ForMember(d => d.StudentCode,  o => o.MapFrom(s => s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : ""))
            .ForMember(d => d.ClassName,    o => o.Ignore())
            .ForMember(d => d.FeeType,      o => o.MapFrom(s => s.Config.FeeType))
            .ForMember(d => d.FinalAmount,  o => o.MapFrom(s => s.FinalAmount))
            .ForMember(d => d.IsOverdue,    o => o.MapFrom(s => s.Status == Domain.Enums.InvoiceStatus.Pending && s.DueDate < DateOnly.FromDateTime(DateTime.UtcNow)))
            .ForMember(d => d.Status,       o => o.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.Payments,     o => o.MapFrom(s => s.Payments));

        CreateMap<TuitionPayment, PaymentSummaryDto>()
            .ForMember(d => d.PaymentId,     o => o.MapFrom(s => s.Id))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()));

        // ── HR ────────────────────────────────────────────────────────────────
        CreateMap<Payroll, PayrollDto>()
            .ForMember(d => d.PayrollId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TeacherName,  o => o.MapFrom(s => s.User.FullName))
            .ForMember(d => d.GrossIncome,  o => o.MapFrom(s => s.GrossIncome))
            .ForMember(d => d.NetSalary,    o => o.MapFrom(s => s.NetSalary))
            .ForMember(d => d.Status,       o => o.MapFrom(s => s.Status.ToString()));

        // ── Discipline ────────────────────────────────────────────────────────
        CreateMap<DisciplineRecord, DisciplineRecordDto>()
            .ForMember(d => d.RecordId,      o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StudentCode,   o => o.MapFrom(s => s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : ""))
            .ForMember(d => d.StudentName,   o => o.MapFrom(s => s.Student.FullName))
            .ForMember(d => d.ClassName,     o => o.Ignore())
            .ForMember(d => d.ReportedByName,o => o.MapFrom(s => s.ReportedBy.FullName))
            .ForMember(d => d.Severity,      o => o.MapFrom(s => s.Severity.ToString()))
            .ForMember(d => d.Status,        o => o.MapFrom(s => s.Status.ToString()));

        // ── Communication ─────────────────────────────────────────────────────
        CreateMap<Notification, NotificationDto>()
            .ForMember(d => d.NotificationId,  o => o.MapFrom(s => s.Id))
            .ForMember(d => d.NotificationType,o => o.MapFrom(s => s.NotificationType.ToString()))
            .ForMember(d => d.Channel,         o => o.MapFrom(s => s.Channel.ToString()))
            .ForMember(d => d.SentByName,      o => o.MapFrom(s => s.SentBy != null ? s.SentBy.FullName : "System"))
            .ForMember(d => d.IsRead,          o => o.Ignore());  // Set separately per user

        CreateMap<Message, MessageDto>()
            .ForMember(d => d.MessageId,    o => o.MapFrom(s => s.Id))
            .ForMember(d => d.SenderName,   o => o.MapFrom(s => s.Sender.FullName))
            .ForMember(d => d.SenderAvatar, o => o.MapFrom(s => s.Sender.AvatarUrl))
            .ForMember(d => d.IsMine,       o => o.Ignore()); // Set per-request

        CreateMap<NewsBoard, NewsBoardDto>()
            .ForMember(d => d.NewsId,          o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Scope,           o => o.MapFrom(s => s.Scope.ToString()))
            .ForMember(d => d.TargetClassName, o => o.MapFrom(s => s.TargetClass != null ? s.TargetClass.ClassName : null))
            .ForMember(d => d.PublishedByName, o => o.MapFrom(s => s.PublishedBy.FullName));
    }

    private static string GetDayName(byte day) => day switch
    {
        2 => "Thứ Hai", 3 => "Thứ Ba", 4 => "Thứ Tư",
        5 => "Thứ Năm", 6 => "Thứ Sáu", 7 => "Thứ Bảy",
        _ => "Không xác định"
    };
}
