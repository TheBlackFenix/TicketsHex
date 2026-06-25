using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqliteRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqliteRepository
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
            _dbContext.Tickets.Update(ticket);
            await _dbContext.SaveChangesAsync();
        }

        public async Task EliminarAsync(Guid id)
        {
            var ticket = await _dbContext.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _dbContext.Tickets.Remove(ticket);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task GuardarAsync(Ticket ticket)
        {
            await _dbContext.Tickets.AddAsync(ticket);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Ticket> ObtenerPorIdAsync(Guid id)
        {
            return await _dbContext.Tickets
            .Include(t => t.HistoricoEstados)
            .FirstOrDefaultAsync(t => t.IdTicket == id);
        }

        public async Task<Ticket> ObtenerTicketPorCodigoYUsuerioAsync(Guid id, int idUsuarioAsignado)
        {
            return await _dbContext.Tickets
            .Include(t => t.HistoricoEstados)
            .FirstOrDefaultAsync(t => t.IdTicket == id && t.IdUsuarioAsignado == idUsuarioAsignado);
        }

        public async Task<IEnumerable<Ticket>> ObtenerTodosAsync()
        {
            return await _dbContext.Tickets
            .AsNoTracking() 
            .OrderByDescending(t => t.FechaAsignacion)
            .ToListAsync();
        }

        public async Task<IEnumerable<Ticket>> ObtenerTodosPorIdUsuarioAsignadoAsync(int idUsuarioAsignado)
        {
            return await _dbContext.Tickets
            .AsNoTracking() // Optimización sustancial para solo lectura
            .Where(t => t.IdUsuarioAsignado == idUsuarioAsignado)
            .OrderByDescending(t => t.FechaAsignacion)
            .ToListAsync();
        }
    }
}
