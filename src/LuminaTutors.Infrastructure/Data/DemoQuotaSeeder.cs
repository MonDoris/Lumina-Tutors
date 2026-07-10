using LuminaTutors.Application.Common;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Profiles;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Infrastructure.Data;

/// <summary>
/// Seeder demo: lấp đầy tài khoản của TỪNG vai trò tới đúng hạn mức <b>Gói Cơ Bản (BASIC)</b>
/// cho trường <c>Demo</c>, để minh hoạ trạng thái quota FULL và luồng chặn khi vượt giới hạn
/// (xem <see cref="LuminaTutors.Application.Services.QuotaService"/>).
///
/// Hạn mức BASIC (lấy động từ bảng SubscriptionPlans, PlanCode = "BASIC"):
/// Giáo viên 20 · Học sinh 500 · Phụ huynh 500 · Quản trị trường 3 · Kế toán 2 · Giám thị 2.
///
/// Đặc tính:
/// • <b>Idempotent</b> — chỉ tạo phần còn thiếu (target − số tài khoản đang hoạt động của vai trò),
///   nên chạy lại nhiều lần đều an toàn, không vượt cap, không nhân bản.
/// • Mật khẩu mọi tài khoản demo = <see cref="DemoPassword"/>.
/// • Email tuân theo quy ước <see cref="RoleEmail"/> (tiền tố subdomain theo vai trò) để đăng nhập đúng.
/// • Mỗi tài khoản kèm hồ sơ (profile) tương ứng + mã định danh duy nhất theo trường
///   (HS/GV/GT/KT…), riêng ADMIN không có profile.
///
/// Lưu ý: ACCOUNTANT không nằm trong danh sách vai trò mà màn hình "Quản lý tài khoản" hiển thị
/// (AccountService.AllowedRoles), nhưng VẪN được QuotaService tính — nên seeder vẫn tạo đủ 2 kế toán
/// để bảng quota hiển thị FULL.
/// </summary>
public static class DemoQuotaSeeder
{
    /// <summary>Mật khẩu dùng chung cho mọi tài khoản demo được tạo bởi seeder này.</summary>
    public const string DemoPassword = "Demo@123";

    private const string DemoSchoolName = "Demo";

    // Các vai trò bị giới hạn quota trong gói (RoleCode đúng như lưu trong DB).
    private static readonly (RoleCode Role, string Code)[] QuotaRoles =
    {
        (RoleCode.Admin,      "ADMIN"),
        (RoleCode.Teacher,    "TEACHER"),
        (RoleCode.Student,    "STUDENT"),
        (RoleCode.Parent,     "PARENT"),
        (RoleCode.Supervisor, "SUPERVISOR"),
        (RoleCode.Accountant, "ACCOUNTANT"),
    };

    /// <summary>
    /// Lấp đầy quota của trường Demo tới hạn mức Gói Cơ Bản. Gọi sau <see cref="DatabaseSeeder.SeedAsync"/>.
    /// </summary>
    public static async Task SeedToBasicLimitAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<LuminaTutorsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<LuminaTutorsDbContext>>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        // 1. Trường Demo (ưu tiên theo tên; fallback trường đầu tiên cho môi trường dev tự đặt tên khác).
        var school = await db.Schools.FirstOrDefaultAsync(s => s.SchoolName == DemoSchoolName, ct)
                   ?? await db.Schools.OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        if (school is null)
        {
            logger.LogWarning("DemoQuotaSeeder: chưa có trường nào trong DB — bỏ qua.");
            return;
        }

