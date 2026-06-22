namespace LuminaTutors.Application.Common;

/// <summary>
/// Quy ước email đăng nhập theo vai trò + domain riêng của từng trường.
///
/// • Domain GỐC của trường = domain email tài khoản Nhà trường (ADMIN) do Sysadmin tạo.
///   Ví dụ admin <c>hieutruong@ltmc.edu.vn</c> ⇒ domain gốc trường = <c>ltmc.edu.vn</c>.
/// • Các vai trò khác ghép TIỀN TỐ cố định vào domain gốc đó:
///   Học sinh <c>@stu.ltmc.edu.vn</c>, Giáo viên <c>@tea.ltmc.edu.vn</c>,
///   Giám thị <c>@sup.</c>, Phụ huynh <c>@par.</c>, Kế toán <c>@acc.</c>
/// • ADMIN (Nhà trường) giữ domain gốc (không tiền tố); SYSADMIN ở cấp nền tảng (không tiền tố).
/// • Đuôi do hệ thống cố định — người tạo chỉ nhập phần tên (trước @).
///
/// Tương thích ngược: email không khớp quy ước (tài khoản cũ) ⇒ <see cref="RoleOf"/> trả null,
/// phần đăng nhập bỏ qua bước đối chiếu đuôi.
/// </summary>
public static class RoleEmail
{
    // Vai trò có tiền tố subdomain riêng (ADMIN & SYSADMIN không có).
    private static readonly Dictionary<string, string> _prefixByRole = new(StringComparer.OrdinalIgnoreCase)
    {
        ["STUDENT"]    = "stu",
        ["TEACHER"]    = "tea",
        ["SUPERVISOR"] = "sup",
        ["PARENT"]     = "par",
        ["ACCOUNTANT"] = "acc",
    };

    private static readonly Dictionary<string, string> _roleByPrefix =
        _prefixByRole.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>Tiền tố subdomain của vai trò (vd "stu"); null nếu vai trò giữ domain gốc.</summary>
    public static string? PrefixFor(string? roleCode)
        => roleCode is not null && _prefixByRole.TryGetValue(roleCode, out var p) ? p : null;

    /// <summary>Domain gốc của trường = phần sau @ của một email trong trường, đã bỏ tiền tố vai trò nếu có.</summary>
    public static string BaseDomain(string? anyEmailInSchool)
    {
        var normalized = (anyEmailInSchool ?? "").Trim().ToLowerInvariant();
        var at = normalized.IndexOf('@');
        if (at < 0 || at == normalized.Length - 1) return "lumina.edu.vn";

        var domain   = normalized[(at + 1)..];
        var firstDot = domain.IndexOf('.');
        if (firstDot > 0 && _roleByPrefix.ContainsKey(domain[..firstDot]))
            domain = domain[(firstDot + 1)..];   // bỏ tiền tố vai trò để lấy domain gốc
        return domain;
    }

    /// <summary>
    /// Dựng email đăng nhập cố định: phần tên (trước @, do người tạo nhập) ghép với
    /// đuôi <c>[tiền-tố-vai-trò.]domain-gốc-trường</c>. Phần domain người nhập bị bỏ qua.
    /// </summary>
    public static string Build(string? localOrEmail, string? roleCode, string baseDomain)
    {
        var raw   = (localOrEmail ?? "").Trim().ToLowerInvariant();
        var at    = raw.IndexOf('@');
        var local = (at >= 0 ? raw[..at] : raw).Trim();
        if (string.IsNullOrEmpty(local)) local = "user";

        var prefix = PrefixFor(roleCode);
        return prefix is null ? $"{local}@{baseDomain}" : $"{local}@{prefix}.{baseDomain}";
    }

    /// <summary>Suy ra RoleCode từ tiền tố subdomain của email; null nếu không có tiền tố vai trò.</summary>
    public static string? RoleOf(string? email)
    {
        var normalized = (email ?? "").Trim().ToLowerInvariant();
        var at = normalized.IndexOf('@');
        if (at < 0) return null;

        var domain   = normalized[(at + 1)..];
        var firstDot = domain.IndexOf('.');
        if (firstDot <= 0) return null;
        return _roleByPrefix.TryGetValue(domain[..firstDot], out var role) ? role : null;
    }
}
