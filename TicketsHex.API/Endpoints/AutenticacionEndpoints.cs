using TicketsHex.API.Reponses;
using TicketsHex.API.Servicios;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;

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
                IAutenticacionService service) =>
            {
                await service.CambiarContrasenaAsync(request);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Contraseña actualizada correctamente."));
            });

            group.MapPost("/logout", async (
                HttpRequest request,
                IAutenticacionService service) =>
            {
                var token = UsuarioActualEndpointExtensions.ObtenerTokenBearer(request);
                await service.CerrarSesionAsync(token);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Sesión cerrada correctamente."));
            });

            return app;
        }
    }
}
