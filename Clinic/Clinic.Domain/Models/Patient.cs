namespace Clinic.Domain.Models
{
public class Patient
{
    public Guid Id { get; set; }
    public string? UserId { get; set; } // Assuming a reference to the User entity
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? ContactNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // RElationships
    
}
}
    

