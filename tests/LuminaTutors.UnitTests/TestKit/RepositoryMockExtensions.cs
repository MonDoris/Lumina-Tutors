using System.Linq.Expressions;

namespace LuminaTutors.UnitTests.TestKit;

/// <summary>
/// Bộ "extension" giúp cấu hình <c>Mock&lt;IRepository&lt;T&gt;&gt;</c> chỉ bằng một dòng.
///
/// Ý tưởng: trong UNIT test, ta không cần repo lọc dữ liệu thật theo predicate —
/// ta chủ động quy định "repo trả về cái gì" rồi kiểm tra Service xử lý ra sao.
/// Vì thế mọi predicate/expression đều khớp bằng <see cref="It.IsAny{T}"/>.
///
/// Các overload có tham số <c>include</c> (nạp navigation) được setup RIÊNG để
/// khớp đúng chữ ký mà Service gọi (ví dụ: FindAsync(predicate, include, ct)).
/// </summary>
internal static class RepositoryMockExtensions
{
    // ─── FindAsync (trả về danh sách) ─────────────────────────────────────────

    /// <summary>Mọi FindAsync (có/không include) đều trả về danh sách <paramref name="results"/>.</summary>
    public static Mock<IRepository<T>> SetupFind<T>(this Mock<IRepository<T>> repo, params T[] results)
        where T : BaseEntity
    {
        IReadOnlyList<T> list = results.ToList();

        // Overload: FindAsync(predicate, ct)
        repo.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Overload: FindAsync(predicate, include, ct)
        repo.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        return repo;
    }

    /// <summary>Chỉ setup overload FindAsync(predicate, ct) — KHÔNG include.</summary>
    public static Mock<IRepository<T>> SetupFindNoInclude<T>(this Mock<IRepository<T>> repo, params T[] results)
        where T : BaseEntity
    {
        repo.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());
        return repo;
    }

    /// <summary>Chỉ setup overload FindAsync(predicate, include, ct) — CÓ include.</summary>
    public static Mock<IRepository<T>> SetupFindWithInclude<T>(this Mock<IRepository<T>> repo, params T[] results)
        where T : BaseEntity
    {
        repo.Setup(r => r.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(results.ToList());
        return repo;
    }

    // ─── FindOneAsync / FirstOrDefaultAsync (trả về 1 phần tử hoặc null) ───────

    /// <summary>FindOneAsync (có/không include) trả về <paramref name="entity"/> (có thể null).</summary>
    public static Mock<IRepository<T>> SetupFindOne<T>(this Mock<IRepository<T>> repo, T? entity)
        where T : BaseEntity
    {
        repo.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        repo.Setup(r => r.FindOneAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        return repo;
    }

    /// <summary>FirstOrDefaultAsync trả về <paramref name="entity"/> (có thể null).</summary>
    public static Mock<IRepository<T>> SetupFirstOrDefault<T>(this Mock<IRepository<T>> repo, T? entity)
        where T : BaseEntity
    {
        repo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        return repo;
    }

    // ─── GetByIdAsync ─────────────────────────────────────────────────────────

    /// <summary>GetByIdAsync (có/không include) trả về <paramref name="entity"/> (có thể null).</summary>
    public static Mock<IRepository<T>> SetupGetById<T>(this Mock<IRepository<T>> repo, T? entity)
        where T : BaseEntity
    {
        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        repo.Setup(r => r.GetByIdAsync(
                It.IsAny<int>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        return repo;
    }

    // ─── AnyAsync / CountAsync ────────────────────────────────────────────────

    /// <summary>AnyAsync luôn trả về <paramref name="exists"/>.</summary>
    public static Mock<IRepository<T>> SetupAny<T>(this Mock<IRepository<T>> repo, bool exists)
        where T : BaseEntity
    {
        repo.Setup(r => r.AnyAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);
        return repo;
    }

    /// <summary>CountAsync (có/không predicate) luôn trả về <paramref name="count"/>.</summary>
    public static Mock<IRepository<T>> SetupCount<T>(this Mock<IRepository<T>> repo, int count)
        where T : BaseEntity
    {
        repo.Setup(r => r.CountAsync(
                It.IsAny<Expression<Func<T, bool>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(count);
        return repo;
    }

    // ─── GetPagedAsync ────────────────────────────────────────────────────────

    /// <summary>GetPagedAsync trả về một trang chứa <paramref name="items"/>.</summary>
    public static Mock<IRepository<T>> SetupPaged<T>(this Mock<IRepository<T>> repo, params T[] items)
        where T : BaseEntity
    {
        var paged = new PagedResult<T>(items.ToList(), items.Length, 1, Math.Max(items.Length, 10));
        repo.Setup(r => r.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<T, bool>>?>(),
                It.IsAny<Func<IQueryable<T>, IOrderedQueryable<T>>?>(),
                It.IsAny<Func<IQueryable<T>, IQueryable<T>>?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);
        return repo;
    }

    // ─── AddAsync — bắt (capture) entity được thêm ────────────────────────────

    /// <summary>
    /// Ghi lại mọi entity được truyền vào <c>AddAsync</c> để test kiểm tra nội dung
    /// entity mà Service đã dựng. Trả về danh sách sẽ được điền dần khi Service chạy.
    /// </summary>
    public static List<T> CaptureAdds<T>(this Mock<IRepository<T>> repo) where T : BaseEntity
    {
        var captured = new List<T>();
        repo.Setup(r => r.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => captured.Add(entity))
            .Returns(Task.CompletedTask);
        return captured;
    }
}
