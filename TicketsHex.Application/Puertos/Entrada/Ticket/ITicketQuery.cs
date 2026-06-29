using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;

namespace TicketsHex.Application.Puertos.Entrada.Ticket
{
    public interface ITicketQuery
    {
        Task<TicketDTO> ObtenerTicketPorIdAsync(Guid id);
        Task<PaginaResultado<TicketDTO>> ObtenerListaTicketsAsync(TicketFiltroRequest filtro);
        Task<PaginaResultado<TicketDTO>> ObtenerMisTicketsAsync(TicketFiltroRequest filtro);
    }
}
