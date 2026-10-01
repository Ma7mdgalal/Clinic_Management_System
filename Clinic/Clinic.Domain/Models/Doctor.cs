namespace Clinic.Domain.Models;

public class Doctor
{
    public Guid Id { get; set; }

    // A doctor always signs in, so the Identity account is mandatory.
    public required string UserId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    // README 5.2 lists Email and Phone on the doctor record. They are duplicated from
    // the Identity account on purpose: this is clinic contact data that staff maintain,
    // and it must not change just because someone edits their login email.
    public required string Email { get; set; }
    public required string Phone { get; set; }

    public Guid SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }

    public required string LicenseNumber { get; set; }
    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
