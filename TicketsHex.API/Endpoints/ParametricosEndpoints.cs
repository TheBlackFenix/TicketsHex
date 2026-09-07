using TicketsHex.API.Reponses;
using TicketsHex.Application.DTO_s.Aplicativo;
using TicketsHex.Application.DTO_s.Parametro;
using TicketsHex.Application.Puertos.Entrada.Aplicativo;
using TicketsHex.Application.Puertos.Entrada.Parametro;

namespace TicketsHex.API.Endpoints
{
    public static class ParametricosEndpoints
    {
        public const string CachePolicyName = "Parametricos12Horas";
        public const string CacheTag = "parametricos";

        public static IEndpointRouteBuilder MapParametricosEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/parametricos", async (
                IParametroQuery parametros,
                IAplicativoService aplicativos) =>
            {
                var resultado = new ParametricosGrupoDTO[]
                {
                    new("tiposTicket", await parametros.ObtenerTiposTicketAsync(false)),
                    new("prioridadesTicket", await parametros.ObtenerPrioridadesTicketAsync(false)),
                    new("impactosTicket", await parametros.ObtenerImpactosTicketAsync(false)),
                    new("roles", await parametros.ObtenerRolesAsync()),
                    new("estadosTicket", await parametros.ObtenerEstadosTicketAsync(false)),
                    new("origenesTicket", await parametros.ObtenerOrigenesTicketAsync(false)),
                    new("areas", await parametros.ObtenerAreasTicketAsync(false)),
                    new("aplicativos", await aplicativos.ObtenerAplicativosAsync(false)),
                    new("tiposEntradaConocimiento", await parametros.ObtenerTiposEntradaConocimientoAsync(false)),
                    new("resultadosEntradaConocimiento", await parametros.ObtenerResultadosEntradaConocimientoAsync(false)),
                    new("ambientesTicket", await parametros.ObtenerAmbientesTicketAsync(false))
                };

                return Results.Ok(ApiResponse<IReadOnlyCollection<ParametricosGrupoDTO>>.Ok(
                    resultado,
                    "Paramétricos consultados correctamente."));
            })
            .WithTags("Paramétricos")
            .WithOpenApi()
            .RequireAuthorization()
            .CacheOutput(CachePolicyName);

            return app;
        }
    }

    public sealed record ParametricosGrupoDTO(
        string Nombre,
        object Items);
}
