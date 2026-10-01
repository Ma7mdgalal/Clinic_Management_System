namespace Clinic.Domain.Models;

public class Patient
{
    public Guid Id { get; set; }

    // Nullable by design: README 6 lets a receptionist register a patient at the desk.
    // Such a patient has clinic records but no login until they are given an account.
    public string? UserId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    // DateOnly, not DateTime: a birth date has no time component and no time zone.
    // EF Core maps this to SQL Server 'date'.
    public DateOnly DateOfBirth { get; set; }

    public string? Gender { get; set; }

    // Phone is the clinic's primary way to reach a patient, so it is required.
    // Email is optional because walk-in patients often do not have one.
    public required string Phone { get; set; }
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? EmergencyContact { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
