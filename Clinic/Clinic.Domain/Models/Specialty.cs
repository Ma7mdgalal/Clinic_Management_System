using Clinic.Domain.Models;
namespace Clinic.Domain.Models
{

public class Specialty
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    // Navigation property for the one-to-many relationship with Doctor
    public ICollection<Doctor>? Doctors { get; set; } = new List<Doctor>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}