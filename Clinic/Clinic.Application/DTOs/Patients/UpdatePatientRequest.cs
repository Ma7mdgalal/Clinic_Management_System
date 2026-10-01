

using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Patients;

public record UpdatePatientRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "First Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "First Name must be 2-100 characters.")]
    public required string FirstName { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Last Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Last Name must be 2-100 characters.")]
    public required string LastName { get; init; }
    [EmailAddress(ErrorMessage = "Enter Correct Email Address")]
    public required string Email { get; init; }

    [Phone(ErrorMessage = "Enter Valid Phone Number")]
    public required string Phone { get; set; }
    public required DateOnly DateOfBirth { get; init; }
    public required string Gender { get; init; }
    public required string Address { get; init; }
    public required string EmergencyContact { get; init; }
    public bool IsActive { get; set; } = true;

}
