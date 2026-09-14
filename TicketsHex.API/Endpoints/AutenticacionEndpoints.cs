using TicketsHex.API.Reponses;
using System.IdentityModel.Tokens.Jwt;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TicketsHex.API.Servicios;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Application.Comun.Configuracion;

namespace TicketsHex.API.Endpoints
{
    public static class AutenticacionEndpoints
    {
        private const string RefreshCookieName = "ticketshex_refresh";
        private const string RefreshRequestHeader = "X-Refresh-Request";

        public static IEndpointRouteBuilder MapAutenticacionEndpoints(
            this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Autenticación")
                .WithOpenApi();

            app.MapGet("/.well-known/jwks.json", (RSA publicKey) =>
            {
                var parameters = publicKey.ExportParameters(includePrivateParameters: false);
                return Results.Ok(new
                {
                    keys = new[]
                    {
                        new
                        {
                            kty = "RSA",
                            use = "sig",
                            alg = "RS256",
                            kid = JwtKeyLoader.CrearKeyId(publicKey),
                            n = Base64UrlEncoder.Encode(parameters.Modulus),
                            e = Base64UrlEncoder.Encode(parameters.Exponent)
                        }
                    }
                });
            })
            .AllowAnonymous()
            .WithTags("Autenticación")
            .WithOpenApi();

            group.MapPost("/inicializar", async (
                InicializarAutenticacionRequest request,
                IAutenticacionService service) =>
            {
                await service.InicializarAsync(request);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Autenticación inicializada correctamente."));
            });

            group.MapPost("/login", async (
                LoginRequest request,
                IAutenticacionService service,
                HttpContext context,
                RefreshOptions refreshOptions) =>
            {
                var resultado = await service.IniciarSesionAsync(request);
                EscribirCookieRefresh(context, resultado, refreshOptions);
                return Results.Ok(ApiResponse<LoginResponse>.Ok(
                    resultado,
                    "Sesión iniciada correctamente."));
            });

            group.MapPost("/refresh", async (
                HttpContext context,
                IAutenticacionService service,
                RefreshOptions refreshOptions) =>
            {
                if (context.Request.Headers[RefreshRequestHeader] != "1")
                    throw new ArgumentException("Falta la cabecera X-Refresh-Request.");

                var refreshToken = context.Request.Cookies[RefreshCookieName] ?? string.Empty;
                var resultado = await service.RenovarSesionAsync(refreshToken);
                EscribirCookieRefresh(context, resultado, refreshOptions);
                return Results.Ok(ApiResponse<LoginResponse>.Ok(
                    resultado,
                    "Sesión renovada correctamente."));
            }).AllowAnonymous();

            group.MapPost("/cambiar-contrasena", async (
                CambiarContrasenaRequest request,
                IAutenticacionService service,
                IUsuarioActual usuarioActual,
                HttpContext context,
                RefreshOptions refreshOptions) =>
            {
                await service.CambiarContrasenaAsync(usuarioActual.IdUsuario, request);
                BorrarCookieRefresh(context, refreshOptions);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Contraseña actualizada correctamente."));
            }).RequireAuthorization();

            group.MapPost("/logout", async (
                HttpContext context,
                IAutenticacionService service,
                RefreshOptions refreshOptions) =>
            {
                var jti = context.User.Identity?.IsAuthenticated == true
                    ? context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                    : null;
                if (!string.IsNullOrWhiteSpace(jti))
                    await service.CerrarSesionAsync(jti);
                else
                {
                    if (context.Request.Headers[RefreshRequestHeader] != "1")
                        throw new UsuarioNoAutenticadoException(
                            "La sesión no es válida o expiró.",
                            TicketsHex.Domain.Comun.Errores.CodigosError.SesionInvalida);
                    await service.CerrarSesionConRefreshAsync(
                        context.Request.Cookies[RefreshCookieName] ?? string.Empty);
                }
                BorrarCookieRefresh(context, refreshOptions);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Sesión cerrada correctamente."));
            }).AllowAnonymous();

            return app;
        }

        private static void EscribirCookieRefresh(
            HttpContext context,
            LoginResponse resultado,
            RefreshOptions options)
        {
            if (string.IsNullOrWhiteSpace(resultado.RefreshToken) ||
                resultado.FechaExpiracionRefresh is null)
                throw new InvalidOperationException("No fue posible emitir la credencial de renovación.");

            context.Response.Cookies.Append(
                RefreshCookieName,
                resultado.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = options.CookieSecure,
                    SameSite = Enum.Parse<SameSiteMode>(options.CookieSameSite, true),
                    Path = "/api/auth",
                    Expires = resultado.FechaExpiracionRefresh.Value
                });
            context.Response.Headers.CacheControl = "no-store";
        }

        private static void BorrarCookieRefresh(HttpContext context, RefreshOptions options) =>
            context.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                Path = "/api/auth",
                Secure = options.CookieSecure,
                SameSite = Enum.Parse<SameSiteMode>(options.CookieSameSite, true)
            });
    }
}
