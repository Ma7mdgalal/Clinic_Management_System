using Clinic.Application.DTOs.Auth;

namespace Clinic.Application.Interfaces.Services;

/// <summary>
/// Authentication and role management (CMS-005 … CMS-009).
/// </summary>
/// <remarks>
/// User ids are strings, not Guids. ASP.NET Core Identity's default key type here is
/// string; the previous Guid signatures forced a ToString() at every boundary and would
/// silently reject any id Identity did not happen to format as a bare GUID.
/// </remarks>
public interface IAuthService
{
    /// <summary>
    /// Registers a new account and grants it the Patient role. Self-registration can
    /// never produce a privileged account.
    /// </summary>
    /// <exception cref="Common.Exceptions.ConflictException">Username or email already taken.</exception>
    /// <exception cref="Common.Exceptions.ValidationException">Identity rejected the password or user data.</exception>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <exception cref="Common.Exceptions.InvalidCredentialsException">
    /// Unknown user, wrong password, locked out, or deactivated — all reported identically.
    /// </exception>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <exception cref="Common.Exceptions.NotFoundException">No such user.</exception>
    /// <exception cref="Common.Exceptions.ValidationException">Role is not one an admin may grant.</exception>
    /// <exception cref="Common.Exceptions.ConflictException">User already holds the role.</exception>
    Task AssignRoleAsync(string userId, string role);

    /// <exception cref="Common.Exceptions.NotFoundException">
    /// The token is valid but its subject no longer exists — e.g. the account was deleted
    /// after the token was issued.
    /// </exception>
    Task<CurrentUserResponse> GetCurrentUserAsync(string userId);

    Task<CurrentUserResponse> GetUserbyEmailAsync(string email);
    Task AddUser(specificUserRequest request);
    // Task<IReadOnlyList<string>> GetUserClaims();

    Task DisableUserAsync(string userId);
    Task EnableUserAsync(string userId);




    


}
