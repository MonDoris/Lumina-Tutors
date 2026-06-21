using LuminaTutors.Domain.Entities.Academic;
using LuminaTutors.Domain.Entities.Identity;
using LuminaTutors.Domain.Entities.Subscription;
using LuminaTutors.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Infrastructure.Data;

/// <summary>
/// Applies pending EF migrations, cleans up leftover demo accounts,
/// and guarantees a default School + Admin user exist for first-run.
/// </summary>
public static class DatabaseSeeder
{
    // Non-admin demo emails created by the old seeder — safe to remove.
    private static readonly string[] DemoEmails =
    [
        "teacher@lumina.edu.vn",
        "student@lumina.edu.vn",
        "parent@lumina.edu.vn",
        "supervisor@lumina.edu.vn",
        "accountant@lumina.edu.vn",
    ];

    private const string AdminEmail    = "admin@lumina.edu.vn";   // → SYSADMIN (Quản trị hệ thống)
    private const string AdminPassword = "Admin@123";

    private const string SchoolAdminEmail    = "nhatruong@lumina.edu.vn";  // → ADMIN (Nhà trường)
    private const string SchoolAdminPassword = "Truong@123";

    private const string SchoolName = "Marie Curie High School";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope  = services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<LuminaTutorsDbContext>();
        var logger       = scope.ServiceProvider.GetRequiredService<ILogger<LuminaTutorsDbContext>>();
        var hasher       = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        // ── 1. Apply pending migrations (chờ LocalDB cold-start) ──────────────
        await MigrateWithRetryAsync(db, logger);

        // ── 2. Remove non-admin demo accounts from old seeder ─────────────────
        var demoUsers = await db.Users
            .Where(u => DemoEmails.Contains(u.Email))
            .ToListAsync();

        if (demoUsers.Count > 0)
        {
            var demoIds = demoUsers.Select(u => u.Id).ToList();

            var teacherProfiles    = await db.TeacherProfiles   .Where(p => demoIds.Contains(p.UserId)).ToListAsync();
            var studentProfiles    = await db.StudentProfiles   .Where(p => demoIds.Contains(p.UserId)).ToListAsync();
            var parentProfiles     = await db.ParentProfiles    .Where(p => demoIds.Contains(p.UserId)).ToListAsync();
            var supervisorProfiles = await db.SupervisorProfiles.Where(p => demoIds.Contains(p.UserId)).ToListAsync();
            var accountantProfiles = await db.AccountantProfiles.Where(p => demoIds.Contains(p.UserId)).ToListAsync();
            var refreshTokens      = await db.RefreshTokens     .Where(r => demoIds.Contains(r.UserId)).ToListAsync();
            var inviteLinks        = await db.InviteLinks       .Where(l => demoIds.Contains(l.CreatedByUserId)).ToListAsync();

            db.TeacherProfiles   .RemoveRange(teacherProfiles);
            db.StudentProfiles   .RemoveRange(studentProfiles);
            db.ParentProfiles    .RemoveRange(parentProfiles);
            db.SupervisorProfiles.RemoveRange(supervisorProfiles);
            db.AccountantProfiles.RemoveRange(accountantProfiles);
            db.RefreshTokens     .RemoveRange(refreshTokens);
            db.InviteLinks       .RemoveRange(inviteLinks);
            db.Users             .RemoveRange(demoUsers);

            await db.SaveChangesAsync();

            logger.LogInformation(
                "🧹 Removed {Count} demo account(s): {Emails}",
                demoUsers.Count,
                string.Join(", ", demoUsers.Select(u => u.Email)));
        }

        // ── 3. Ensure default School exists & tên là Marie Curie High School ────
        var school = await db.Schools.FirstOrDefaultAsync();
        if (school is null)
        {
            school = new School
            {
                SchoolCode = "MARIECURIE",
                SchoolName = SchoolName,
                Address    = "Việt Nam",
                IsActive   = true
            };
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            logger.LogInformation("🏫 Created default school '{Name}' (Id={Id})", school.SchoolName, school.Id);
        }
        else if (school.SchoolName != SchoolName)
        {
            var oldName = school.SchoolName;
            school.SchoolName = SchoolName;
            await db.SaveChangesAsync();
            logger.LogInformation("🏫 Renamed school '{Old}' → '{New}'", oldName, SchoolName);
        }

