using System.ComponentModel.DataAnnotations.Schema;
namespace Clinic.Domain.Models
{
public class Doctor
{

    public Guid Id { get; set; }

    // Referring to the User entity, assuming a one-to-one relationship
    public required string UserId { get; set; } 
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    // Referring to the Specialty entity, assuming a one-to-many relationship
    public Guid SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public required string LicenseNumber { get; set; } // Adding License Number
    public decimal ConsultationFee { get; set; } // Adding Consultation Fee
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
}
    

