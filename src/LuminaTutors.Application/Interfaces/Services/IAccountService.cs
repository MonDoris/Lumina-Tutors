using LuminaTutors.Application.DTOs.Account;
using LuminaTutors.Domain.Common;

namespace LuminaTutors.Application.Interfaces.Services;

/// <summary>
/// Admin-only service: CRUD tài khoản cho 4 roles (Student, Teacher, Supervisor, Parent).
/// </summary>
public interface IAccountService
{
    /// <summary>Danh sách tài khoản có phân trang và lọc theo role / keyword.</summary>
    Task<Result<PagedResult<AccountListItemDto>>> GetAccountsAsync(
        int schoolId, AccountFilterRequest filter, CancellationToken ct = default);

    /// <summary>Chi tiết một tài khoản.</summary>
    Task<Result<AccountDetailDto>> GetAccountByIdAsync(
        int schoolId, int userId, CancellationToken ct = default);

    /// <summary>Tạo tài khoản + profile tương ứng theo role trong một transaction.</summary>
    Task<Result<AccountDetailDto>> CreateAccountAsync(
        int schoolId, CreateAccountRequest request, CancellationToken ct = default);

    /// <summary>Cập nhật thông tin tài khoản + profile.</summary>
    Task<Result<AccountDetailDto>> UpdateAccountAsync(
        int schoolId, int userId, UpdateAccountRequest request, CancellationToken ct = default);

    /// <summary>Bật/tắt trạng thái hoạt động.</summary>
    Task<Result> ToggleActiveAsync(
        int schoolId, int userId, CancellationToken ct = default);

    /// <summary>Đặt lại mật khẩu bởi Admin.</summary>
    Task<Result> ResetPasswordAsync(
        int schoolId, int userId, string newPassword, CancellationToken ct = default);

    /// <summary>Xóa tài khoản + profile (soft delete = deactivate, hard delete = xóa hẳn).</summary>
    Task<Result> DeleteAccountAsync(
        int schoolId, int userId, CancellationToken ct = default);

    /// <summary>Danh sách học sinh trong trường (dùng cho dropdown gán lớp / liên kết phụ huynh).</summary>
    Task<Result<IReadOnlyList<(int UserId, string FullName, string? ClassName)>>> GetStudentSelectListAsync(
        int schoolId, CancellationToken ct = default);

    /// <summary>Danh sách lớp học trong trường (dùng cho dropdown).</summary>
    Task<Result<IReadOnlyList<(int ClassId, string ClassName)>>> GetClassSelectListAsync(
        int schoolId, CancellationToken ct = default);
}
