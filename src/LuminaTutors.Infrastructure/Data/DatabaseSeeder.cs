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

    private const string AdminEmail    = "admin@lumina.edu.vn";
    private const string AdminPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope  = services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<LuminaTutorsDbContext>();
        var logger       = scope.ServiceProvider.GetRequiredService<ILogger<LuminaTutorsDbContext>>();
        var hasher       = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        // ── 1. Apply pending migrations ───────────────────────────────────────
        await db.Database.MigrateAsync();

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

        // ── 3. Ensure default School exists ───────────────────────────────────
        var school = await db.Schools.FirstOrDefaultAsync();
        if (school is null)
        {
            school = new School
            {
                SchoolCode = "LUMINA001",
                SchoolName = "Lumina Tutors",
                Address    = "Việt Nam",
                IsActive   = true
            };
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            logger.LogInformation("🏫 Created default school '{Name}' (Id={Id})", school.SchoolName, school.Id);
        }

        // ── 4. Ensure Admin account exists ────────────────────────────────────
        var adminExists = await db.Users
            .AnyAsync(u => u.Email == AdminEmail && u.SchoolId == school.Id);

        if (!adminExists)
        {
            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "ADMIN");
            if (adminRole is null)
            {
                logger.LogError("ADMIN role not found — cannot seed admin user.");
                return;
            }

            var admin = new User
            {
                SchoolId        = school.Id,
                RoleId          = adminRole.Id,
                Email           = AdminEmail,
                FullName        = "Quản trị viên",
                IsActive        = true,
                IsEmailVerified = true,
                PasswordHash    = string.Empty
            };
            admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);

            db.Users.Add(admin);
            await db.SaveChangesAsync();

            logger.LogInformation("👤 Created admin account: {Email} / {Password}", AdminEmail, AdminPassword);
        }
        else
        {
            logger.LogInformation("ℹ️  Admin account already exists — skipping seed.");
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

        // Đảm bảo trường mặc định có đăng ký Premium đang hoạt động (để demo không bị khóa tính năng)
        if (!await db.SchoolSubscriptions.AnyAsync(s => s.SchoolId == school.Id))
        {
            var premium = await db.SubscriptionPlans.FirstAsync(p => p.PlanCode == "PREMIUM");
            var today   = DateOnly.FromDateTime(DateTime.UtcNow);
            db.SchoolSubscriptions.Add(new SchoolSubscription
            {
                SchoolId         = school.Id,
                PlanId           = premium.Id,
                BillingCycle     = SubscriptionCycle.Yearly,
                Status           = SubscriptionStatus.Active,
                StartDate        = today,
                CurrentPeriodEnd = today.AddYears(1),
                AutoRenew        = true
            });
            await db.SaveChangesAsync();
            logger.LogInformation("🏷️  Default school '{Name}' subscribed to PREMIUM (active 1 year)", school.SchoolName);
        }
    }
}
