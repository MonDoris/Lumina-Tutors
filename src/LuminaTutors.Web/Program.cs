using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using LuminaTutors.Application.Extensions;
using LuminaTutors.Infrastructure.Extensions;
using LuminaTutors.Web.Hubs;

// ── Bootstrap Serilog ─────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── QuestPDF (giấy phép Community — miễn phí, bắt buộc set trước khi tạo PDF) ─
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

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
    var mvcBuilder = builder.Services.AddControllersWithViews()
        .AddViewOptions(o => o.HtmlHelperOptions.ClientValidationEnabled = true)
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    // Hot-reload Razor views in Development without restarting the app
    if (builder.Environment.IsDevelopment())
        mvcBuilder.AddRazorRuntimeCompilation();

    // ── Authentication (Cookie for MVC + JWT for API) ────────────────────────
    var jwtKey    = builder.Configuration["JwtSettings:SecretKey"]!;
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]!;
    var jwtAud    = builder.Configuration["JwtSettings:Audience"]!;

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtIssuer,
                ValidAudience            = jwtAud,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew                = TimeSpan.Zero
            };
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath         = "/Auth/Login";
            options.LogoutPath        = "/Auth/Logout";
            options.AccessDeniedPath  = "/Auth/AccessDenied";
            options.ExpireTimeSpan    = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly     = true;
            // SameAsRequest: HTTPS → Secure cookie, HTTP → non-secure cookie
            // (cần thiết khi mobile WebView dùng http://192.168.x.x)
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            // Lax cho phép cookie gửi kèm redirect (Strict chặn cả redirect WebView)
            options.Cookie.SameSite     = SameSiteMode.Lax;
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly",       p => p.RequireRole("ADMIN"));
        options.AddPolicy("TeacherOrAdmin",  p => p.RequireRole("TEACHER", "ADMIN"));
        options.AddPolicy("FinanceAccess",   p => p.RequireRole("ACCOUNTANT", "ADMIN"));
        options.AddPolicy("SupervisorAccess",p => p.RequireRole("SUPERVISOR", "ADMIN"));
        options.AddPolicy("AnyAuthenticated",p => p.RequireAuthenticatedUser());
        options.AddPolicy("LabAccess",       p => p.RequireRole("TEACHER", "ADMIN", "STUDENT", "PARENT", "SUPERVISOR", "ACCOUNTANT"));
        // API policies (JWT)
        options.AddPolicy("ApiAccess", p => p
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());
    });

    // ── CORS for React Native ────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("MobileApp", policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
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

    // ── SignalR (Online Classroom real-time) ──────────────────────────────────
    builder.Services.AddSignalR();

    // ── Lumina Holographic Nexus: SFU thuần C# (SIPSorcery) ───────────────────
    builder.Services.AddSingleton<LuminaTutors.Web.Hubs.ILuminaSfuService,
                                  LuminaTutors.Web.Hubs.LuminaSfuService>();

    // ── HttpClient (URL scraping for Question Bank import) ────────────────────
    builder.Services.AddHttpClient();

    var app = builder.Build();

    // ── Dev Seeder ────────────────────────────────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        await LuminaTutors.Infrastructure.Data.DatabaseSeeder.SeedAsync(app.Services);
    }

    // ── Forwarded headers (chạy SAU Cloudflare Tunnel / ngrok / reverse proxy) ──
    // Giúp app nhận đúng scheme (https) & host gốc khi đứng sau tunnel — cần cho
    // cookie Secure, redirect và sinh URL tuyệt đối. Tunnel chạy cục bộ nên proxy
    // là loopback (được tin mặc định). Phải đặt TRƯỚC mọi middleware khác.
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
    });

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

    if (!app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    // Trong Development: tắt cache file tĩnh để các module JS (đặc biệt là ESM
    // import lồng nhau trong /js/nexus, /js/three không được asp-append-version)
    // luôn được tải lại sau khi sửa — tránh chạy phải bản cache cũ.
    if (app.Environment.IsDevelopment())
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                ctx.Context.Response.Headers.Pragma = "no-cache";
                ctx.Context.Response.Headers.Expires = "0";
            }
        });
    }
    else
    {
        app.UseStaticFiles();
    }
    app.UseSerilogRequestLogging();
    app.UseCors("MobileApp");
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

    // ── SignalR Hub Route ─────────────────────────────────────────────────────
    app.MapHub<OnlineClassHub>("/hubs/online-class");
    app.MapHub<LuminaTutors.Web.Hubs.LuminaRtcHub>("/hubs/lumina-rtc");

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
