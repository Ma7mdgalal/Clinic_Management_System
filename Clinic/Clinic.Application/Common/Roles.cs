namespace Clinic.Application.Common;


public static class Roles
{
    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";
    public const string Receptionist = "Receptionist";

    public static readonly IReadOnlyList<string> All = [Admin, Doctor, Patient, Receptionist];

    /// <summary>
    /// Roles an admin may grant through the API. Admin is deliberately excluded:
    /// administrators are created by the bootstrap seeder, not promoted over HTTP.
    /// </summary>
    public static readonly IReadOnlyList<string> Assignable = [Doctor, Patient, Receptionist];

    public static bool IsAssignable(string role) =>
        Assignable.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));

    /// <summary>Returns the canonical casing for a role, or null if it is not assignable.</summary>
    public static string? NormalizeAssignable(string role) =>
        Assignable.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
}
