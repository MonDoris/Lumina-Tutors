using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LuminaTutors.Application.DTOs.Auth;
using LuminaTutors.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LuminaTutors.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthApiController : ControllerBase
{
    private readonly IAuthService    _auth;
    private readonly IConfiguration  _config;

    public AuthApiController(IAuthService auth, IConfiguration config)
    {
        _auth   = auth;
        _config = config;
    }

    /// <summary>POST api/auth/login — đăng nhập, trả JWT</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Dữ liệu không hợp lệ.";
            return BadRequest(new { message = errors });
        }

        var result = await _auth.LoginAsync(model);
        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Error });

        var data  = result.Data!;
        var token = GenerateJwt(data);

        return Ok(new
        {
            token,
            expiresIn  = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"] ?? "60") * 60,
            userId     = data.UserId,
            fullName   = data.FullName,
            email      = data.Email,
            roleCode   = data.RoleCode,
            schoolId   = data.SchoolId,
            schoolName = data.SchoolName,
            avatarUrl  = data.AvatarUrl
        });
    }

    /// <summary>GET api/auth/me — thông tin user hiện tại</summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Me() => Ok(new
    {
        userId   = User.FindFirstValue(ClaimTypes.NameIdentifier),
        fullName = User.FindFirstValue(ClaimTypes.Name),
        email    = User.FindFirstValue(ClaimTypes.Email),
        roleCode = User.FindFirstValue(ClaimTypes.Role),
        schoolId = User.FindFirstValue("SchoolId")
    });

    // ── Private helpers ───────────────────────────────────────────────────────

    private string GenerateJwt(LoginResponse data)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry  = int.Parse(_config["JwtSettings:AccessTokenExpiryMinutes"] ?? "60");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, data.UserId.ToString()),
            new Claim(ClaimTypes.Name,           data.FullName),
            new Claim(ClaimTypes.Email,          data.Email),
            new Claim(ClaimTypes.Role,           data.RoleCode),
            new Claim("SchoolId",                data.SchoolId.ToString()),
            new Claim("SchoolName",              data.SchoolName)
        };

        var token = new JwtSecurityToken(
            issuer:             _config["JwtSettings:Issuer"],
            audience:           _config["JwtSettings:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
