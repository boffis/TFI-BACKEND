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
                BillingUnavailableException => ((int)HttpStatusCode.BadGateway, "Bad gateway", "The payment provider is unavailable. Please try again later."),
                _ => ((int)HttpStatusCode.InternalServerError, "Internal server error", "Something went wrong. Please try again later.")
            };

            // Only our own exceptions carry a message safe to show verbatim; anything else could
            // leak internal detail, so it gets safeMessage. The full detail is logged above.
            bool isKnownAppException = exception is UnauthorizedException
                or ForbiddenException
                or ConflictException
                or NotFoundException
                or ValidationException
                or BillingUnavailableException;

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
