using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.Common;

/// <summary>
/// JWT options bound from configuration. The annotations are not decoration: the API
/// registers this with ValidateDataAnnotations().ValidateOnStart(), so a missing or
/// too-short signing key stops the application at startup with a clear message
/// instead of throwing an obscure cryptography error on the first login attempt.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key. Must be at least 32 bytes because that is the output
    /// size of SHA-256 — a shorter key makes SymmetricSecurityKey throw.
    /// This value is a secret: it belongs in user secrets or environment variables,
    /// never in appsettings.json, which is committed to source control.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Key is not configured.")]
    [MinLength(32, ErrorMessage = "Jwt:Key must be at least 32 characters for HMAC-SHA256.")]
    public string Key { get; set; } = default!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Issuer is not configured.")]
    public string Issuer { get; set; } = default!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Audience is not configured.")]
    public string Audience { get; set; } = default!;

    [Range(1, 1440, ErrorMessage = "Jwt:ExpiryMinutes must be between 1 and 1440.")]
    public int ExpiryMinutes { get; set; } = 120;
}
