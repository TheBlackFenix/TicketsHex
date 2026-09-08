using Microsoft.AspNetCore.Mvc;

namespace TicketsHex.API.Middelwares.ExceptionHandling
{
    public sealed class ApiProblemDetailsWriter
    {
        private readonly ExceptionMessageResolver _resolver;
        private readonly IHostEnvironment _environment;

        public ApiProblemDetailsWriter(
            ExceptionMessageResolver resolver,
            IHostEnvironment environment)
        {
            _resolver = resolver;
            _environment = environment;
        }

        public Task WriteAsync(HttpContext context, Exception exception)
        {
            var options = _resolver.Resolve(exception);
            var detail = options.UseExceptionMessage
                ? exception.Message
                : options.Detail;
            var errors = exception is ArgumentException argumentException &&
                !string.IsNullOrWhiteSpace(argumentException.ParamName)
                ? new Dictionary<string, string[]>
                {
                    [argumentException.ParamName] = [detail]
                }
                : null;

            return WriteAsync(
                context,
                options,
                detail,
                errors,
                _environment.IsDevelopment() ? exception.GetType().Name : null);
        }

        public Task WriteAsync(HttpContext context, string codigo)
        {
            var options = _resolver.Resolve(codigo);
            return WriteAsync(context, options, options.Detail);
        }

        private static async Task WriteAsync(
            HttpContext context,
            ExceptionMessageOptions options,
            string detail,
            IReadOnlyDictionary<string, string[]>? errors = null,
            string? exceptionName = null)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.StatusCode = options.StatusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = options.StatusCode,
                Title = options.Title,
                Detail = detail,
                Instance = context.Request.Path
            };
            problemDetails.Extensions["code"] = options.Code;
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            if (errors is not null)
                problemDetails.Extensions["errors"] = errors;
            if (exceptionName is not null)
                problemDetails.Extensions["exception"] = exceptionName;

            await context.Response.WriteAsJsonAsync(
                problemDetails,
                cancellationToken: context.RequestAborted);
        }
    }
}
