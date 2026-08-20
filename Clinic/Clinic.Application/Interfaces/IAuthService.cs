using Clinic.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterUserAsync(RegisterRequest request);
        Task<AuthResult> LoginUserAsync(LoginRequest request);
    }
}
