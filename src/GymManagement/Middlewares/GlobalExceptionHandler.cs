using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
            _logger.LogError(exception, "Ha ocurrido un error no controlado: {Message}", exception.Message);

            (int statusCode, string title) = exception switch
            {
                UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, "No autorizado"),
                InvalidOperationException => ((int)HttpStatusCode.BadRequest, "Operación inválida"),
                KeyNotFoundException => ((int)HttpStatusCode.NotFound, "Recurso no encontrado"),
                _ => ((int)HttpStatusCode.InternalServerError, "Error interno del servidor")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
