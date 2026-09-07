using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository
{
    public sealed class HistorialAsignacionTicketRepository
        : IHistorialAsignacionTicketRepository
    {
        private readonly MantenimientoContext _dbContext;

        public HistorialAsignacionTicketRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Ticket?> ObtenerTicketParaValidarAccesoAsync(
            Guid idTicket,
            long idUsuario,
            bool incluirEliminados) =>
            HistorialAsignacionTicketRepositoryBase.ObtenerTicketParaValidarAccesoAsync(
                _dbContext.Tickets,
                idTicket,
                idUsuario,
                incluirEliminados);

        public Task<PaginaResultado<HistorialAsignacionTicketDTO>> ObtenerPaginaAsync(
            Guid idTicket,
            HistorialAsignacionTicketFiltroRequest filtro) =>
            HistorialAsignacionTicketRepositoryBase.ObtenerPaginaAsync(
                _dbContext.HistoricoAsignaciones,
                _dbContext.Usuarios,
                idTicket,
                filtro);
    }
}
