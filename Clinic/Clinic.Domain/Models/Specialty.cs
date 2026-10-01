namespace Clinic.Domain.Models;

public class Specialty
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }

    // New specialties are usable immediately; deactivation is an explicit admin action.
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // One specialty has many doctors. Never null: an empty list means "no doctors yet".
    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
