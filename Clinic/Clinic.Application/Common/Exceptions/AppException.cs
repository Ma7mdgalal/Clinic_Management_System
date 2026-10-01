namespace Clinic.Application.Common.Exceptions;

/// <summary>
/// Base type for failures that are the caller's fault and map to a specific HTTP status.
/// The API's exception handler translates these; anything not derived from this type is
/// treated as an unexpected server fault and reported as 500 with no internal detail.
///
/// One error model for the whole application. The alternative — services returning
/// result objects that every controller has to unpack and translate — spreads
/// status-code decisions across dozens of action methods, and they drift apart.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>
    /// Extra messages when a single sentence is not enough. ASP.NET Core Identity, for
    /// example, can reject one password for several reasons at once, and the caller
    /// needs to see all of them to fix the request in one attempt.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    protected AppException(string message, IReadOnlyList<string>? errors = null)
        : base(message)
    {
        Errors = errors ?? [];
    }
}

/// <summary>The requested resource does not exist. Maps to 404.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }

    public static NotFoundException For(string resource, object key) =>
        new($"{resource} with id '{key}' was not found.");
}

/// <summary>
/// The request conflicts with the current state of the resource — a duplicate unique
/// value, or an operation that is invalid for the resource's current state. Maps to 409.
/// </summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>
/// The request is well-formed but semantically invalid in a way model binding cannot
/// express. Maps to 400. Prefer data annotations for simple per-field rules; use this
/// for rules that need the database or other services to evaluate.
/// </summary>
public sealed class ValidationException : AppException
{
    public ValidationException(string message) : base(message) { }

    public ValidationException(string message, IReadOnlyList<string> errors)
        : base(message, errors) { }
}

/// <summary>
/// The credentials supplied were not accepted. Maps to 401.
///
/// The message is intentionally fixed and generic. Telling a caller "user not found"
/// versus "wrong password" turns the login endpoint into a free account-enumeration
/// oracle: an attacker learns which usernames exist before trying a single password.
/// </summary>
public sealed class InvalidCredentialsException : AppException
{
    public InvalidCredentialsException()
        : base("Invalid username or password.") { }
}
