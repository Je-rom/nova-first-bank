using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using NovaWallet.Exceptions;

namespace NovaWallet.Middleware
{
    /// <summary>
    /// Catches every unhandled exception and turns it into an RFC 7807
    /// ProblemDetails response. Known DomainException subclasses map to their
    /// own status code + type; anything else is logged and returned as a
    /// generic 500 so internals never leak to the client.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, problemType, title) = exception switch
            {
                DomainException domainEx => (domainEx.StatusCode, domainEx.ProblemType, domainEx.GetType().Name),
                _ => (StatusCodes.Status500InternalServerError, "internal-server-error", "An unexpected error occurred")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                // Only log full details for genuinely unexpected exceptions —
                // domain exceptions are expected control flow, not bugs.
                _logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
                Type = $"https://novawallet.firstbank.example/problems/{problemType}",
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true; // we handled it — don't let it bubble further
        }
    }
}