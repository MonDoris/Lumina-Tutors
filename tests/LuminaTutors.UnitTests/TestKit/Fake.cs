using Identity = LuminaTutors.Domain.Entities.Identity;
using Academic = LuminaTutors.Domain.Entities.Academic;
using Profiles = LuminaTutors.Domain.Entities.Profiles;
using Att = LuminaTutors.Domain.Entities.Attendance;
using Finance = LuminaTutors.Domain.Entities.Finance;
using Sub = LuminaTutors.Domain.Entities.Subscription;

namespace LuminaTutors.UnitTests.TestKit;

/// <summary>
/// "Xưởng" tạo entity mẫu cho test — mỗi hàm trả về một entity đã điền sẵn dữ liệu
/// hợp lệ, và cho phép ghi đè các trường cần thiết qua tham số tùy chọn.
///
/// Mục tiêu: phần "Arrange" của test chỉ khai báo đúng những gì QUAN TRỌNG với test đó,
/// mọi thứ còn lại dùng mặc định — nhờ vậy test ngắn, rõ ý và dễ sửa.
///
/// Quy ước: dùng alias namespace (Identity./Sub.) để tên hàm trùng tên entity mà
/// không xung đột (ví dụ hàm <c>Role()</c> trả về kiểu <c>Identity.Role</c>).
/// </summary>
internal static class Fake
{
    // ─── Identity ─────────────────────────────────────────────────────────────

    public static Identity.Role Role(int id = 2, string code = "TEACHER", string name = "Giáo viên")
        => new() { Id = id, RoleCode = code, RoleName = name };

    public static Identity.School School(int id = 1, string name = "Trường THPT Đông Sơn")
        => new() { Id = id, SchoolCode = "DS01", SchoolName = name, IsActive = true };

    public static Identity.User User(
        int id = 1,
        int schoolId = 1,
        string email = "teacher@ds.edu.vn",
        string fullName = "Nguyễn Văn A",
        string passwordHash = "HASHED_PASSWORD",
        bool isActive = true,
        string? phone = null,
        Identity.Role? role = null,
        Identity.School? school = null)
    {
        role ??= Role();
        school ??= School(id: schoolId);
        return new Identity.User
        {
            Id           = id,
            SchoolId     = schoolId,
            RoleId       = role.Id,
            Email        = email,
            FullName     = fullName,
            PasswordHash = passwordHash,
            IsActive     = isActive,
            PhoneNumber  = phone,
            Role         = role,
            School       = school
        };
    }

    public static Identity.InviteLink Invite(
        int id = 10,
        int schoolId = 1,
        Guid? token = null,
        int targetRoleId = 2,
        string? targetEmail = "moi@ds.edu.vn",
        bool isRevoked = false,
        DateTime? expiresAt = null,
        DateTime? usedAt = null,
        Identity.Role? targetRole = null,
        Identity.School? school = null)
        => new()
        {
            Id              = id,
            SchoolId        = schoolId,
            Token           = token ?? Guid.NewGuid(),
            TargetRoleId    = targetRoleId,
            TargetEmail     = targetEmail,
            IsRevoked       = isRevoked,
            ExpiresAt       = expiresAt ?? DateTime.UtcNow.AddDays(3),
            UsedAt          = usedAt,
            TargetRole      = targetRole ?? Role(id: targetRoleId),
            School          = school ?? School(id: schoolId),
            CreatedByUserId = 1
        };

    // ─── Academic ─────────────────────────────────────────────────────────────

    public static Academic.Class Class(
        int id = 1,
        int schoolId = 1,
        string name = "10A1",
        bool isActive = true,
        int? homeRoomTeacherId = null,
        byte maxStudents = 40)
        => new()
        {
            Id                = id,
            SchoolId          = schoolId,
            ClassName         = name,
            IsActive          = isActive,
            HomeRoomTeacherId = homeRoomTeacherId,
            MaxStudents       = maxStudents,
            AcademicYearId    = 1,
            GradeLevelId      = 1
        };

    public static Academic.Subject Subject(
        int id = 1, int schoolId = 1, string name = "Toán", string code = "TOAN")
        => new() { Id = id, SchoolId = schoolId, SubjectName = name, SubjectCode = code };

    public static Academic.ClassEnrollment Enrollment(
        int id = 1,
        int classId = 1,
        int studentId = 100,
        EnrollmentStatus status = EnrollmentStatus.Active,
        Academic.Class? cls = null,
        Identity.User? student = null)
        => new()
        {
            Id        = id,
            ClassId   = classId,
            StudentId = studentId,
            Status    = status,
            Class     = cls ?? Class(id: classId),
            Student   = student ?? User(id: studentId, role: Role(id: 3, code: "STUDENT", name: "Học sinh"))
        };

    // ─── Profiles ─────────────────────────────────────────────────────────────

    public static Profiles.StudentProfile StudentProfile(
        int userId = 100, int schoolId = 1, string code = "HS0001", Identity.User? user = null)
        => new()
        {
            UserId      = userId,
            SchoolId    = schoolId,
            StudentCode = code,
            User        = user ?? User(id: userId, fullName: "Học Sinh A",
                                       role: Role(id: 3, code: "STUDENT", name: "Học sinh"))
        };

    public static Profiles.TeacherProfile TeacherProfile(
        int userId = 50, int schoolId = 1, string code = "GV0001", int? primarySubjectId = null)
        => new()
        {
            UserId           = userId,
            SchoolId         = schoolId,
            TeacherCode      = code,
            PrimarySubjectId = primarySubjectId,
            User             = User(id: userId, role: Role(id: 2, code: "TEACHER"))
        };

