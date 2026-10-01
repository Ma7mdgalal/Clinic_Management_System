namespace Clinic.Application.Common;

/// <summary>
/// The first administrator account, created at startup if it does not exist.
/// </summary>
/// <remarks>
/// This exists to break a real deadlock in the design: every admin-only endpoint needs
/// an Admin to call it, self-registration deliberately only ever produces a Patient, and
/// the role seed creates roles but no users. Without a bootstrap step there is no way for
/// the first administrator to come into existence, so CMS-010 could never be exercised.
///
/// UserName and Email belong in appsettings.json. <see cref="Password"/> is a credential
/// and must come from user secrets in development or an environment variable / key vault
/// in production — never from a committed file. If it is absent the seeder logs a warning
/// and skips, rather than inventing a default password that would ship as a known
/// back door.
/// </remarks>
public class BootstrapAdminSettings
{
    public const string SectionName = "BootstrapAdmin";

    public string UserName { get; set; } = "admin";
    public string Email { get; set; } = "admin@clinic.local";
    public string? Password { get; set; } // // ChangeMe1Admin
}