        // ── 4. Tài khoản quản trị ─────────────────────────────────────────────
        // • admin@lumina.edu.vn  → SYSADMIN (Quản trị hệ thống: toàn quyền + E-Selling)
        // • nhatruong@lumina.edu.vn → ADMIN (Nhà trường)
        var sysAdminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "SYSADMIN");
        var schoolRole   = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ADMIN");
        if (sysAdminRole is null || schoolRole is null)
        {
            logger.LogError("Roles SYSADMIN/ADMIN not found — cannot seed admin users.");
            return;
        }

        // 4a. Quản trị hệ thống (giữ admin@lumina.edu.vn, nâng lên SYSADMIN nếu cần)
        var sysAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail && u.SchoolId == school.Id);
        if (sysAdmin is null)
        {
            sysAdmin = new User
            {
                SchoolId        = school.Id,
                RoleId          = sysAdminRole.Id,
                Email           = AdminEmail,
                FullName        = "Quản trị hệ thống",
                IsActive        = true,
                IsEmailVerified = true,
                PasswordHash    = string.Empty
            };
            sysAdmin.PasswordHash = hasher.HashPassword(sysAdmin, AdminPassword);
            db.Users.Add(sysAdmin);
            await db.SaveChangesAsync();
            logger.LogInformation("👑 Created system admin (SYSADMIN): {Email} / {Password}", AdminEmail, AdminPassword);
        }
        else if (sysAdmin.RoleId != sysAdminRole.Id)
        {
            sysAdmin.RoleId = sysAdminRole.Id;   // nâng tài khoản admin hiện có → SYSADMIN
            await db.SaveChangesAsync();
            logger.LogInformation("👑 Upgraded {Email} to SYSADMIN (Quản trị hệ thống).", AdminEmail);
        }

        // 4b. Tài khoản Nhà trường (ADMIN)
        var schoolAdmin = await db.Users.FirstOrDefaultAsync(u => u.Email == SchoolAdminEmail && u.SchoolId == school.Id);
        if (schoolAdmin is null)
        {
            schoolAdmin = new User
            {
                SchoolId        = school.Id,
                RoleId          = schoolRole.Id,
                Email           = SchoolAdminEmail,
                FullName        = "Quản trị Nhà trường",
                IsActive        = true,
                IsEmailVerified = true,
                PasswordHash    = string.Empty
            };
            schoolAdmin.PasswordHash = hasher.HashPassword(schoolAdmin, SchoolAdminPassword);
            db.Users.Add(schoolAdmin);
            await db.SaveChangesAsync();
            logger.LogInformation("🏫 Created school admin (Nhà trường): {Email} / {Password}", SchoolAdminEmail, SchoolAdminPassword);
        }

        // ── 5. Ensure AcademicYear 2025-2026 + 2 semesters exist ─────────────
        var ay = await db.AcademicYears
            .FirstOrDefaultAsync(a => a.SchoolId == school.Id && a.YearName == "2026-2027");

        if (ay is null)
        {
            ay = new AcademicYear
            {
                SchoolId  = school.Id,
                YearName  = "2026-2027",
                StartDate = new DateOnly(2026, 9, 1),
                EndDate   = new DateOnly(2027, 5, 31),
                IsActive  = true
            };
            db.AcademicYears.Add(ay);
            await db.SaveChangesAsync();
            logger.LogInformation("📅 Created AcademicYear 2026-2027 (Id={Id})", ay.Id);
        }

        var semesterExists = await db.Semesters.AnyAsync(s => s.AcademicYearId == ay.Id);
        if (!semesterExists)
        {
            db.Semesters.AddRange(
                new Semester
                {
                    SchoolId       = school.Id,
                    AcademicYearId = ay.Id,
                    SemesterNumber = 1,
                    SemesterName   = "Học Kỳ 1",
                    StartDate      = new DateOnly(2026, 9, 1),
                    EndDate        = new DateOnly(2027, 1, 15),
                    IsActive       = true
                },
                new Semester
                {
                    SchoolId       = school.Id,
                    AcademicYearId = ay.Id,
                    SemesterNumber = 2,
                    SemesterName   = "Học Kỳ 2",
                    StartDate      = new DateOnly(2027, 1, 20),
                    EndDate        = new DateOnly(2027, 5, 31),
                    IsActive       = false
                }
            );
            await db.SaveChangesAsync();
            logger.LogInformation("📚 Created Học Kỳ 1 & Học Kỳ 2 for AcademicYear {AY}", ay.YearName);
        }

        // ── 6. Seed gói dịch vụ (SaaS) + add-on + đăng ký mặc định ───────────────
        await SeedSubscriptionsAsync(db, school, logger);
    }

    /// <summary>
    /// Áp migration nhưng chịu được việc LocalDB tự tắt khi idle: lần kết nối đầu
    /// thường timeout vì instance đang cold-start, nên thử lại vài lần với delay tăng dần.
    /// </summary>
    private static async Task MigrateWithRetryAsync(
        LuminaTutorsDbContext db, ILogger logger, int maxAttempts = 5)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                if (attempt > 1)
                    logger.LogInformation("✅ Kết nối DB thành công ở lần thử {Attempt}.", attempt);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(attempt * 3);   // 3s, 6s, 9s, 12s
                logger.LogWarning(
                    "⏳ DB chưa sẵn sàng (lần {Attempt}/{Max}): {Msg}. Thử lại sau {Delay}s…",
                    attempt, maxAttempts, ex.GetBaseException().Message, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }

    /// <summary>
    /// Tạo catalog gói (Basic/Premium) + add-on (AI Tutor, Virtual Lab) và đảm bảo
    /// trường mặc định có một đăng ký Premium đang hoạt động để các tính năng cao cấp
    /// dùng được ngay. Idempotent — bỏ qua nếu đã có.
    /// </summary>
    private static async Task SeedSubscriptionsAsync(
        LuminaTutorsDbContext db, School school, ILogger logger)
    {
        if (!await db.SubscriptionPlans.AnyAsync())
        {
            db.SubscriptionPlans.AddRange(
                new SubscriptionPlan
                {
                    PlanCode = "BASIC", Name = "Gói Cơ Bản", Tier = 1,
                    Description = "Quản lý học vụ, điểm danh, sổ điểm, tài chính cơ bản. Không gồm AI & Phòng 3D.",
                    MonthlyPrice = 990_000M, QuarterlyPrice = 2_700_000M, YearlyPrice = 9_900_000M,
                    IncludesAiTutor = false, IncludesVirtualLab = false, IsActive = true
                },
                new SubscriptionPlan
                {
                    PlanCode = "PREMIUM", Name = "Gói Cao Cấp", Tier = 2,
                    Description = "Toàn bộ tính năng Cơ Bản + Gia Sư AI + Phòng học 3D (Lab ảo & Lumina Nexus).",
                    MonthlyPrice = 2_490_000M, QuarterlyPrice = 6_900_000M, YearlyPrice = 24_900_000M,
                    IncludesAiTutor = true, IncludesVirtualLab = true, IsActive = true
                });
            await db.SaveChangesAsync();
            logger.LogInformation("💎 Seeded subscription plans: BASIC, PREMIUM");
        }

        if (!await db.SubscriptionAddOns.AnyAsync())
        {
            db.SubscriptionAddOns.AddRange(
                new SubscriptionAddOn
                {
                    AddOnCode = "AI_TUTOR", Name = "Gia Sư AI", Feature = PremiumFeature.AiTutor,
                    Description = "Trợ giảng AI trả lời câu hỏi cho học sinh 24/7.",
                    MonthlyPrice = 790_000M, QuarterlyPrice = 2_100_000M, YearlyPrice = 7_900_000M, IsActive = true
                },
                new SubscriptionAddOn
                {
                    AddOnCode = "VIRTUAL_LAB", Name = "Phòng học 3D", Feature = PremiumFeature.VirtualLab,
                    Description = "Lab thí nghiệm 3D ảo & phòng học hologram Lumina Nexus.",
                    MonthlyPrice = 990_000M, QuarterlyPrice = 2_700_000M, YearlyPrice = 9_900_000M, IsActive = true
                });
            await db.SaveChangesAsync();
            logger.LogInformation("🧩 Seeded subscription add-ons: AI_TUTOR, VIRTUAL_LAB");
        }

        // Trường mặc định khởi đầu ở gói Cơ Bản (KHÔNG gồm AI & Phòng 3D) để demo đúng
        // luồng chặn tính năng: phải nâng cấp lên Premium / mua add-on mới mở khóa AI/3D.
        if (!await db.SchoolSubscriptions.AnyAsync(s => s.SchoolId == school.Id))
        {
            var basic = await db.SubscriptionPlans.FirstAsync(p => p.PlanCode == "BASIC");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.SchoolSubscriptions.Add(new SchoolSubscription
            {
                SchoolId         = school.Id,
                PlanId           = basic.Id,
                BillingCycle     = SubscriptionCycle.Yearly,
                Status           = SubscriptionStatus.Active,
                StartDate        = today,
                CurrentPeriodEnd = today.AddYears(1),
                AutoRenew        = true
            });
            await db.SaveChangesAsync();
            logger.LogInformation("🏷️  Default school '{Name}' subscribed to BASIC (active 1 year)", school.SchoolName);
        }
    }
}
