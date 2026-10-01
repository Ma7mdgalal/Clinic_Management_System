using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Auth;

public record LoginRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "UserName is required.")]
    public required string UserName { get; init; }

 
    [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required.")]
    public required string Password { get; init; }
}
