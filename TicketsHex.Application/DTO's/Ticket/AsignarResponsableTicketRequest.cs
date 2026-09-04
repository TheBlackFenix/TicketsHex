namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record AsignarResponsableTicketRequest(
        long IdUsuario,
        string? Comentario = null);
}
