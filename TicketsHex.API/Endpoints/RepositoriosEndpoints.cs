using TicketsHex.API.Reponses;
using TicketsHex.Application.DTO_s.Repositorio;
using TicketsHex.Application.Puertos.Entrada.Repositorio;

namespace TicketsHex.API.Endpoints
{
    public static class RepositoriosEndpoints
    {
        public static IEndpointRouteBuilder MapRepositoriosEndpoints(
            this IEndpointRouteBuilder app)
        {
            var repositorios = app.MapGroup("/api/repositorios")
                .WithTags("Repositorios y ramas")
                .WithOpenApi()
                .RequireAuthorization();

            repositorios.MapGet("/", async (IRepositorioRamaService service) =>
            {
                var resultado = await service.ObtenerRepositoriosAsync();
                return Results.Ok(
                    ApiResponse<IReadOnlyCollection<RepositorioDTO>>.Ok(resultado));
            });

            repositorios.MapPost("/", async (
                CrearRepositorioRequest request,
                IRepositorioRamaService service) =>
            {
                var id = await service.CrearRepositorioAsync(request);
                return Results.Created(
                    $"/api/repositorios/{id}",
                    ApiResponse<Guid>.Ok(id, "Repositorio creado correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            repositorios.MapGet("/{idRepositorio:guid}/ramas", async (
                Guid idRepositorio,
                IRepositorioRamaService service) =>
            {
                var resultado = await service.ObtenerRamasAsync(idRepositorio);
                return Results.Ok(
                    ApiResponse<IReadOnlyCollection<RamaDTO>>.Ok(resultado));
            });

            repositorios.MapPost("/{idRepositorio:guid}/ramas", async (
                Guid idRepositorio,
                CrearRamaRequest request,
                IRepositorioRamaService service) =>
            {
                var id = await service.CrearRamaAsync(idRepositorio, request);
                return Results.Created(
                    $"/api/repositorios/{idRepositorio}/ramas/{id}",
                    ApiResponse<Guid>.Ok(id, "Rama creada correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            var ramasTicket = app.MapGroup("/api/tickets/{idTicket:guid}/ramas")
                .WithTags("Repositorios y ramas")
                .WithOpenApi()
                .RequireAuthorization();

            ramasTicket.MapGet("/", async (
                Guid idTicket,
                IRepositorioRamaService service) =>
            {
                var resultado = await service.ObtenerRamasTicketAsync(idTicket);
                return Results.Ok(
                    ApiResponse<IReadOnlyCollection<RamaTicketDTO>>.Ok(resultado));
            });

            ramasTicket.MapPost("/", async (
                Guid idTicket,
                AsignarRamaTicketRequest request,
                IRepositorioRamaService service) =>
            {
                var id = await service.AsignarRamaAsync(idTicket, request);
                return Results.Created(
                    $"/api/tickets/{idTicket}/ramas/{request.IdRama}",
                    ApiResponse<Guid>.Ok(id, "Rama asignada correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            ramasTicket.MapDelete("/{idRama:guid}", async (
                Guid idTicket,
                Guid idRama,
                IRepositorioRamaService service) =>
            {
                await service.DesasignarRamaAsync(idTicket, idRama);
                return Results.Ok(
                    ApiResponse<bool>.Ok(true, "Rama desasignada correctamente."));
            }).RequireAuthorization("PlannerOrLiderTecnico");

            return app;
        }
    }
}
