using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using LuminaTutors.Application.Extensions;
using LuminaTutors.Infrastructure.Extensions;
using LuminaTutors.Web.Hubs;
using LuminaTutors.Web.Filters;

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
    var mvcBuilder = builder.Services.AddControllersWithViews(o =>
            o.Filters.Add<RequireActiveSubscriptionFilter>())   // cổng: bắt buộc có gói đang hoạt động
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
        // Quản trị hệ thống: quản lý gói E-Selling (catalog gói/add-on, đăng ký của mọi trường)
        options.AddPolicy("SystemAdmin",     p => p.RequireRole("SYSADMIN"));
        // Chỉ Nhà trường (ADMIN) — KHÔNG gồm Quản trị hệ thống: dùng cho việc mua/đăng ký
        // gói dịch vụ của chính trường. SYSADMIN là bên bán nên không đăng ký gói.
        options.AddPolicy("SchoolAdminOnly", p => p.RequireAssertion(ctx =>
            ctx.User.IsInRole("ADMIN") && !ctx.User.IsInRole("SYSADMIN")));
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

    // ── Bảng vẽ Toán 3D: kho trạng thái phòng in-memory (đồng bộ real-time) ────
    builder.Services.AddSingleton<LuminaTutors.Web.Hubs.LabRoomStore>();

    // ── Lumina Holographic Nexus: SFU thuần C# (SIPSorcery) ───────────────────
    builder.Services.AddSingleton<LuminaTutors.Web.Hubs.ILuminaSfuService,
                                  LuminaTutors.Web.Hubs.LuminaSfuService>();

    // ── HttpClient (URL scraping for Question Bank import) ────────────────────
    builder.Services.AddHttpClient();

    var app = builder.Build();

    // ── Dev Seeder ────────────────────────────────────────────────────────────
    // KHÔNG để lỗi seeding/migration làm sập app lúc khởi động: nếu LocalDB chưa
    // sẵn sàng, web server vẫn chạy (tránh lỗi VS "web server is no longer running").
    if (app.Environment.IsDevelopment())
    {
        try
        {
            await LuminaTutors.Infrastructure.Data.DatabaseSeeder.SeedAsync(app.Services);
        }
        catch (Exception seedEx)
        {
            Log.Warning(seedEx,
                "Bỏ qua seeding do lỗi cơ sở dữ liệu lúc khởi động — ứng dụng vẫn chạy. " +
                "Kiểm tra LocalDB (sqllocaldb start mssqllocaldb) rồi tải lại trang/khởi động lại.");
        }
    }

    // ── Demo Quota Seeder (tùy chọn — bật bằng cờ Seed:FillBasicQuota) ──────────
    // Lấp đầy tài khoản MỌI vai trò tới hạn mức Gói Cơ Bản cho trường Demo
    // (GV 20 · HS 500 · PH 500 · Quản trị 3 · Kế toán 2 · Giám thị 2).
    // Idempotent: chỉ tạo phần còn thiếu, đã đủ thì không tạo thêm. Mật khẩu chung: Demo@123.
    if (app.Environment.IsDevelopment()
        && app.Configuration.GetValue<bool>("Seed:FillBasicQuota"))
    {
        try
        {
            await LuminaTutors.Infrastructure.Data.DemoQuotaSeeder.SeedToBasicLimitAsync(app.Services);
        }
        catch (Exception seedEx)
        {
            Log.Warning(seedEx, "Bỏ qua DemoQuotaSeeder do lỗi cơ sở dữ liệu — ứng dụng vẫn chạy.");
        }
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

    // ── Mất kết nối CSDL (LocalDB cold-start/treo) → trang thân thiện ──────────
    // Đặt SAU trang lỗi dev nên middleware này (lớp trong) bắt lỗi CSDL trước và
    // hiển thị hướng dẫn; các lỗi lập trình khác vẫn ném lên trang lỗi dev để debug.
    app.Use(async (ctx, next) =>
    {
        try { await next(); }
        catch (Exception ex) when (IsDbDown(ex) && !ctx.Response.HasStarted)
        {
            Log.Warning(ex, "CSDL không kết nối được khi xử lý {Path}", ctx.Request.Path);
            ctx.Response.StatusCode  = StatusCodes.Status503ServiceUnavailable;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync(DbDownPage());
        }
    });

    static bool IsDbDown(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            var m = e.Message ?? "";
            if (e.GetType().Name == "SqlException"
                || m.Contains("LocalDB", StringComparison.OrdinalIgnoreCase)
                || m.Contains("network-related", StringComparison.OrdinalIgnoreCase)
                || m.Contains("RetryLimitExceeded", StringComparison.OrdinalIgnoreCase)
                || m.Contains("connection to SQL Server", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static string DbDownPage() =>
        "<!DOCTYPE html><html lang='vi'><head><meta charset='utf-8'>" +
        "<meta name='viewport' content='width=device-width,initial-scale=1'>" +
        "<title>Mất kết nối cơ sở dữ liệu</title></head>" +
        "<body style='margin:0;font-family:system-ui,Segoe UI,sans-serif;background:#0b1726;color:#eaf1fb;" +
        "min-height:100vh;display:flex;align-items:center;justify-content:center;padding:24px'>" +
        "<div style='max-width:460px;text-align:center;background:rgba(255,255,255,.05);" +
        "border:1px solid rgba(255,255,255,.12);border-radius:18px;padding:38px 32px'>" +
        "<div style='font-size:3rem'>🗄️</div>" +
        "<h1 style='font-size:1.3rem;margin:14px 0 8px;color:#fff'>Chưa kết nối được cơ sở dữ liệu</h1>" +
        "<p style='color:#a9bbd4;line-height:1.6;font-size:.95rem;margin:0 0 10px'>" +
        "LocalDB đang khởi động lại hoặc tạm thời không phản hồi. Vui lòng thử lại sau giây lát.</p>" +
        "<p style='color:#7e90ad;font-size:.8rem;margin:0 0 22px'>Nếu vẫn lỗi, mở PowerShell chạy: " +
        "<code style='background:rgba(0,0,0,.35);padding:2px 7px;border-radius:6px'>sqllocaldb start mssqllocaldb</code></p>" +
        "<a href='javascript:location.reload()' style='display:inline-block;" +
        "background:linear-gradient(135deg,#e7cf86,#c9a84c);color:#3a2e08;text-decoration:none;" +
        "font-weight:700;padding:12px 26px;border-radius:11px'>&#8635; Thử lại</a>" +
        "</div></body></html>";

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
    app.MapHub<LuminaTutors.Web.Hubs.LabHub>("/hubs/lab");

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
