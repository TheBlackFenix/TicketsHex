using Microsoft.AspNetCore.Mvc;
using TicketsHex.API.Reponses;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Conocimiento;
using TicketsHex.Application.Puertos.Entrada.Conocimiento;

namespace TicketsHex.API.Endpoints
{
    public static class ConocimientoEndpoints
    {
        public static IEndpointRouteBuilder MapConocimientoEndpoints(
            this IEndpointRouteBuilder app)
        {
            var conocimientoTicket = app.MapGroup("/api/tickets/{idTicket:guid}/conocimiento")
                .WithTags("Base de conocimiento")
                .WithOpenApi()
                .RequireAuthorization();

            conocimientoTicket.MapGet("/", async (
                Guid idTicket,
                IConocimientoTicketService service) =>
            {
                var resultado = await service.ObtenerBaseAsync(idTicket);
                return Results.Ok(ApiResponse<BaseConocimientoTicketDTO>.Ok(
                    resultado,
                    "Base de conocimiento consultada correctamente."));
            });

            conocimientoTicket.MapPost("/diagnosticos", async (
                Guid idTicket,
                CrearDiagnosticoRequest request,
                IConocimientoTicketService service) =>
            {
                var id = await service.CrearDiagnosticoAsync(idTicket, request);
                return Results.Created(
                    $"/api/tickets/{idTicket}/conocimiento/entradas/{id}",
                    ApiResponse<Guid>.Ok(id, "Diagnóstico registrado correctamente."));
            });

            conocimientoTicket.MapPost("/soluciones", async (
                Guid idTicket,
                CrearSolucionRequest request,
                IConocimientoTicketService service) =>
            {
                var id = await service.CrearSolucionAsync(idTicket, request);
                return Results.Created(
                    $"/api/tickets/{idTicket}/conocimiento/entradas/{id}",
                    ApiResponse<Guid>.Ok(id, "Solución registrada correctamente."));
            });

            conocimientoTicket.MapPost("/validaciones", async (
                Guid idTicket,
                CrearValidacionQaRequest request,
                IConocimientoTicketService service) =>
            {
                var id = await service.CrearValidacionQaAsync(idTicket, request);
                return Results.Created(
                    $"/api/tickets/{idTicket}/conocimiento/entradas/{id}",
                    ApiResponse<Guid>.Ok(id, "Validación QA registrada correctamente."));
            });

            conocimientoTicket.MapPatch("/entradas/{idEntrada:guid}", async (
                Guid idTicket,
                Guid idEntrada,
                ActualizarEntradaConocimientoRequest request,
                IConocimientoTicketService service) =>
            {
                await service.ActualizarEntradaAsync(idTicket, idEntrada, request);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Entrada de conocimiento actualizada correctamente."));
            });

            conocimientoTicket.MapGet("/entradas/{idEntrada:guid}/revisiones", async (
                Guid idTicket,
                Guid idEntrada,
                IConocimientoTicketService service) =>
            {
                var resultado = await service.ObtenerRevisionesAsync(idTicket, idEntrada);
                return Results.Ok(ApiResponse<IReadOnlyCollection<RevisionEntradaConocimientoDTO>>.Ok(
                    resultado,
                    "Revisiones consultadas correctamente."));
            });

            app.MapGet("/api/conocimiento", async (
                [AsParameters] ConocimientoFiltroRequest filtro,
                IConocimientoTicketService service) =>
            {
                var resultado = await service.BuscarAsync(filtro);
                return Results.Ok(ApiResponse<PaginaResultado<EntradaConocimientoDTO>>.Ok(
                    resultado,
                    "Conocimiento consultado correctamente."));
            })
            .WithTags("Base de conocimiento")
            .WithOpenApi()
            .RequireAuthorization();

            return app;
        }
    }
}
