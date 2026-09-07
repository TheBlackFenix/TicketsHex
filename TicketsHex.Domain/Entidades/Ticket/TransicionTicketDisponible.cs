using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public sealed record TransicionTicketDisponible(
        TicketEstado EstadoDestino,
        TipoTransicionDisponible Tipo,
        bool RequiereComentario);
}
