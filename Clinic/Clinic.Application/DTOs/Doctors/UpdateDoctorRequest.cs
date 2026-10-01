using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Doctors;

public record UpdateDoctorRequest
{

    public string Email { get; init; } = default!;

    [Phone(ErrorMessage = "Enter Valid Phone Number")]
    public required string Phone { get; set; }
    public Guid SpecialtyId { get; set; }
    public required string LicenseNumber { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsActive { get; set; } = true;
}