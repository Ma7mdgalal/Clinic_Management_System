namespace Clinic.Application.DTOs.Auth;

public record CurrentUserResponse(
    string Id,
    string UserName,
    string? Email,
    IReadOnlyList<string> Roles);
