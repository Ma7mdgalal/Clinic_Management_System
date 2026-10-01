using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Doctors;

public record CreateDoctorRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "UserName Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "UserName must be 2-100 characters.")]
    public required string UserName { get; init; }

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

    public Guid SpecialtyId { get; set; }
    public required string LicenseNumber { get; set; }
    public decimal ConsultationFee { get; set; }

    public bool IsActive { get; set; } = true;


}