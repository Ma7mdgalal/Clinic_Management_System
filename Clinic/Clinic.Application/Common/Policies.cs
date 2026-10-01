namespace Clinic.Application.Common;

/// <summary>
/// Authorization policy names. Same reasoning as <see cref="Roles"/>: a mistyped
/// policy name in an [Authorize] attribute throws at runtime, not at compile time.
/// </summary>
public static class Policies
{
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireDoctor = nameof(RequireDoctor);
    public const string RequirePatient = nameof(RequirePatient);
    public const string RequireReceptionist = nameof(RequireReceptionist);

    /// <summary>Admin or receptionist — the two roles that handle front-desk operations.</summary>
    public const string RequireClinicStaff = nameof(RequireClinicStaff);
    public const string RequireDoctorOrAdmin = nameof(RequireDoctorOrAdmin);
}
