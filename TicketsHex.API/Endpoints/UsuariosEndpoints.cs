using Microsoft.OpenApi.Models;
using TicketsHex.API.Reponses;
using TicketsHex.API.Servicios;
using TicketsHex.Application.DTO_s.Usuario;
using TicketsHex.Application.Puertos.Entrada.Usuario;

namespace TicketsHex.API.Endpoints
{
    public static class UsuariosEndpoints
    {
        public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/usuarios")
                .WithTags("Usuarios")
                .WithOpenApi(operation =>
                {
                    // Añadir el primer header
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = "X-User-Id",
                        In = ParameterLocation.Header,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = "Descripción del primer header"
                    });

                    // Añadir el segundo header
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = "X-User-Role",
                        In = ParameterLocation.Header,
                        Required = true,
                        Schema = new OpenApiSchema { Type = "string" },
                        Description = "Descripción del segundo header"
                    });

                    return operation;
                })
                .ConUsuarioActualTemporal();

            group.MapGet("/", async (bool incluirInactivos, IUsuarioService service) =>
            {
                var usuarios = await service.ObtenerTodosAsync(incluirInactivos);
                return Results.Ok(ApiResponse<IReadOnlyCollection<UsuarioDTO>>.Ok(usuarios));
            });

            group.MapGet("/{id:long}", async (long id, IUsuarioService service) =>
            {
                var usuario = await service.ObtenerPorIdAsync(id);
                return Results.Ok(ApiResponse<UsuarioDTO>.Ok(usuario));
            });

            group.MapPost("/", async (CrearUsuarioRequest request, IUsuarioService service) =>
            {
                await service.CrearAsync(request);
                return Results.Created(
                    $"/api/usuarios/{request.IdUsuario}",
                    ApiResponse<long>.Ok(request.IdUsuario, "Usuario creado correctamente."));
            });

            group.MapPut("/{id:long}", async (
                long id,
                ActualizarUsuarioRequest request,
                IUsuarioService service) =>
            {
                await service.ActualizarAsync(id, request);
                return Results.Ok(ApiResponse<bool>.Ok(true, "Usuario actualizado correctamente."));
            });

            group.MapDelete("/{id:long}", async (long id, IUsuarioService service) =>
            {
                await service.DesactivarAsync(id);
                return Results.Ok(ApiResponse<bool>.Ok(true, "Usuario desactivado correctamente."));
            });

            return app;
        }
    }
}
