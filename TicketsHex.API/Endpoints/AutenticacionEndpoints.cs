using TicketsHex.API.Reponses;
using System.IdentityModel.Tokens.Jwt;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TicketsHex.API.Servicios;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.API.Endpoints
{
    public static class AutenticacionEndpoints
    {
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
                IAutenticacionService service) =>
            {
                var resultado = await service.IniciarSesionAsync(request);
                return Results.Ok(ApiResponse<LoginResponse>.Ok(
                    resultado,
                    "Sesión iniciada correctamente."));
            });

            group.MapPost("/cambiar-contrasena", async (
                CambiarContrasenaRequest request,
                IAutenticacionService service,
                IUsuarioActual usuarioActual) =>
            {
                await service.CambiarContrasenaAsync(usuarioActual.IdUsuario, request);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Contraseña actualizada correctamente."));
            }).RequireAuthorization();

            group.MapPost("/logout", async (
                HttpContext context,
                IAutenticacionService service) =>
            {
                var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                    ?? throw new UsuarioNoAutenticadoException(
                        "El token no contiene un identificador de sesión.");
                await service.CerrarSesionAsync(jti);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Sesión cerrada correctamente."));
            }).RequireAuthorization();

            return app;
        }
    }
}
