using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using TicketsHex.API.Reponses;
using TicketsHex.API.Servicios;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Entrada.Ticket;

namespace TicketsHex.API.Endpoints
{
    public static class TicketEndpoints
    {
        public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/tickets")
                .WithTags("Tickets")
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

            group.MapGet("/", async (
                [AsParameters] TicketFiltroRequest filtro,
                ITicketQuery queries) =>
            {
                var tickets = await queries.ObtenerListaTicketsAsync(filtro);
                return Results.Ok(ApiResponse<PaginaResultado<TicketDTO>>.Ok(
                    tickets,
                    "Tickets consultados correctamente."));
            });

            group.MapGet("/mis-tickets", async (
                [AsParameters] TicketFiltroRequest filtro,
                ITicketQuery queries) =>
            {
                var tickets = await queries.ObtenerMisTicketsAsync(filtro);
                return Results.Ok(ApiResponse<PaginaResultado<TicketDTO>>.Ok(
                    tickets,
                    "Tickets asignados consultados correctamente."));
            });

            group.MapGet("/{id:guid}", async (Guid id, ITicketQuery queries) =>
            {
                var ticket = await queries.ObtenerTicketPorIdAsync(id);
                return Results.Ok(ApiResponse<TicketDTO>.Ok(
                    ticket,
                    "Ticket consultado correctamente."));
            });

            group.MapPost("/", async (CrearTicketRequest request, ITicketCommand commands) =>
            {
                var id = await commands.CrearTicketAsync(request);
                return Results.Created(
                    $"/api/tickets/{id}",
                    ApiResponse<Guid>.Ok(id, "Ticket creado correctamente."));
            });

            group.MapPatch("/{id:guid}", async (
                Guid id,
                ActualizarTicketRequest request,
                ITicketCommand commands) =>
            {
                await commands.ActualizarTicketAsync(id, request);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Ticket actualizado correctamente."));
            });

            group.MapDelete("/{id:guid}", async (
                Guid id,
                [FromQuery] string? comentario,
                ITicketCommand commands) =>
            {
                await commands.EliminarTicketAsync(id, comentario);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Ticket eliminado correctamente."));
            });

            app.MapUsuariosEndpoints();
            return app;
        }
    }
}
