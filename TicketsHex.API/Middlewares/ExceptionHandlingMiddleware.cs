using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TicketsHex.API.Middelwares.ExceptionHandling;

namespace TicketsHex.API.Middelwares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly ExceptionMessageResolver _resolver;
        private readonly IHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            ExceptionMessageResolver resolver,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _resolver = resolver;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Ocurrió un error no controlado en {Method} {Path}. Message: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    exception.Message);

                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("La respuesta ya había iniciado. No se pudo escribir ProblemDetails.");
                return;
            }

            var exceptionOptions = _resolver.Resolve(exception);

            var detail = exceptionOptions.UseExceptionMessage
                ? exception.Message
                : exceptionOptions.Detail;

            context.Response.Clear();
            context.Response.StatusCode = exceptionOptions.StatusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = exceptionOptions.StatusCode,
                Title = exceptionOptions.Title,
                Detail = detail,
                Instance = context.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            if (_environment.IsDevelopment())
            {
                problemDetails.Extensions["exception"] = exception.GetType().Name;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonResponse = JsonSerializer.Serialize(problemDetails, jsonOptions);

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
