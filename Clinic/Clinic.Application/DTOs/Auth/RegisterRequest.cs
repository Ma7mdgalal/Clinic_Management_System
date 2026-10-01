using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Auth;

public record RegisterRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "UserName is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "UserName must be 3-50 characters.")]
    public required string UserName { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
    [StringLength(256)]
    public required string Email { get; init; }

  
    [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public required string Password { get; init; }

}
