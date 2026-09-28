using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OSCU.Api.Middleware;

/// <summary>
/// Last line of defence: turns an unhandled exception into an RFC 7807
/// response carrying a reference the caller can quote to support.
/// </summary>
/// <remarks>
/// The source template put <c>exception.Message</c> straight into the
/// response <c>Detail</c>. That hands connection strings, file paths and
/// internal type names to whoever triggered the error. Here the message goes
/// to the log — where Serilog ships it to the ELK stack — and the caller gets
/// only the correlating reference.
/// </remarks>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Prefer the ambient trace id so the reference lines up with
        // distributed tracing in Application Insights.
        string reference = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception {Reference} on {Method} {Path}.",
            reference, httpContext.Request.Method, httpContext.Request.Path);

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred. Quote the reference below when reporting it.",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Extensions = { ["reference"] = reference }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
