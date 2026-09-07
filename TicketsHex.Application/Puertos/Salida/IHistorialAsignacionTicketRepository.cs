using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IHistorialAsignacionTicketRepository
    {
        Task<Ticket?> ObtenerTicketParaValidarAccesoAsync(
            Guid idTicket,
            long idUsuario,
            bool incluirEliminados);

        Task<PaginaResultado<HistorialAsignacionTicketDTO>> ObtenerPaginaAsync(
            Guid idTicket,
            HistorialAsignacionTicketFiltroRequest filtro);
    }
}
