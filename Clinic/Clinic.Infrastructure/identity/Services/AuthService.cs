using Clinic.Application.DTOs;
using Clinic.Application.Interfaces;
using Clinic.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;


namespace Clinic.Infrastructure.identity.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        public AuthService(UserManager<AppUser> userManager, ILogger<AuthService> logger, AppDbContext context, JwtService jwtService)
        {
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _jwtService = jwtService;
        }

        // Register User Service
        public async Task<AuthResult> RegisterUserAsync(RegisterRequest request)
        {
            _context.Database.BeginTransaction();
            _logger.LogInformation($"Database transaction started for user registration with email: {request.Email} and username: {request.UserName}");
            _logger.LogInformation($"Check if user with email: {request.Email} or username: {request.UserName} already exists");
            var existingUserByEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning($"User with email: {request.Email} already exists");
                return new AuthResult() { Errors = new List<string>() { $"Email {request.Email} is already taken" }, Success = false };
            }
            var existingUserByUsername = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserByUsername != null)
            {
                _logger.LogWarning($"User with username: {request.UserName} already exists");
                return new AuthResult() { Errors = new List<string>() { $"Username {request.UserName} is already taken" }, Success = false };
            }
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
            if(request.Role == null)
            {
                request.Role = "Patient";
            }
            if (!await _userManager.IsInRoleAsync(user, request.Role!))
            {
                // await _roleManager.CreateAsync(new ApplicationRole { Name = request.Role! });
                await _userManager.AddToRoleAsync(user, request.Role!);
            }
            _context.Database.CommitTransaction();
            _logger.LogInformation($"User registered successfully with email: {request.Email} and username: {request.UserName} and Transaction committed");
            var token = _jwtService.GenerateJwtToken(user);
            return new AuthResult() { Success = true, Token = token };

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
            var token = _jwtService.GenerateJwtToken(user);
            return new AuthResult() { Success = true, Token = token };
        }

        

    }

}

