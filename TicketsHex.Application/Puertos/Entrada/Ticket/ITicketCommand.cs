using TicketsHex.Application.DTO_s.Ticket;

namespace TicketsHex.Application.Puertos.Entrada.Ticket
{
    public interface ITicketCommand
    {
        Task<Guid> CrearTicketAsync(CrearTicketRequest request);
        Task ActualizarTicketAsync(Guid ticketId, ActualizarTicketRequest request);
        Task AsignarResponsableDesarrolloAsync(Guid ticketId, AsignarResponsableTicketRequest request);
        Task AsignarResponsableQaAsync(Guid ticketId, AsignarResponsableTicketRequest request);
        Task CambiarResponsableActualAsync(Guid ticketId, AsignarResponsableTicketRequest request);
        Task EliminarTicketAsync(Guid ticketId, string? comentario);
    }
}
