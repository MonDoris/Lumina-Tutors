using LuminaTutors.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Infrastructure.Data;

/// <summary>
/// Seeds the database with one demo School + one user per role for development / testing.
/// Call this from Program.cs in Development:
///     await DatabaseSeeder.SeedAsync(app.Services);
/// </summary>
public static class DatabaseSeeder
{
    // ── Demo credentials ──────────────────────────────────────────────────────
    //
    //  Role        | Email                          | Password
    //  ------------|-------------------------------|------------
    //  ADMIN       | admin@lumina.edu.vn            | Admin@123
    //  TEACHER     | teacher@lumina.edu.vn          | Teacher@123
    //  ACCOUNTANT  | accountant@lumina.edu.vn       | Account@123
    //  SUPERVISOR  | supervisor@lumina.edu.vn       | Super@123
    //  STUDENT     | student@lumina.edu.vn          | Student@123
    //  PARENT      | parent@lumina.edu.vn           | Parent@123
    //
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope  = services.CreateScope();
        var db           = scope.ServiceProvider.GetRequiredService<LuminaTutorsDbContext>();
        var hasher       = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var logger       = scope.ServiceProvider.GetRequiredService<ILogger<LuminaTutorsDbContext>>();

        // Apply pending migrations first
        await db.Database.MigrateAsync();

        // ── 1. Seed School ────────────────────────────────────────────────────
        int schoolId;
        var existingSchool = await db.Schools.FirstOrDefaultAsync(s => s.SchoolCode == "LT-DEMO-001");

        if (existingSchool is null)
        {
            var school = new School
            {
                SchoolCode  = "LT-DEMO-001",
                SchoolName  = "Trung tâm Lumina Tutors (Demo)",
                Address     = "123 Nguyễn Văn Linh, Quận 7, TP. Hồ Chí Minh",
                Province    = "TP. Hồ Chí Minh",
                PhoneNumber = "028 1234 5678",
                Email       = "info@lumina.edu.vn",
                IsActive    = true,
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow
            };
            db.Schools.Add(school);
            await db.SaveChangesAsync();
            schoolId = school.Id;
            logger.LogInformation("✅ Seeded demo school (Id={SchoolId})", schoolId);
        }
        else
        {
            schoolId = existingSchool.Id;
            logger.LogInformation("ℹ️  Demo school already exists (Id={SchoolId})", schoolId);
        }

        // ── 2. Seed one user per role ─────────────────────────────────────────
        var roleMap = new Dictionary<string, (int RoleId, string Email, string FullName, string Password)>
        {
            ["ADMIN"]      = (1, "admin@lumina.edu.vn",       "Nguyễn Quản Trị",    "Admin@123"),
            ["TEACHER"]    = (2, "teacher@lumina.edu.vn",     "Trần Giáo Viên",     "Teacher@123"),
            ["STUDENT"]    = (3, "student@lumina.edu.vn",     "Lê Học Sinh",        "Student@123"),
            ["PARENT"]     = (4, "parent@lumina.edu.vn",      "Phạm Phụ Huynh",    "Parent@123"),
            ["SUPERVISOR"] = (5, "supervisor@lumina.edu.vn",  "Hoàng Giám Thị",    "Super@123"),
            ["ACCOUNTANT"] = (6, "accountant@lumina.edu.vn",  "Vũ Kế Toán",        "Account@123"),
        };

        foreach (var (roleCode, (roleId, email, fullName, password)) in roleMap)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var exists = await db.Users.AnyAsync(
                u => u.Email == normalizedEmail && u.SchoolId == schoolId);

            if (exists)
            {
                logger.LogInformation("ℹ️  User [{Role}] {Email} already exists — skipping", roleCode, email);
                continue;
            }

            // Compute hash using the real ASP.NET Identity hasher
            var dummy = new User();
            var hash  = hasher.HashPassword(dummy, password);

            var user = new User
            {
                SchoolId        = schoolId,
                RoleId          = roleId,
                Email           = normalizedEmail,
                PasswordHash    = hash,
                FullName        = fullName,
                IsActive        = true,
                IsEmailVerified = true,
                CreatedAt       = DateTime.UtcNow,
                UpdatedAt       = DateTime.UtcNow
            };

            db.Users.Add(user);
            logger.LogInformation("✅ Seeded [{Role}] {Email}", roleCode, email);
        }

        await db.SaveChangesAsync();
        logger.LogInformation("🌱 DatabaseSeeder completed.");
    }
}
