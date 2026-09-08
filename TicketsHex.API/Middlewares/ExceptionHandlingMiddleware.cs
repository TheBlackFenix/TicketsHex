using TicketsHex.API.Middelwares.ExceptionHandling;

namespace TicketsHex.API.Middelwares
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly ApiProblemDetailsWriter _problemDetailsWriter;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            ApiProblemDetailsWriter problemDetailsWriter)
        {
            _next = next;
            _logger = logger;
            _problemDetailsWriter = problemDetailsWriter;
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

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        "La respuesta ya había iniciado. No se pudo escribir ProblemDetails.");
                    return;
                }

                await _problemDetailsWriter.WriteAsync(context, exception);
            }
        }
    }
}
