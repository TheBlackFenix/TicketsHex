using Microsoft.AspNetCore.Mvc;

namespace TicketsHex.API.Middelwares
{
    public sealed class CambioContrasenaObligatorioMiddleware
    {
        public const string ClaimName = "password_change_required";

        private static readonly PathString RutaCambio = new("/api/auth/cambiar-contrasena");
        private static readonly PathString RutaLogout = new("/api/auth/logout");
        private readonly RequestDelegate _next;

        public CambioContrasenaObligatorioMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requiereCambio = context.User.Identity?.IsAuthenticated == true &&
                string.Equals(
                    context.User.FindFirst(ClaimName)?.Value,
                    bool.TrueString,
                    StringComparison.OrdinalIgnoreCase);

            if (!requiereCambio ||
                context.Request.Path.Equals(RutaCambio) ||
                context.Request.Path.Equals(RutaLogout))
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Cambio de contraseña requerido",
                Detail = "Debe cambiar la contraseña antes de utilizar el resto de la API.",
                Instance = context.Request.Path,
                Extensions =
                {
                    ["code"] = "PASSWORD_CHANGE_REQUIRED",
                    ["traceId"] = context.TraceIdentifier
                }
            });
        }
    }
}