    // ─── Attendance ───────────────────────────────────────────────────────────

    public static Att.AttendanceSession Session(
        int id = 1,
        int schoolId = 1,
        int scheduleId = 1,
        int createdByTeacherId = 50,
        SessionStatus status = SessionStatus.Open,
        DateOnly? sessionDate = null,
        DateTime? qrExpiresAt = null,
        Guid? qrToken = null)
        => new()
        {
            Id                 = id,
            SchoolId           = schoolId,
            ScheduleId         = scheduleId,
            CreatedByTeacherId = createdByTeacherId,
            SessionStatus      = status,
            SessionDate        = sessionDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            QRToken            = qrToken ?? Guid.NewGuid(),
            QRExpiresAt        = qrExpiresAt ?? DateTime.UtcNow.AddMinutes(10)
        };

    public static Att.StudentAttendance Attendance(
        int id = 1,
        int sessionId = 1,
        int studentId = 100,
        AttendanceStatus status = AttendanceStatus.Absent,
        bool notifiedParent = false,
        Identity.User? student = null)
        => new()
        {
            Id             = id,
            SessionId      = sessionId,
            StudentId      = studentId,
            Status         = status,
            NotifiedParent = notifiedParent,
            Student        = student ?? User(id: studentId, fullName: "Học Sinh A",
                                             role: Role(id: 3, code: "STUDENT", name: "Học sinh"))
        };

    // ─── Finance ──────────────────────────────────────────────────────────────

    public static Finance.TuitionFeeConfig FeeConfig(
        int id = 1, int schoolId = 1, string feeType = "Học phí", decimal amount = 1_000_000)
        => new() { Id = id, SchoolId = schoolId, FeeType = feeType, Amount = amount, IsActive = true, AcademicYearId = 1 };

    public static Finance.TuitionInvoice Invoice(
        int id = 1,
        int schoolId = 1,
        int studentId = 100,
        decimal amount = 1_000_000,
        decimal discount = 0,
        InvoiceStatus status = InvoiceStatus.Pending,
        string billingPeriod = "2026-01",
        Finance.TuitionFeeConfig? config = null,
        Identity.User? student = null)
        => new()
        {
            Id            = id,
            SchoolId      = schoolId,
            StudentId     = studentId,
            ConfigId      = (config ?? FeeConfig()).Id,
            Amount        = amount,
            Discount      = discount,
            Status        = status,
            BillingPeriod = billingPeriod,
            InvoiceCode   = "INV001-202601-0001",
            DueDate       = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            Config        = config ?? FeeConfig(),
            Student       = student ?? User(id: studentId, fullName: "Học Sinh A")
        };

    public static Finance.TuitionPayment Payment(
        int id = 1, int invoiceId = 1, decimal amountPaid = 1_000_000,
        PaymentStatus status = PaymentStatus.Success)
        => new() { Id = id, InvoiceId = invoiceId, AmountPaid = amountPaid, PaymentStatus = status };

    // ─── Subscription / Quota ─────────────────────────────────────────────────

    public static Sub.SubscriptionPlan Plan(
        int id = 1,
        int maxTeachers = -1,
        int maxStudents = -1,
        int maxParents = -1,
        int maxAdmins = -1,
        int maxAccountants = -1,
        int maxSupervisors = -1,
        int maxClasses = -1)
        => new()
        {
            Id             = id,
            PlanCode       = "PREMIUM",
            Name           = "Gói Premium",
            MaxTeachers    = maxTeachers,
            MaxStudents    = maxStudents,
            MaxParents     = maxParents,
            MaxAdmins      = maxAdmins,
            MaxAccountants = maxAccountants,
            MaxSupervisors = maxSupervisors,
            MaxClasses     = maxClasses
        };

    /// <summary>
    /// Đăng ký đang hoạt động của một trường. Mặc định: Active, còn hạn.
    /// Truyền <paramref name="plan"/> để quy định quota; <paramref name="quotaAddOns"/> để cộng thêm quota.
    /// </summary>
    public static Sub.SchoolSubscription Subscription(
        int id = 1,
        int schoolId = 1,
        SubscriptionStatus status = SubscriptionStatus.Active,
        DateOnly? currentPeriodEnd = null,
        Sub.SubscriptionPlan? plan = null,
        params Sub.SchoolRoleQuotaAddOn[] quotaAddOns)
        => new()
        {
            Id               = id,
            SchoolId         = schoolId,
            PlanId           = (plan ?? Plan()).Id,
            Status           = status,
            StartDate        = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1)),
            CurrentPeriodEnd = currentPeriodEnd ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            Plan             = plan ?? Plan(),
            RoleQuotaAddOns  = quotaAddOns.ToList()
        };

    /// <summary>Add-on quota đang gắn vào đăng ký của trường (cộng thêm slot tài khoản/lớp).</summary>
    public static Sub.SchoolRoleQuotaAddOn QuotaAddOn(
        RoleCode? targetRole = null,
        int extraQuota = 0,
        int extraClasses = 0,
        bool isActive = true,
        DateOnly? activeUntil = null)
        => new()
        {
            IsActive    = isActive,
            ActiveUntil = activeUntil ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            AddOn = new Sub.RoleQuotaAddOn
            {
                TargetRole   = targetRole,
                ExtraQuota   = extraQuota,
                ExtraClasses = extraClasses
            }
        };
}
