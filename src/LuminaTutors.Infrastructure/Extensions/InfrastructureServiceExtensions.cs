using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Interfaces.Repositories;
using LuminaTutors.Infrastructure.Data;
using LuminaTutors.Infrastructure.Data.Interceptors;
using LuminaTutors.Infrastructure.Repositories;
using LuminaTutors.Infrastructure.Services;

namespace LuminaTutors.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure services.
    /// Call from Web/Program.cs: builder.Services.AddInfrastructure(builder.Configuration);
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core DbContext ──────────────────────────────────────────────────
        services.AddSingleton<AuditInterceptor>();

        services.AddDbContext<LuminaTutorsDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("LuminaTutorsDb"),
                sql =>
                {
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    sql.CommandTimeout(30);
                    sql.MigrationsAssembly(typeof(LuminaTutorsDbContext).Assembly.FullName);
                });

            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());

#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        // ── Repository Pattern ────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        // ── Email (SMTP: App Password hoặc Gmail OAuth) ───────────────────────
        // Chưa cấu hình section "Email" → sender ghi email ra logs/emails thay vì gửi thật.
        services.AddHttpClient("GmailOAuth", c => c.Timeout = TimeSpan.FromSeconds(20));
        services.AddSingleton<IGmailTokenProvider, GmailOAuthTokenProvider>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // ── AI Tutor (Ollama) ─────────────────────────────────────────────────
        services.AddHttpClient("Ollama", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(15);
        });
        services.AddScoped<IAiTutorService, AiTutorService>();

        // Nạp sẵn model Ollama lúc khởi động (chạy nền, không chặn startup)
        services.AddHostedService<OllamaWarmupService>();

        return services;
    }
}
