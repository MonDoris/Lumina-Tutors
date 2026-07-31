using AutoMapper;
using LuminaTutors.Application.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LuminaTutors.UnitTests.TestKit;

/// <summary>
/// Cung cấp một <see cref="IMapper"/> THẬT, dựng từ đúng <see cref="MappingProfile"/>
/// mà ứng dụng dùng. Nhờ đó test cũng gián tiếp kiểm tra cấu hình AutoMapper là hợp lệ.
///
/// Ta dựng qua DI (giống hệt <c>AddApplication()</c>) để tránh khác biệt API giữa
/// các phiên bản AutoMapper. Mapper được khởi tạo một lần và tái sử dụng (Instance).
/// </summary>
internal static class TestMapper
{
    public static readonly IMapper Instance = Build();

    private static IMapper Build()
    {
        var services = new ServiceCollection();
        // AutoMapper 16 phân giải ILoggerFactory từ DI ⇒ đăng ký NullLoggerFactory (không log).
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));
        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }
}
