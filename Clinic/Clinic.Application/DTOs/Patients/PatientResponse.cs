namespace Clinic.Application.DTOs.Patients;

public record PatientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    DateOnly DateOfBirth,
    string Gender,
    string Address,
    string EmergencyContact,
    bool IsActive,
    DateTime CreatedAt
);
