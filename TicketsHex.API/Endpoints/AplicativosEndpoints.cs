using TicketsHex.API.Reponses;
using TicketsHex.Application.DTO_s.Aplicativo;
using TicketsHex.Application.DTO_s.Repositorio;
using TicketsHex.Application.Puertos.Entrada.Aplicativo;
using Microsoft.AspNetCore.OutputCaching;

namespace TicketsHex.API.Endpoints
{
    public static class AplicativosEndpoints
    {
        public static IEndpointRouteBuilder MapAplicativosEndpoints(this IEndpointRouteBuilder app)
        {
            var aplicativos = app.MapGroup("/api/aplicativos")
                .WithTags("Aplicativos")
                .WithOpenApi()
                .RequireAuthorization();

            aplicativos.MapGet("/", async (
                bool incluirInactivos,
                IAplicativoService service) =>
            {
                var resultado = await service.ObtenerAplicativosAsync(incluirInactivos);
                return Results.Ok(ApiResponse<IReadOnlyCollection<AplicativoDTO>>.Ok(resultado));
            });

            aplicativos.MapPost("/", async (
                CrearAplicativoRequest request,
                IAplicativoService service,
                IOutputCacheStore cache,
                CancellationToken cancellationToken) =>
            {
                var id = await service.CrearAplicativoAsync(request);
                await cache.EvictByTagAsync(ParametricosEndpoints.CacheTag, cancellationToken);
                return Results.Created(
                    $"/api/aplicativos/{id}",
                    ApiResponse<Guid>.Ok(id, "Aplicativo creado correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            aplicativos.MapGet("/{idAplicativo:guid}/repositorios", async (
                Guid idAplicativo,
                IAplicativoService service) =>
            {
                var resultado = await service.ObtenerRepositoriosAplicativoAsync(idAplicativo);
                return Results.Ok(ApiResponse<IReadOnlyCollection<RepositorioAplicativoDTO>>.Ok(resultado));
            });

            aplicativos.MapPost("/{idAplicativo:guid}/repositorios", async (
                Guid idAplicativo,
                AsignarRepositorioAplicativoRequest request,
                IAplicativoService service) =>
            {
                var id = await service.AsignarRepositorioAsync(idAplicativo, request);
                return Results.Created(
                    $"/api/aplicativos/{idAplicativo}/repositorios/{request.IdRepositorio}",
                    ApiResponse<Guid>.Ok(id, "Repositorio asociado correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            aplicativos.MapDelete("/{idAplicativo:guid}/repositorios/{idRepositorio:guid}", async (
                Guid idAplicativo,
                Guid idRepositorio,
                IAplicativoService service) =>
            {
                await service.DesasignarRepositorioAsync(idAplicativo, idRepositorio);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Repositorio desasociado correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            var aplicativosTicket = app.MapGroup("/api/tickets/{idTicket:guid}/aplicativos")
                .WithTags("Aplicativos")
                .WithOpenApi()
                .RequireAuthorization();

            aplicativosTicket.MapGet("/", async (
                Guid idTicket,
                IAplicativoService service) =>
            {
                var resultado = await service.ObtenerAplicativosTicketAsync(idTicket);
                return Results.Ok(ApiResponse<IReadOnlyCollection<AplicativoTicketDTO>>.Ok(resultado));
            });

            aplicativosTicket.MapPost("/", async (
                Guid idTicket,
                AsignarAplicativoTicketRequest request,
                IAplicativoService service) =>
            {
                var id = await service.AsignarAplicativoAsync(idTicket, request);
                return Results.Created(
                    $"/api/tickets/{idTicket}/aplicativos/{request.IdAplicativo}",
                    ApiResponse<Guid>.Ok(id, "Aplicativo asociado correctamente."));
            });

            aplicativosTicket.MapDelete("/{idAplicativo:guid}", async (
                Guid idTicket,
                Guid idAplicativo,
                IAplicativoService service) =>
            {
                await service.DesasignarAplicativoAsync(idTicket, idAplicativo);
                return Results.Ok(ApiResponse<bool>.Ok(true, "Aplicativo desasociado correctamente."));
            });

            return app;
        }
    }
}
