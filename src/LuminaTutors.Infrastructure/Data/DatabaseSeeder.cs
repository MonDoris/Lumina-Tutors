using LuminaTutors.Domain.Entities.Identity;
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
    }
}
