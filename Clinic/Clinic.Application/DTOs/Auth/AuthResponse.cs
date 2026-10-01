namespace Clinic.Application.DTOs.Auth;

public record AuthResponse(string Token, DateTime ExpiresAtUtc);
