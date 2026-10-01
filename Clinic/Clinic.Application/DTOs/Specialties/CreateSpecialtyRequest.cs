using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs.Specialties;


public record CreateSpecialtyRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters.")]
    public required string Name { get; init; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; init; }
}
