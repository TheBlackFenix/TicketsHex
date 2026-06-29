using TicketsHex.Application.DTO_s.Ticket;

namespace TicketsHex.Application.Puertos.Entrada.Ticket
{
    public interface ITicketCommand
    {
        Task<Guid> CrearTicketAsync(CrearTicketRequest request);
        Task ActualizarTicketAsync(Guid ticketId, ActualizarTicketRequest request);
        Task EliminarTicketAsync(Guid ticketId, string? comentario);
    }
}
