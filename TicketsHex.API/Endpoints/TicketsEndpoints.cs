using TicketsHex.API.Reponses;
using TicketsHex.Application.CasosUso.TicketCasosUso;
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
                           .WithOpenApi();

            group.MapGet("/", async (ITicketQuery ticketsQueries) =>
            {
                var tickets = await ticketsQueries.ObtenerListaTicketsAsync();

                return Results.Ok(
                    ApiResponse<IEnumerable<TicketDTO>>.Ok(
                        tickets,
                        "Tickets consultados correctamente."));
            });

            group.MapGet("/{id:guid}", async (Guid id, ITicketQuery ticketsQueries) =>
            {
                var ticket = await ticketsQueries.ObtenerTicketPorIdAsync(id);

                return ticket is not null
                    ? Results.Ok(ApiResponse<TicketDTO>.Ok(ticket, "Ticket consultado correctamente."))
                    : Results.NotFound();
            });

            group.MapPost("/", async (CrearTicketRequest ticketDto, ITicketCommand ticketCommands) =>
            {
                var guid = await ticketCommands.CrearTicketAsync(ticketDto);

                return Results.Created(
                    $"/api/tickets/{guid}",
                    ApiResponse<Guid>.Ok(guid, "Ticket creado correctamente."));
            });

            group.MapPut("/{id:guid}", async (Guid id, ActualizarEstadoRequest ticket, ITicketCommand ticketCommands) =>
            {
                if (id != ticket.IdTicket)
                    return Results.BadRequest("El id de la URL no coincide con el Guid del cuerpo.");

                await ticketCommands.ActualizarEstadoAsync(ticket);
                return Results.Ok(ApiResponse<bool>.Ok(true, "Estado del ticket actualizado correctamente."));
            });

            group.MapDelete("/{id:guid}", async (Guid id, ITicketCommand ticketCommands) =>
            {
                await ticketCommands.EliminarTicketAsync(id);

                return Results.Ok(ApiResponse<bool>.Ok(true, "Ticket eliminado correctamente."));
            });

            return app;
        }
    }
}
