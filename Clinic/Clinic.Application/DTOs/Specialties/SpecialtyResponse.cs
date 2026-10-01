namespace Clinic.Application.DTOs.Specialties;

public record SpecialtyResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);
