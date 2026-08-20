using System.ComponentModel.DataAnnotations;

namespace Clinic.Application.DTOs
{
    public record RegisterRequest
    {
        public required string UserName { get; set; } 
        public required string Password { get; set; }

        [EmailAddress]
        public required string Email { get; set; }  
    }
}
