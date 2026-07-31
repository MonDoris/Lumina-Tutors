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

        // StudentService.GetByIdAsync/CreateAsync/UpdateAsync map StudentProfile → StudentDetailDto.
        // Cũng là positional record ⇒ dùng ConstructUsing. Danh sách Parents được service ghi đè
        // sau khi map (qua "with { Parents = ... }"), nên ở đây khởi tạo rỗng.
        CreateMap<StudentProfile, StudentDetailDto>()
            .ConstructUsing((s, _) => new StudentDetailDto(
                UserId:              s.UserId,
                StudentCode:         s.StudentCode,
                FullName:            s.User != null ? s.User.FullName : string.Empty,
                Email:               s.User != null ? s.User.Email    : string.Empty,
                PhoneNumber:         s.User != null ? s.User.PhoneNumber : null,
                AvatarUrl:           s.User != null ? s.User.AvatarUrl   : null,
                DateOfBirth:         s.DateOfBirth,
                Gender:              s.Gender != null ? s.Gender.ToString() : null,
                PlaceOfBirth:        s.PlaceOfBirth,
                PermanentAddress:    s.PermanentAddress,
                EthnicGroup:         s.EthnicGroup,
                AdmissionDate:       s.AdmissionDate,
                AdmissionType:       s.AdmissionType != null ? s.AdmissionType.ToString() : null,
                CurrentClassId:      null,
                CurrentClassName:    null,
                CurrentGradeName:    null,
                HomeRoomTeacherName: null,
                Parents:             new List<ParentInfoDto>(),
                IsActive:            s.User != null && s.User.IsActive
            ))
            .ForAllMembers(o => o.Ignore());

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

        CreateMap<TeacherProfile, TeacherDetailDto>()
            .ForMember(d => d.ProfileId,             o => o.MapFrom(s => s.Id))
            .ForMember(d => d.UserId,                o => o.MapFrom(s => s.User.Id))
            .ForMember(d => d.FullName,              o => o.MapFrom(s => s.User.FullName))
            .ForMember(d => d.Email,                 o => o.MapFrom(s => s.User.Email))
            .ForMember(d => d.PhoneNumber,           o => o.MapFrom(s => s.User.PhoneNumber))
            .ForMember(d => d.AvatarUrl,             o => o.MapFrom(s => s.User.AvatarUrl))
            .ForMember(d => d.IsActive,              o => o.MapFrom(s => s.User.IsActive))
            .ForMember(d => d.Gender,                o => o.MapFrom(s => s.Gender.HasValue ? s.Gender.Value.ToString() : null))
            .ForMember(d => d.ContractType,          o => o.MapFrom(s => s.ContractType.HasValue ? s.ContractType.Value.ToString() : null))
            .ForMember(d => d.AssignedSubjects,      o => o.Ignore())
            .ForMember(d => d.AssignedClasses,       o => o.Ignore());

        CreateMap<User, TeacherSummaryDto>()
            .ForMember(d => d.UserId,                o => o.MapFrom(s => s.Id))
            .ForMember(d => d.TeacherCode,           o => o.MapFrom(s => s.TeacherProfile != null ? s.TeacherProfile.TeacherCode : ""))
            .ForMember(d => d.SpecializationSubject, o => o.MapFrom(s => s.TeacherProfile != null ? s.TeacherProfile.SpecializationSubject : null))
            .ForMember(d => d.Qualification,         o => o.MapFrom(s => s.TeacherProfile != null ? s.TeacherProfile.Qualification : null))
            .ForMember(d => d.ContractType,          o => o.MapFrom(s => s.TeacherProfile != null && s.TeacherProfile.ContractType.HasValue ? s.TeacherProfile.ContractType.Value.ToString() : null));

        // ── Attendance ────────────────────────────────────────────────────────
        // AttendanceSessionDto/AttendanceRecordDto là positional record ⇒ phải dùng ConstructUsing
        // (ForMember không gán được vào tham số constructor của record — xem chú thích ở StudentSummaryDto).
        CreateMap<AttendanceSession, AttendanceSessionDto>()
            .ConstructUsing((s, _) => new AttendanceSessionDto(
                SessionId:     s.Id,
                ScheduleId:    s.ScheduleId,
                ClassName:     s.Schedule != null && s.Schedule.SubjectAssignment != null && s.Schedule.SubjectAssignment.Class != null ? s.Schedule.SubjectAssignment.Class.ClassName : string.Empty,
                SubjectName:   s.Schedule != null && s.Schedule.SubjectAssignment != null && s.Schedule.SubjectAssignment.Subject != null ? s.Schedule.SubjectAssignment.Subject.SubjectName : string.Empty,
                SessionDate:   s.SessionDate,
                SessionStatus: s.SessionStatus.ToString(),
                QRToken:       s.QRToken,
                QRExpiresAt:   s.QRExpiresAt,
                IsQRExpired:   DateTime.UtcNow > s.QRExpiresAt,
                TopicNote:     s.TopicNote,
                TotalStudents: s.Attendances != null ? s.Attendances.Count : 0,
                PresentCount:  s.Attendances != null ? s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Present) : 0,
                AbsentCount:   s.Attendances != null ? s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Absent) : 0,
                LateCount:     s.Attendances != null ? s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Late) : 0,
                ExcusedCount:  s.Attendances != null ? s.Attendances.Count(a => a.Status == Domain.Enums.AttendanceStatus.Excused) : 0,
                CreatedAt:     s.CreatedAt,
                Records:       null
            ))
            .ForAllMembers(o => o.Ignore());

        CreateMap<StudentAttendance, AttendanceRecordDto>()
            .ConstructUsing((s, _) => new AttendanceRecordDto(
                AttendanceId:   s.Id,
                StudentId:      s.StudentId,
                StudentCode:    s.Student != null && s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : string.Empty,
                StudentName:    s.Student != null ? s.Student.FullName : string.Empty,
                Status:         s.Status.ToString(),
                CheckedInAt:    s.CheckedInAt,
                CheckMethod:    s.CheckMethod.HasValue ? s.CheckMethod.Value.ToString() : null,
                NotifiedParent: s.NotifiedParent,
                Note:           s.Note
            ))
            .ForAllMembers(o => o.Ignore());

        // ── Grading ───────────────────────────────────────────────────────────
        CreateMap<ScoreEntry, ScoreEntryDto>()
            .ForMember(d => d.ScoreEntryId,  o => o.MapFrom(s => s.Id))
            .ForMember(d => d.StudentCode,   o => o.MapFrom(s => s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : ""))
            .ForMember(d => d.StudentName,   o => o.MapFrom(s => s.Student.FullName))
            .ForMember(d => d.CategoryCode,  o => o.MapFrom(s => s.GradeCategory.CategoryCode))
            .ForMember(d => d.CategoryName,  o => o.MapFrom(s => s.GradeCategory.CategoryName))
            .ForMember(d => d.Coefficient,   o => o.MapFrom(s => s.GradeCategory.Coefficient));

        // ExamDto là positional record ⇒ dùng ConstructUsing (xem chú thích ở StudentSummaryDto).
        CreateMap<Exam, ExamDto>()
            .ConstructUsing((s, _) => new ExamDto(
                ExamId:          s.Id,
                ExamName:        s.ExamName,
                ExamType:        s.ExamType.ToString(),
                SubjectName:     s.Subject != null ? s.Subject.SubjectName : string.Empty,
                GradeName:       s.GradeLevel != null ? s.GradeLevel.GradeName : string.Empty,
                SemesterName:    s.Semester != null ? s.Semester.SemesterName : string.Empty,
                ExamDate:        s.ExamDate,
                StartTime:       s.StartTime.ToString("HH:mm"),
                DurationMinutes: s.DurationMinutes,
                MaxScore:        s.MaxScore,
                RoomCount:       s.ExamRooms != null ? s.ExamRooms.Count : 0,
                TotalStudents:   s.ExamRooms != null ? s.ExamRooms.Sum(r => r.SeatAssignments != null ? r.SeatAssignments.Count : 0) : 0
            ))
            .ForAllMembers(o => o.Ignore());

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

        CreateMap<TuitionInvoice, InvoiceSummaryDto>()
            .ForMember(d => d.InvoiceId,     o => o.MapFrom(s => s.Id))
            .ForMember(d => d.FeeType,       o => o.MapFrom(s => s.Config.FeeType))
            .ForMember(d => d.FinalAmount,   o => o.MapFrom(s => s.FinalAmount))
            .ForMember(d => d.Status,        o => o.MapFrom(s => s.Status.ToString()));

        CreateMap<TuitionPayment, PaymentSummaryDto>()
            .ForMember(d => d.PaymentId,     o => o.MapFrom(s => s.Id))
            .ForMember(d => d.PaymentMethod, o => o.MapFrom(s => s.PaymentMethod.ToString()))
            .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.PaymentStatus.ToString()));

        // ── HR ────────────────────────────────────────────────────────────────
        // PayrollDto là positional record ⇒ dùng ConstructUsing (xem chú thích ở StudentSummaryDto).
        CreateMap<Payroll, PayrollDto>()
            .ConstructUsing((s, _) => new PayrollDto(
                PayrollId:          s.Id,
                TeacherName:        s.User != null ? s.User.FullName : string.Empty,
                Month:              s.PayrollMonth,
                Year:               s.PayrollYear,
                BaseSalary:         s.BaseSalary,
                TeachingAllowance:  s.TeachingAllowance,
                PositionAllowance:  s.PositionAllowance,
                OvertimePay:        s.OvertimePay,
                Bonus:              s.Bonus,
                GrossIncome:        s.GrossIncome,
                InsuranceDeduction: s.InsuranceDeduction,
                TaxDeduction:       s.TaxDeduction,
                OtherDeductions:    s.OtherDeductions,
                NetSalary:          s.NetSalary,
                Status:             s.Status.ToString()
            ))
            .ForAllMembers(o => o.Ignore());

        // ── Discipline ────────────────────────────────────────────────────────
        // DisciplineRecordDto là positional record ⇒ dùng ConstructUsing (xem chú thích ở StudentSummaryDto).
        CreateMap<DisciplineRecord, DisciplineRecordDto>()
            .ConstructUsing((s, _) => new DisciplineRecordDto(
                RecordId:       s.Id,
                StudentId:      s.StudentId,
                StudentCode:    s.Student != null && s.Student.StudentProfile != null ? s.Student.StudentProfile.StudentCode : string.Empty,
                StudentName:    s.Student != null ? s.Student.FullName : string.Empty,
                ClassName:      string.Empty,   // được service điền riêng nếu cần
                ReportedByName: s.ReportedBy != null ? s.ReportedBy.FullName : string.Empty,
                RecordDate:     s.RecordDate,
                ViolationType:  s.ViolationType,
                Severity:       s.Severity.ToString(),
                Description:    s.Description,
                ActionTaken:    s.ActionTaken,
                Status:         s.Status.ToString(),
                CreatedAt:      s.CreatedAt
            ))
            .ForAllMembers(o => o.Ignore());

        // ── Communication ─────────────────────────────────────────────────────
        // NotificationDto là positional record ⇒ dùng ConstructUsing (xem chú thích ở StudentSummaryDto).
        // IsRead được service ghi đè theo từng người nhận qua "with { IsRead = ... }".
        CreateMap<Notification, NotificationDto>()
            .ConstructUsing((s, _) => new NotificationDto(
                NotificationId:   s.Id,
                Title:            s.Title,
                Body:             s.Body,
                NotificationType: s.NotificationType.ToString(),
                Channel:          s.Channel.ToString(),
                SentByName:       s.SentBy != null ? s.SentBy.FullName : "System",
                CreatedAt:        s.CreatedAt,
                IsRead:           false
            ))
            .ForAllMembers(o => o.Ignore());

        CreateMap<Message, MessageDto>()
            .ConstructUsing((s, _) => new MessageDto(
                MessageId:      s.Id,
                SenderId:       s.SenderId,
                SenderName:     s.Sender?.FullName ?? "—",
                SenderAvatar:   s.Sender?.AvatarUrl,
                MessageText:    s.IsDeleted ? null : s.MessageText,
                AttachmentUrl:  s.AttachmentUrl,
                AttachmentType: s.AttachmentType,
                IsDeleted:      s.IsDeleted,
                SentAt:         s.SentAt,
                IsMine:         false   // overridden in service via `with { IsMine = ... }`
            ))
            .ForAllMembers(o => o.Ignore());

        // Conversation → ConversationDto — OtherPartyName and LastMessage resolved at service layer
        // via ConstructUsing so AutoMapper doesn't need to infer complex navigation paths.
        CreateMap<Conversation, ConversationDto>()
            .ConstructUsing((s, _) => new ConversationDto(
                ConversationId:    s.Id,
                ConversationType:  s.ConversationType.ToString(),
                ConversationName:  null,
                OtherPartyName:    s.Participants.FirstOrDefault()?.User?.FullName ?? "—",
                OtherPartyAvatar:  s.Participants.FirstOrDefault()?.User?.AvatarUrl,
                LastMessage:       s.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.MessageText,
                LastMessageAt:     s.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault()?.SentAt,
                UnreadCount:       0
            ))
            .ForAllMembers(o => o.Ignore());

        // NewsBoardDto là positional record ⇒ dùng ConstructUsing (xem chú thích ở StudentSummaryDto).
        CreateMap<NewsBoard, NewsBoardDto>()
            .ConstructUsing((s, _) => new NewsBoardDto(
                NewsId:          s.Id,
                Title:           s.Title,
                ContentHtml:     s.ContentHtml,
                CoverImageUrl:   s.CoverImageUrl,
                Scope:           s.Scope.ToString(),
                TargetClassName: s.TargetClass != null ? s.TargetClass.ClassName : null,
                IsPinned:        s.IsPinned,
                IsPublished:     s.IsPublished,
                PublishedByName: s.PublishedBy != null ? s.PublishedBy.FullName : string.Empty,
                PublishedAt:     s.PublishedAt,
                CreatedAt:       s.CreatedAt
            ))
            .ForAllMembers(o => o.Ignore());
    }

    private static string GetDayName(byte day) => day switch
    {
        2 => "Thứ Hai", 3 => "Thứ Ba", 4 => "Thứ Tư",
        5 => "Thứ Năm", 6 => "Thứ Sáu", 7 => "Thứ Bảy",
        _ => "Không xác định"
    };
}
