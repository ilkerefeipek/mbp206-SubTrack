using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SubTrack.Domain.Common.Exceptions;
using DomainValidationException = SubTrack.Domain.Common.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace SubTrack.Api.Middleware;

/// <summary>
/// Maps domain exceptions and uncaught framework exceptions to RFC 7807
/// ProblemDetails responses. Stack traces are emitted only in Development.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, problem) = Map(exception);

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception: {Type}", exception.GetType().Name);
        }
        else
        {
            logger.LogInformation(
                "Handled exception {Type} -> {Status}: {Message}",
                exception.GetType().Name,
                status,
                exception.Message);
        }

        if (env.IsDevelopment() && status >= 500)
        {
            problem.Extensions["stackTrace"] = exception.StackTrace;
        }

        problem.Instance = httpContext.Request.Path;

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, ProblemDetails Problem) Map(Exception ex) => ex switch
    {
        DomainValidationException dve => (StatusCodes.Status400BadRequest, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Validation failed",
            Detail = dve.Message,
            Status = StatusCodes.Status400BadRequest,
            Extensions = { ["errors"] = dve.Errors }
        }),

        FluentValidationException fve => (StatusCodes.Status400BadRequest, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Validation failed",
            Detail = "Bir veya daha fazla doğrulama hatası oluştu.",
            Status = StatusCodes.Status400BadRequest,
            Extensions =
            {
                ["errors"] = fve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray())
            }
        }),

        EntityNotFoundException nfe => (StatusCodes.Status404NotFound, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Resource not found",
            Detail = nfe.Message,
            Status = StatusCodes.Status404NotFound
        }),

        ConflictException ce => (StatusCodes.Status409Conflict, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Conflict",
            Detail = ce.Message,
            Status = StatusCodes.Status409Conflict
        }),

        InvalidCredentialsException ice => (StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Unauthorized",
            Detail = ice.Message,
            Status = StatusCodes.Status401Unauthorized
        }),

        UnauthorizedException ue => (StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Unauthorized",
            Detail = ue.Message,
            Status = StatusCodes.Status401Unauthorized
        }),

        ForbiddenException fe => (StatusCodes.Status403Forbidden, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Forbidden",
            Detail = fe.Message,
            Status = StatusCodes.Status403Forbidden
        }),

        _ => (StatusCodes.Status500InternalServerError, new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred while processing the request.",
            Status = StatusCodes.Status500InternalServerError
        })
    };
}
