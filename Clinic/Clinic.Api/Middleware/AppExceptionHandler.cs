using Clinic.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Clinic.Api.Middleware;

/// <summary>
/// Single place where an exception becomes an HTTP response (README 19).
/// </summary>
/// <remarks>
/// Before this existed, services threw ArgumentNullException for a missing record and
/// InvalidOperationException for a duplicate name. Neither is an AppException, so both
/// surfaced as 500 Internal Server Error — the API told every client "I am broken" when
/// the correct answers were 404 and 409. Controllers then had to guess at status codes
/// from bool and null return values, and the guesses drifted apart between endpoints.
///
/// The rule here: a known AppException carries a message written for the caller, and it
/// is safe to return. Anything else is a bug, gets logged with its stack trace, and the
/// caller receives a generic 500 — an unexpected exception message can contain
/// connection strings, file paths, or SQL, and must never reach the client.
/// </remarks>
public sealed class AppExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<AppExceptionHandler> _logger;

    public AppExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<AppExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Translate(exception);

        if (exception is AppException)
        {
            // Expected outcome, not a fault: log it as a warning, without a stack trace.
            _logger.LogWarning(
                "Request failed. Method={Method} Path={Path} Status={Status} Reason={Reason}",
                httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception. Method={Method} Path={Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        // The trace id lets a user quote one value from an error response and have it
        // matched against the server logs.
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is AppException { Errors.Count: > 0 } appException)
        {
            problemDetails.Extensions["errors"] = appException.Errors;
        }

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static (int Status, string Title, string Detail) Translate(Exception exception) =>
        exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Authentication failed", exception.Message),

            // Deliberately generic. The real message and stack trace go to the log only.
            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred",
                  "The request could not be completed. Quote the traceId when reporting this.")
        };
}
