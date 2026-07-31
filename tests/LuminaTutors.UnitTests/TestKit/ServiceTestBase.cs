using System.Linq.Expressions;

namespace LuminaTutors.UnitTests.TestKit;

/// <summary>
/// Lớp cơ sở cho mọi bộ test của tầng Application (Service).
///
/// Nhiệm vụ:
///  • Tạo sẵn một <see cref="IUnitOfWork"/> đã được mock (<see cref="Uow"/>).
///  • Cấp phát "lazy" mock cho từng repository khi test cần tới — gọi <see cref="Repo{T}"/>.
///    Mỗi entity chỉ có DUY NHẤT một mock repo, được cache lại và tự động gắn vào UoW.
///  • Cung cấp mapper AutoMapper thật (dựng từ MappingProfile của ứng dụng).
///
/// Nhờ vậy phần "Arrange" trong mỗi test rất ngắn:
///     Repo(u => u.Users).SetupFind(user);
/// thay vì phải tự tạo Mock, tự Setup UoW... lặp đi lặp lại.
/// </summary>
public abstract class ServiceTestBase
{
    /// <summary>Unit of Work đã mock — tiêm vào constructor của Service đang test.</summary>
    protected readonly Mock<IUnitOfWork> Uow = new();

    /// <summary>Mapper thật (dùng chung MappingProfile với ứng dụng).</summary>
    protected readonly AutoMapper.IMapper Mapper = TestMapper.Instance;

    // Cache mock repo theo kiểu entity để mỗi entity chỉ có một mock duy nhất.
    private readonly Dictionary<Type, object> _repos = new();

    protected ServiceTestBase()
    {
        // Mặc định: ExecuteInTransactionAsync CHẠY THẬT hàm được truyền vào (thay vì bỏ qua),
        // để test bao phủ được cả phần thân giao dịch của service.
        Uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
           .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    /// <summary>
    /// Lấy (hoặc tạo mới lần đầu) mock repository cho entity <typeparamref name="T"/>,
    /// đồng thời gắn nó vào <see cref="Uow"/> qua <paramref name="selector"/>.
    /// </summary>
    /// <example>
    ///   Repo(u => u.Users).SetupFind(existingUser);
    /// </example>
    protected Mock<IRepository<T>> Repo<T>(Expression<Func<IUnitOfWork, IRepository<T>>> selector)
        where T : BaseEntity
    {
        if (_repos.TryGetValue(typeof(T), out var cached))
            return (Mock<IRepository<T>>)cached;

        var mock = new Mock<IRepository<T>>();
        Uow.Setup(selector).Returns(mock.Object);
        _repos[typeof(T)] = mock;
        return mock;
    }

    /// <summary>Khẳng định service đã lưu thay đổi (gọi SaveChangesAsync).</summary>
    protected void ShouldHaveSaved(Times? times = null) =>
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            times ?? Times.AtLeastOnce());

    /// <summary>Khẳng định service KHÔNG lưu gì cả (dùng cho các nhánh lỗi/validation).</summary>
    protected void ShouldNotHaveSaved() =>
        Uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());

    // ─── Trợ giúp khẳng định thất bại ─────────────────────────────────────────
    // Một số service đặt mã lỗi vào ô Error thay vì ErrorCode (quy ước không đồng nhất).
    // Hai helper dưới kiểm tra mã xuất hiện ở BẤT KỲ ô nào ⇒ test bền vững với sự khác biệt đó.

    protected static void ShouldFail(Result result, string expectedCode)
    {
        result.IsSuccess.Should().BeFalse();
        ($"{result.Error}|{result.ErrorCode}").Should().Contain(expectedCode);
    }

    protected static void ShouldFail<T>(Result<T> result, string expectedCode)
    {
        result.IsSuccess.Should().BeFalse();
        ($"{result.Error}|{result.ErrorCode}").Should().Contain(expectedCode);
    }
}
