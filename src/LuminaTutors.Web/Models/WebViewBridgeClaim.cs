namespace LuminaTutors.Web.Models;

/// <summary>Claim data stored in IMemoryCache for the WebView bridge (one-time use).</summary>
public sealed record WebViewBridgeClaim(string Type, string Value);
