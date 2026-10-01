using Clinic.Application.Common;
using Clinic.Application.Common.Exceptions;
using Clinic.Application.DTOs.Auth;
using Clinic.Application.Interfaces.Services;
using Clinic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ValidationException = Clinic.Application.Common.Exceptions.ValidationException;

namespace Clinic.Infrastructure.identity.Services;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<AppUser> userManager,
        AppDbContext context,
        JwtService jwtService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            _logger.LogWarning("Registration rejected: email already registered. Email={Email}", request.Email);
            throw new ConflictException($"Email '{request.Email}' is already registered.");
        }

        if (await _userManager.FindByNameAsync(request.UserName) is not null)
        {
            _logger.LogWarning("Registration rejected: username taken. UserName={UserName}", request.UserName);
            throw new ConflictException($"Username '{request.UserName}' is already taken.");
        }


        await using var transaction = await _context.Database.BeginTransactionAsync();

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description).ToList();

            _logger.LogWarning(
                "Identity rejected registration. UserName={UserName} Errors={@Errors}",
                request.UserName, errors);

            throw new ValidationException("Registration failed.", errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, Roles.Patient);
        if (!roleResult.Succeeded)
        {

            var reasons = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Could not grant the '{Roles.Patient}' role during registration: {reasons}");
        }

        await transaction.CommitAsync();

        _logger.LogInformation(
            "User registered. UserId={UserId} UserName={UserName} Role={Role}",
            user.Id, user.UserName, Roles.Patient);

        var token = await _jwtService.GenerateTokenAsync(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user is null)
        {
            // Hash a throwaway password so an unknown username costs roughly the same
            // wall-clock time as a real password check. Without this, an attacker can
            // enumerate valid usernames purely from response latency, even though both
            // paths return the identical error message.
            _ = _userManager.PasswordHasher.HashPassword(new AppUser(), request.Password);

            _logger.LogWarning("Failed login: unknown username. UserName={UserName}", request.UserName);
            throw new InvalidCredentialsException();
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            _logger.LogWarning("Failed login: wrong password. UserId={UserId}", user.Id);
            throw new InvalidCredentialsException();
        }
        if (await _userManager.IsLockedOutAsync(user))
                throw new InvalidCredentialsException();
                _logger.LogWarning("Failed login: user locked out. UserId={UserId}", user.Id);

        // Both branches above throw the same exception with the same message. The old code
        // returned "User Not Found" versus "Password is incorrect", which let anyone
        // confirm which accounts exist before attempting a single password — the log
        // records the distinction, the response does not.

        _logger.LogInformation("User logged in. UserId={UserId}", user.Id);

        var token = await _jwtService.GenerateTokenAsync(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc);
    }

    public async Task AssignRoleAsync(string userId, string role)
    {
        // Validated first because it needs no database round-trip. NormalizeAssignable
        // also accepts "doctor" and returns the canonical "Doctor", so callers are not
        // punished for casing, and Admin is rejected: administrators come from the
        // bootstrap seeder, never from an HTTP call.
        var normalizedRole = Roles.NormalizeAssignable(role)
            ?? throw new ValidationException(
                $"'{role}' cannot be assigned. Allowed roles: {string.Join(", ", Roles.Assignable)}.");

        // The old version called IsInRoleAsync(user, role) without checking that
        // FindByIdAsync found anything, so an unknown id threw a NullReferenceException
        // and surfaced as an opaque 500 instead of a 404.
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw NotFoundException.For("User", userId);

        if (await _userManager.IsInRoleAsync(user, normalizedRole))
        {
            throw new ConflictException($"User already has the '{normalizedRole}' role.");
        }

        // The result is checked. Discarding it, as the old code did, made the method
        // report success for a role that was never granted.
        var result = await _userManager.AddToRoleAsync(user, normalizedRole);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning(
                "Role assignment failed. UserId={UserId} Role={Role} Errors={@Errors}",
                user.Id, normalizedRole, errors);

            throw new ValidationException("Could not assign the role.", errors);
        }

        _logger.LogInformation(
            "Role granted. UserId={UserId} Role={Role}", user.Id, normalizedRole);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("The authenticated user no longer exists.");

        var roles = await _userManager.GetRolesAsync(user);

        // A DTO, not the AppUser entity. Returning the entity would serialise
        // PasswordHash and SecurityStamp straight to the client.
        return new CurrentUserResponse(
            user.Id,
            user.UserName!,
            user.Email,
            roles.ToList());
    }

    public async Task<CurrentUserResponse> GetUserbyEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new NotFoundException("The authenticated user no longer exists.");

        var roles = await _userManager.GetRolesAsync(user);

        // A DTO, not the AppUser entity. Returning the entity would serialise
        // PasswordHash and SecurityStamp straight to the client.
        return new CurrentUserResponse(
            user.Id,
            user.UserName!,
            user.Email,
            roles.ToList());
    }
    public async Task AddUser(specificUserRequest request)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
        {
            throw new ConflictException($"Email '{request.Email}' is already registered.");
        }

        if (await _userManager.FindByNameAsync(request.UserName) is not null)
        {
            throw new ConflictException($"Username '{request.UserName}' is already taken.");
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors.Select(e => e.Description).ToList();
            throw new ValidationException("User creation failed.", errors);
        }
        var normalizedRole = Roles.NormalizeAssignable(request.Role)
                    ?? throw new ValidationException(
                        $"'{request.Role}' cannot be assigned. Allowed roles: {string.Join(", ", Roles.Assignable)}.");

        var roleResult = await _userManager.AddToRoleAsync(user, normalizedRole);
        if (!roleResult.Succeeded)
        {
            var reasons = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Could not grant the '{request.Role}' role during user creation: {reasons}");
        }
    }
    public async Task DisableUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw NotFoundException.For("User", userId);

        var result = await _userManager.SetLockoutEnabledAsync(user, true);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ValidationException("Could not disable the user.", errors);
        }

        // Optionally, you can also set a lockout end date to a far future date
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        _logger.LogInformation("User disabled. UserId={UserId}", user.Id);
    }
    public async Task EnableUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw NotFoundException.For("User", userId);

        var result = await _userManager.SetLockoutEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ValidationException("Could not enable the user.", errors);
        }

        // Optionally, you can also reset the lockout end date
        await _userManager.SetLockoutEndDateAsync(user, null);

        _logger.LogInformation("User enabled. UserId={UserId}", user.Id);
    }
    // public async Task<IReadOnlyList<string>> GetUserClaims()
    // {
    //     var claims = await _userManager.GetClaimsAsync(new AppUser());
    //     return claims.Select(c => c.Type).ToList();
    // }
}
