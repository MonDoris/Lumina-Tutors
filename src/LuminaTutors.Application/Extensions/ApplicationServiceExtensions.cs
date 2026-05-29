using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Application.Mappings;
using LuminaTutors.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LuminaTutors.Application.Extensions;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all Application-layer services into the DI container.
    /// Call this from Program.cs: builder.Services.AddApplication();
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ── AutoMapper ──────────────────────────────────────────────────────
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

        // ── Password Hasher (shared across auth + creation flows) ───────────
        services.AddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));

        // ── Business Services ───────────────────────────────────────────────
        services.AddScoped<IAuthService,         AuthService>();
        services.AddScoped<IStudentService,      StudentService>();
        services.AddScoped<IClassService,        ClassService>();
        services.AddScoped<IAttendanceService,   AttendanceService>();
        services.AddScoped<IGradingService,      GradingService>();
        services.AddScoped<IFinanceService,      FinanceService>();
        services.AddScoped<IHRService,           HRService>();
        services.AddScoped<IDisciplineService,   DisciplineService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMessageService,      MessageService>();
        services.AddScoped<INewsBoardService,    NewsBoardService>();
        services.AddScoped<IAccountService,      AccountService>();
        services.AddScoped<IVirtualLabService,   VirtualLabService>();
        services.AddScoped<IQuizService,         QuizService>();
        services.AddScoped<IOnlineClassService,      OnlineClassService>();
        services.AddScoped<IOnlineClassroomService,  OnlineClassroomService>();
        services.AddScoped<IQuestionBankService,     QuestionBankService>();

        return services;
    }
}
