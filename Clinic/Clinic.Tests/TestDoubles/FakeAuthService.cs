using Clinic.Application.Common.Exceptions;
using Clinic.Application.DTOs.Auth;
using Clinic.Application.Interfaces.Services;

namespace Clinic.Tests.TestDoubles;

/// <summary>
/// In-memory <see cref="IAuthService"/> that records every account it is asked to create.
///
/// The recorded <see cref="CreatedAccount.Password"/> is the whole point: the README
/// forbids passwords derived from profile data, and the only way to assert that from a
/// unit test is to capture what the service actually handed to Identity.
///
/// It shares a <see cref="FakeTransaction"/> with the repository so account creation is
/// undone on rollback, which is what makes the orphan-account tests meaningful.
/// </summary>
public sealed class FakeAuthService : IAuthService
{
    public FakeAuthService(FakeTransaction? transaction = null)
    {
        Transaction = transaction ?? new FakeTransaction();
    }

    public FakeTransaction Transaction { get; }

    public sealed record CreatedAccount(string UserName, string Email, string Password, string Role);

    public List<CreatedAccount> Accounts { get; } = [];

    /// <summary>Set to make <see cref="AddUser"/> fail, e.g. to simulate a duplicate email.</summary>
    public Exception? AddUserThrows { get; set; }

    public Task AddUser(specificUserRequest request)
    {
        if (AddUserThrows is not null)
        {
            throw AddUserThrows;
        }

        var account = new CreatedAccount(
            request.UserName, request.Email, request.Password, request.Role);

        Accounts.Add(account);
        Transaction.OnRollback(() => Accounts.Remove(account));

        return Task.CompletedTask;
    }

    public Task<CurrentUserResponse> GetUserbyEmailAsync(string email)
    {
        var account = Accounts.SingleOrDefault(
            a => string.Equals(a.Email, email, StringComparison.OrdinalIgnoreCase))
            ?? throw new NotFoundException($"No account for '{email}'.");

        // A deterministic id derived from the email keeps assertions readable.
        return Task.FromResult(new CurrentUserResponse(
            $"user-{account.UserName}", account.UserName, account.Email, [account.Role]));
    }

    // Not exercised by these tests. Throwing rather than returning a default makes an
    // accidental dependency obvious instead of silently passing.
    public Task<AuthResponse> RegisterAsync(RegisterRequest request) =>
        throw new NotSupportedException();

    public Task<AuthResponse> LoginAsync(LoginRequest request) =>
        throw new NotSupportedException();

    public Task AssignRoleAsync(string userId, string role) =>
        throw new NotSupportedException();

    public Task<CurrentUserResponse> GetCurrentUserAsync(string userId) =>
        throw new NotSupportedException();
}