        // 2. Gói Cơ Bản — nguồn hạn mức mục tiêu (đọc động phòng khi giá trị thay đổi).
        var basic = await db.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanCode == "BASIC", ct);
        if (basic is null)
        {
            logger.LogWarning("DemoQuotaSeeder: chưa seed gói BASIC — bỏ qua. (Chạy DatabaseSeeder trước.)");
            return;
        }

        // 3. Bảng tra Role theo RoleCode.
        var roleByCode = await db.Roles.ToDictionaryAsync(r => r.RoleCode, r => r, ct);

        // 4. Domain gốc của trường = domain email tài khoản Nhà trường (ADMIN). Dùng dựng email theo vai trò.
        var schoolAdmin = await db.Users
            .Where(u => u.SchoolId == school.Id && u.Role.RoleCode == "ADMIN")
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(ct);
        var baseDomain = RoleEmail.BaseDomain(schoolAdmin?.Email);

        // 5. Tập email đang dùng trong trường (chặn trùng UQ_Users_Email_School).
        var existingEmails = (await db.Users
                .Where(u => u.SchoolId == school.Id)
                .Select(u => u.Email)
                .ToListAsync(ct))
            .Select(e => e.ToLowerInvariant())
            .ToHashSet();

        // 6. Hash mật khẩu MỘT lần rồi tái sử dụng cho mọi tài khoản:
        //    PasswordHasher<T> mặc định của ASP.NET Core KHÔNG dùng tham số user khi băm,
        //    nên cùng một chuỗi hash (đã chứa salt riêng) vẫn xác thực đúng "Demo@123" cho mọi user.
        //    Tránh băm PBKDF2 hàng nghìn lần (mỗi lần ~chục ms) khi lấp đầy 500 HS + 500 PH.
        var sharedHash = hasher.HashPassword(new User(), DemoPassword);

        var today      = DateOnly.FromDateTime(DateTime.UtcNow);
        var stamp      = DateTime.UtcNow.ToString("yyMM");
        var phoneSeq   = 0;     // sinh số điện thoại demo không trùng (không bắt buộc unique nhưng cho gọn)
        var grandTotal = 0;

        logger.LogInformation(
            "🎯 DemoQuotaSeeder: trường '{School}' (Id={Id}) — lấp đầy theo Gói Cơ Bản (BASIC).",
            school.SchoolName, school.Id);

        foreach (var (role, code) in QuotaRoles)
        {
            var target = PlanMaxFor(basic, role);
            if (target < 0)
            {
                logger.LogInformation("   • {Role}: gói không giới hạn (-1) — bỏ qua.", code);
                continue;
            }

            var used = await db.Users.CountAsync(
                u => u.SchoolId == school.Id && u.IsActive && u.Role.RoleCode == code, ct);

            var toCreate = target - used;
            if (toCreate <= 0)
            {
                logger.LogInformation("   ✓ {Role}: đã đạt {Used}/{Target} — không tạo thêm.", code, used, target);
                continue;
            }

            var roleEntity = roleByCode[code];
            var stem       = LocalStemFor(role);

            // 6a. Tạo Users theo lô, đánh số tuần tự, bỏ qua email đã tồn tại (an toàn khi chạy lại).
            var newUsers = new List<User>(toCreate);
            var seq = 0;
            while (newUsers.Count < toCreate)
            {
                seq++;
                var email = RoleEmail.Build($"{stem}{seq:D3}", code, baseDomain);
                if (!existingEmails.Add(email)) continue;   // đã có → thử số kế tiếp

                var displayIdx = used + newUsers.Count + 1;
                newUsers.Add(new User
                {
                    SchoolId        = school.Id,
                    RoleId          = roleEntity.Id,
                    Email           = email,
                    FullName        = DemoFullName(displayIdx),
                    PhoneNumber     = $"09{10_000_000 + phoneSeq++:D8}",
                    IsActive        = true,
                    IsEmailVerified = true,
                    PasswordHash    = sharedHash
                });
            }

            db.Users.AddRange(newUsers);
            await db.SaveChangesAsync(ct);   // flush để populate Id cho từng user

            // 6b. Tạo hồ sơ (profile) tương ứng — mã định danh suy từ user.Id nên luôn duy nhất theo trường.
            foreach (var u in newUsers)
                AddProfile(db, school.Id, role, u, stamp, today, DemoBio(role, u.Id).Gender);

            await db.SaveChangesAsync(ct);

            grandTotal += newUsers.Count;
            logger.LogInformation(
                "   ➕ {Role}: tạo thêm {N} tài khoản → đủ {Target} (FULL).", code, newUsers.Count, target);
        }

        logger.LogInformation(
            "✅ DemoQuotaSeeder hoàn tất: tạo mới {Total} tài khoản cho trường '{School}'. " +
            "Mật khẩu chung mọi tài khoản: {Pwd}",
            grandTotal, school.SchoolName, DemoPassword);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static int PlanMaxFor(Domain.Entities.Subscription.SubscriptionPlan p, RoleCode role) => role switch
    {
        RoleCode.Teacher    => p.MaxTeachers,
        RoleCode.Student    => p.MaxStudents,
        RoleCode.Parent     => p.MaxParents,
        RoleCode.Admin      => p.MaxAdmins,
        RoleCode.Accountant => p.MaxAccountants,
        RoleCode.Supervisor => p.MaxSupervisors,
        _                   => -1
    };

    /// <summary>Tiền tố phần local-part của email theo vai trò (gv001, hs001…).</summary>
    private static string LocalStemFor(RoleCode role) => role switch
    {
        RoleCode.Teacher    => "gv",
        RoleCode.Student    => "hs",
        RoleCode.Parent     => "ph",
        RoleCode.Admin      => "qtv",
        RoleCode.Supervisor => "gt",
        RoleCode.Accountant => "kt",
        _                   => "user"
    };

    private static void AddProfile(
        LuminaTutorsDbContext db, int schoolId, RoleCode role, User u,
        string stamp, DateOnly today, Gender? gender)
    {
        switch (role)
        {
            case RoleCode.Student:
                db.StudentProfiles.Add(new StudentProfile
                {
                    UserId      = u.Id,
                    SchoolId    = schoolId,
                    StudentCode = $"HS{stamp}{u.Id:D4}",
                    DateOfBirth = DemoBio(role, u.Id).Dob,
                    Gender      = gender
                });
                break;

            case RoleCode.Teacher:
                db.TeacherProfiles.Add(new TeacherProfile
                {
                    UserId      = u.Id,
                    SchoolId    = schoolId,
                    TeacherCode = $"GV{stamp}{u.Id:D4}",
                    DateOfBirth = DemoBio(role, u.Id).Dob,
                    Gender      = gender,
                    HireDate    = today
                });
                break;

            case RoleCode.Supervisor:
                db.SupervisorProfiles.Add(new SupervisorProfile
                {
                    UserId         = u.Id,
                    SchoolId       = schoolId,
                    SupervisorCode = $"GT{stamp}{u.Id:D4}",
                    DateOfBirth    = DemoBio(role, u.Id).Dob,
                    Gender         = gender,
                    HireDate       = today
                });
                break;

            case RoleCode.Accountant:
                db.AccountantProfiles.Add(new AccountantProfile
                {
                    UserId         = u.Id,
                    SchoolId       = schoolId,
                    AccountantCode = $"KT{stamp}{u.Id:D4}",
                    DateOfBirth    = DemoBio(role, u.Id).Dob,
                    Gender         = gender,
                    HireDate       = today
                });
                break;

            case RoleCode.Parent:
                db.ParentProfiles.Add(new ParentProfile
                {
                    UserId     = u.Id,
                    SchoolId   = schoolId,
                    Occupation = "Phụ huynh (demo)"
                });
                break;

            // RoleCode.Admin: Nhà trường không có profile riêng.
        }
    }

    // ─── Dữ liệu demo (tất định theo index để chạy lại cho kết quả nhất quán) ───

    private static readonly string[] Surnames =
        { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương", "Lý" };
    private static readonly string[] MiddleNames =
        { "Văn", "Thị", "Hữu", "Đức", "Minh", "Ngọc", "Gia", "Quang", "Thanh", "Hoài", "Khánh", "Bảo" };
    private static readonly string[] GivenNames =
        { "An", "Bình", "Châu", "Dũng", "Hà", "Hải", "Hương", "Khoa", "Lan", "Linh", "Mai", "Nam", "Phúc", "Quân", "Sơn", "Trang", "Tú", "Vy", "Yến", "Đạt" };

    private static string DemoFullName(int i) =>
        $"{Surnames[(i * 7) % Surnames.Length]} {MiddleNames[(i * 3) % MiddleNames.Length]} {GivenNames[(i * 5) % GivenNames.Length]}";

    /// <summary>Giới tính + ngày sinh demo hợp lệ (ngày ≤ 27 để không vượt số ngày của tháng).</summary>
    private static (Gender Gender, DateOnly Dob) DemoBio(RoleCode role, int i)
    {
        var gender = (i % 2 == 0) ? Gender.Female : Gender.Male;
        var month  = (i % 12) + 1;
        var day    = (i % 27) + 1;
        var year   = role == RoleCode.Student ? 2008 - (i % 7)   // học sinh ~ 10–17 tuổi
                                              : 1985 - (i % 20);  // nhân sự ~ trưởng thành
        return (gender, new DateOnly(year, month, day));
    }
}
