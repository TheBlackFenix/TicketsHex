using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    public sealed class RepositorioRamaRepository : IRepositorioRamaRepository
    {
        private readonly MantenimientoContext _dbContext;

        public RepositorioRamaRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAsync() =>
            await _dbContext.Repositorios
                .AsNoTracking()
                .OrderBy(item => item.Nombre)
                .ToArrayAsync();

        public async Task<Repositorio?> ObtenerRepositorioAsync(Guid idRepositorio) =>
            await _dbContext.Repositorios
                .AsNoTracking()
                .Include(item => item.Ramas)
                .FirstOrDefaultAsync(item => item.IdRepositorio == idRepositorio);

        public async Task<Repositorio?> ObtenerRepositorioPorNombreAsync(string nombre)
        {
            var nombreNormalizado = nombre.Trim().ToLower();
            return await _dbContext.Repositorios
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Nombre.ToLower() == nombreNormalizado);
        }

        public async Task<Rama?> ObtenerRamaAsync(Guid idRama) =>
            await _dbContext.Ramas
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IdRama == idRama);

        public async Task<Rama?> ObtenerRamaPorNombreAsync(
            Guid idRepositorio,
            string nombre)
        {
            var nombreNormalizado = nombre.Trim().ToLower();
            return await _dbContext.Ramas
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.IdRepositorio == idRepositorio &&
                    item.NombreRama.ToLower() == nombreNormalizado);
        }

        public async Task<IReadOnlyCollection<RamaTicket>> ObtenerAsignacionesTicketAsync(
            Guid idTicket) =>
            await _dbContext.RamasTicket
                .AsNoTracking()
                .Where(item => item.IdTicket == idTicket)
                .OrderBy(item => item.FechaAsignacion)
                .ToArrayAsync();

        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idRama) =>
            _dbContext.RamasTicket.AnyAsync(item =>
                item.IdTicket == idTicket && item.IdRama == idRama);

        public async Task GuardarRepositorioAsync(Repositorio repositorio)
        {
            await _dbContext.Repositorios.AddAsync(repositorio);
            await _dbContext.SaveChangesAsync();
        }

        public async Task GuardarRamaAsync(Rama rama)
        {
            await _dbContext.Ramas.AddAsync(rama);
            await _dbContext.SaveChangesAsync();
        }

        public async Task GuardarAsignacionAsync(RamaTicket asignacion)
        {
            await _dbContext.RamasTicket.AddAsync(asignacion);
            await _dbContext.SaveChangesAsync();
        }

        public async Task EliminarAsignacionAsync(Guid idTicket, Guid idRama)
        {
            await _dbContext.RamasTicket
                .Where(item => item.IdTicket == idTicket && item.IdRama == idRama)
                .ExecuteDeleteAsync();
        }
    }
}
