namespace LuminaTutors.Web.Models;

/// <summary>
/// Stored in IMemoryCache during the phone-based password-reset OTP flow.
/// Key pattern: "fp:otp:{normalizedPhone}"
/// </summary>
public sealed class OtpEntry
{
    public string   Code      { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public int      Attempts  { get; set;  }
}
