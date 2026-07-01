using TicketsHex.API.Reponses;
using TicketsHex.Application.DTO_s.Parametro;
using TicketsHex.Application.Puertos.Entrada.Parametro;

namespace TicketsHex.API.Endpoints
{
    public static class ParametrosEndpoints
    {
        public static IEndpointRouteBuilder MapParametrosEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/parametros")
                .WithTags("Parámetros")
                .WithOpenApi()
                .RequireAuthorization();

            group.MapGet("/roles", async (IParametroQuery query) =>
            {
                var resultado = await query.ObtenerRolesAsync();
                return Results.Ok(ApiResponse<IReadOnlyCollection<ParametroDTO>>.Ok(
                    resultado,
                    "Roles consultados correctamente."));
            });

            group.MapGet("/estados-ticket", async (
                bool? incluirInactivos,
                IParametroQuery query) =>
            {
                var resultado = await query.ObtenerEstadosTicketAsync(incluirInactivos ?? false);
                return Results.Ok(ApiResponse<IReadOnlyCollection<ParametroDTO>>.Ok(
                    resultado,
                    "Estados de ticket consultados correctamente."));
            });

            group.MapGet("/origenes-ticket", async (
                bool? incluirInactivos,
                IParametroQuery query) =>
            {
                var resultado = await query.ObtenerOrigenesTicketAsync(incluirInactivos ?? false);
                return Results.Ok(ApiResponse<IReadOnlyCollection<ParametroDTO>>.Ok(
                    resultado,
                    "Orígenes de ticket consultados correctamente."));
            });

            group.MapGet("/areas-ticket", async (
                bool? incluirInactivos,
                IParametroQuery query) =>
            {
                var resultado = await query.ObtenerAreasTicketAsync(incluirInactivos ?? false);
                return Results.Ok(ApiResponse<IReadOnlyCollection<ParametroDTO>>.Ok(
                    resultado,
                    "Áreas de ticket consultadas correctamente."));
            });

            return app;
        }
    }
}
