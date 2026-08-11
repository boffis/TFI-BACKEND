using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using GymManagement.Application.Exceptions;

namespace GymManagement.Presentation.Middlewares
{
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
            _logger.LogError(exception, "Unhandled error: {Message}", exception.Message);

            (int statusCode, string title, string safeMessage) = exception switch
            {
                UnauthorizedException => ((int)HttpStatusCode.Unauthorized, "Unauthorized", "Access denied."),
                ForbiddenException => ((int)HttpStatusCode.Forbidden, "Forbidden", "You don't have permission to perform this action."),
                ConflictException => ((int)HttpStatusCode.Conflict, "Conflict", "This action couldn't be completed."),
                NotFoundException => ((int)HttpStatusCode.NotFound, "Not found", "The requested resource was not found."),
                ValidationException => ((int)HttpStatusCode.BadRequest, "Validation error", "There was a validation error in the request."),
                _ => ((int)HttpStatusCode.InternalServerError, "Internal server error", "Something went wrong. Please try again later.")
            };

            // Only exceptions we deliberately throw with a short, user-facing message are ever
            // shown to the client verbatim. Anything else — unexpected .NET/third-party exceptions,
            // database errors, raw upstream API failures — could contain internal details or status
            // codes not meant for clients, so it always gets the generic safeMessage above. The full
            // detail is still captured server-side via the log line above.
            bool isKnownAppException = exception is UnauthorizedException
                or ForbiddenException
                or ConflictException
                or NotFoundException
                or ValidationException;

            string detail = isKnownAppException && !string.IsNullOrWhiteSpace(exception.Message)
                ? exception.Message
                : safeMessage;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
