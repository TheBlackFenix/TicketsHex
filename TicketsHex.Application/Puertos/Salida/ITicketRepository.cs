using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface ITicketRepository
    {
        Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false);
        Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro);
        Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(TicketFiltroRequest filtro);
        Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(
            long idUsuario,
            TicketFiltroRequest filtro);
        Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario);
        Task GuardarAsync(Ticket ticket);
        Task ActualizarAsync(Ticket ticket);
        Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets);
    }
}
