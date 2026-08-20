using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Application.DTOs
{
    public record AuthResult
{
        public string Token { get; set; }= string.Empty;
        public bool Success { get; set; } 
        public List<string>? Errors { get; set; } = new List<string>();
    }
}
