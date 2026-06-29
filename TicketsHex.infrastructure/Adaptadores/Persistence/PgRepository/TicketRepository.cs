using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.ValueObjects.Ticket;
using TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly MantenimientoContext _dbContext;

        public TicketRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ActualizarAsync(Ticket ticket)
        {
            await _dbContext.SaveChangesAsync();
        }

        public async Task GuardarAsync(Ticket ticket)
        {
            await _dbContext.Tickets.AddAsync(ticket);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false)
        {
            return await _dbContext.Tickets
                .Include(t => t.HistoricoEstados)
                .Where(t => incluirEliminados || t.Activo)
                .FirstOrDefaultAsync(t => t.IdTicket == id);
        }

        public async Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro)
        {
            IQueryable<Ticket> query = _dbContext.Tickets.AsNoTracking();

            if (!filtro.IncluirEliminados)
                query = query.Where(t => t.Activo);
            if (filtro.Estado.HasValue)
                query = query.Where(t => t.IdEstado == filtro.Estado.Value);
            if (filtro.Origen.HasValue)
                query = query.Where(t => t.IdOrigen == filtro.Origen.Value);
            if (filtro.IdUsuarioAsignado.HasValue)
                query = query.Where(t => t.IdUsuarioAsignado == filtro.IdUsuarioAsignado.Value);
            if (!string.IsNullOrWhiteSpace(filtro.CodigoCaso))
            {
                var codigo = new CodigoCasoVO(filtro.CodigoCaso);
                query = query.Where(t => t.CodigoCaso == codigo);
            }
            if (filtro.Desde.HasValue)
                query = query.Where(t => t.FechaAsignacion >= filtro.Desde.Value);
            if (filtro.Hasta.HasValue)
                query = query.Where(t => t.FechaAsignacion <= filtro.Hasta.Value);

            var total = await query.CountAsync();
            var tickets = await query
                .Include(t => t.HistoricoEstados)
                .AsSplitQuery()
                .OrderByDescending(t => t.FechaAsignacion)
                .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
                .Take(filtro.TamanoPagina)
                .ToListAsync();

            return new PaginaResultado<Ticket>(
                tickets,
                filtro.Pagina,
                filtro.TamanoPagina,
                total);
        }
    }
}
