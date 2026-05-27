using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using LuminaTutors.Application.Extensions;
using LuminaTutors.Infrastructure.Extensions;

// ── Bootstrap Serilog ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/lumina-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30));

    // ── Infrastructure (DbContext + UnitOfWork + Repositories) ───────────────
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Application (Services + AutoMapper) ──────────────────────────────────
    builder.Services.AddApplication();

    // ── MVC ───────────────────────────────────────────────────────────────────
    builder.Services.AddControllersWithViews()
        .AddViewOptions(o => o.HtmlHelperOptions.ClientValidationEnabled = true);

    // ── Authentication (Cookie-based for MVC) ─────────────────────────────────
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath         = "/Auth/Login";
            options.LogoutPath        = "/Auth/Logout";
            options.AccessDeniedPath  = "/Auth/AccessDenied";
            options.ExpireTimeSpan    = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly   = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite   = SameSiteMode.Strict;
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly",       p => p.RequireRole("ADMIN"));
        options.AddPolicy("TeacherOrAdmin",  p => p.RequireRole("TEACHER", "ADMIN"));
        options.AddPolicy("FinanceAccess",   p => p.RequireRole("ACCOUNTANT", "ADMIN"));
        options.AddPolicy("SupervisorAccess",p => p.RequireRole("SUPERVISOR", "ADMIN"));
        options.AddPolicy("AnyAuthenticated",p => p.RequireAuthenticatedUser());
    });

    // ── Session ───────────────────────────────────────────────────────────────
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout        = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly    = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddMemoryCache();
    builder.Services.AddHttpContextAccessor();

    var app = builder.Build();

    // ── Dev Seeder ────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        await LuminaTutors.Infrastructure.Data.DatabaseSeeder.SeedAsync(app.Services);
    }

    // ── Middleware Pipeline ────────────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseSerilogRequestLogging();
    app.UseRouting();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Routes ────────────────────────────────────────────────────────────────
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    Log.Information("🌟 Lumina Tutors starting on {Environment}", app.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Lumina Tutors terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
