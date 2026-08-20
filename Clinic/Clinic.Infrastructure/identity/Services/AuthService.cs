using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Clinic.Infrastructure.identity.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        public AuthService(UserManager<AppUser> userManager, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }
        // Register User Service
        public async Task<AuthResult> RegisterUserAsync(RegisterRequest request)
        {
            _logger.LogInformation($"Try to Register user with email: {request.Email} and username: {request.UserName}");
            var user = new AppUser()
            {
                Email = request.Email,
                UserName = request.UserName,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogError($"Failed to Register user with email: {request.Email} and username: {request.UserName}. Errors: {string.Join(", ", errors)}");
                return new AuthResult() { Errors = errors, Success = false };
            }
            _logger.LogInformation($"User registered successfully with email: {request.Email} and username: {request.UserName}");
            return new AuthResult() { Success = true };

        }

        // Login User Service
        public async Task<AuthResult> LoginUserAsync(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            _logger.LogInformation($"Try to Login user with username: {request.UserName}");
            if (user == null)
            {
                _logger.LogWarning($"User not found with username: {request.UserName}");
                return new AuthResult() { Errors = new List<string>() { "User Not Found" }, Success = false, Token = "" };
            }
            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result)
            {
                _logger.LogWarning($"Invalid password for user with username: {request.UserName}");
                return new AuthResult() { Errors = new List<string>() { "Password is incorrect" }, Success = false, Token = "" };
            }
            _logger.LogInformation($"User logged in successfully with username: {request.UserName}");
            return new AuthResult() { Success = true };
        }


    }
}
