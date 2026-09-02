using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    public sealed class AplicativoRepository : IAplicativoRepository
    {
        private readonly MantenimientoContext _dbContext;

        public AplicativoRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<Aplicativo>> ObtenerAplicativosAsync(bool incluirInactivos) =>
            await _dbContext.Aplicativos
                .AsNoTracking()
                .Where(item => incluirInactivos || item.Activo)
                .OrderBy(item => item.Nombre)
                .ToArrayAsync();

        public async Task<Aplicativo?> ObtenerAplicativoAsync(Guid idAplicativo) =>
            await _dbContext.Aplicativos
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.IdAplicativo == idAplicativo);

        public async Task<Aplicativo?> ObtenerAplicativoPorNombreAsync(string nombre)
        {
            var nombreNormalizado = nombre.Trim().ToLower();
            return await _dbContext.Aplicativos
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Nombre.ToLower() == nombreNormalizado);
        }

        public async Task<IReadOnlyCollection<AplicativoTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket) =>
            await _dbContext.AplicativosTicket
                .AsNoTracking()
                .Where(item => item.IdTicket == idTicket)
                .OrderBy(item => item.FechaAsignacion)
                .ToArrayAsync();

        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idAplicativo) =>
            _dbContext.AplicativosTicket.AnyAsync(item =>
                item.IdTicket == idTicket && item.IdAplicativo == idAplicativo);

        public async Task GuardarAplicativoAsync(Aplicativo aplicativo)
        {
            await _dbContext.Aplicativos.AddAsync(aplicativo);
            await _dbContext.SaveChangesAsync();
        }

        public async Task GuardarAsignacionAsync(AplicativoTicket asignacion)
        {
            await _dbContext.AplicativosTicket.AddAsync(asignacion);
            await _dbContext.SaveChangesAsync();
        }

        public async Task EliminarAsignacionAsync(Guid idTicket, Guid idAplicativo)
        {
            await _dbContext.AplicativosTicket
                .Where(item => item.IdTicket == idTicket && item.IdAplicativo == idAplicativo)
                .ExecuteDeleteAsync();
        }
    }
}
