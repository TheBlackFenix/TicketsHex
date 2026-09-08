using TicketsHex.API.Middelwares.ExceptionHandling;
using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.API.Middelwares
{
    public sealed class CambioContrasenaObligatorioMiddleware
    {
        public const string ClaimName = "password_change_required";

        private static readonly PathString RutaCambio = new("/api/auth/cambiar-contrasena");
        private static readonly PathString RutaLogin = new("/api/auth/login");
        private static readonly PathString RutaInicializacion = new("/api/auth/inicializar");
        private static readonly PathString RutaHealth = new("/health");
        private static readonly PathString RutaJwks = new("/.well-known/jwks.json");
        private static readonly PathString RutaSwagger = new("/swagger");
        private readonly RequestDelegate _next;

        public CambioContrasenaObligatorioMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ApiProblemDetailsWriter problemDetailsWriter)
        {
            var requiereCambio = context.User.Identity?.IsAuthenticated == true &&
                string.Equals(
                    context.User.FindFirst(ClaimName)?.Value,
                    bool.TrueString,
                    StringComparison.OrdinalIgnoreCase);

            if (!requiereCambio || EsRutaPermitida(context.Request.Path))
            {
                await _next(context);
                return;
            }

            await problemDetailsWriter.WriteAsync(
                context,
                CodigosError.CambioContrasenaRequerido);
        }

        private static bool EsRutaPermitida(PathString ruta) =>
            ruta.Equals(RutaCambio) ||
            ruta.Equals(RutaLogin) ||
            ruta.Equals(RutaInicializacion) ||
            ruta.Equals(RutaHealth) ||
            ruta.Equals(RutaJwks) ||
            ruta.StartsWithSegments(RutaSwagger);
    }
}
