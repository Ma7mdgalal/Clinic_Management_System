using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Auth;

public record AssignRoleRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Role is required.")]
    public required string Role { get; init; }
}
