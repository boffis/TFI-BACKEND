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
            _logger.LogError(exception, "Ha ocurrido un error no controlado: {Message}", exception.Message);

            (int statusCode, string title, string defaultMessage) = exception switch
            {
                UnauthorizedException => ((int)HttpStatusCode.Unauthorized, "No autorizado", "Acceso denegado."),
                ForbiddenException => ((int)HttpStatusCode.Forbidden, "Prohibido", "No tiene permisos para realizar esta acción."),
                ConflictException => ((int)HttpStatusCode.Conflict, "Conflicto", "Operación inválida."),
                NotFoundException => ((int)HttpStatusCode.NotFound, "Recurso no encontrado", "El recurso solicitado no fue encontrado."),
                ValidationException => ((int)HttpStatusCode.BadRequest, "Error de validacion", "Hubo uno o mas errores de validación en la solicitud."),
                _ => ((int)HttpStatusCode.InternalServerError, "Error interno del servidor", "Ha ocurrido un error interno.")
            };

            bool hasMeaningfulMessage = !string.IsNullOrWhiteSpace(exception.Message) && !exception.Message.Contains("Exception of type");
            string detail = hasMeaningfulMessage ? exception.Message : defaultMessage;

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
