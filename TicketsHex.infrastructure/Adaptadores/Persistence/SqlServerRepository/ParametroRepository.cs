using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Parametros;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    public sealed class ParametroRepository : IParametroRepository
    {
        private readonly MantenimientoContext _dbContext;

        public ParametroRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<RolParametro>> ObtenerRolesAsync() =>
            await _dbContext.Roles
                .AsNoTracking()
                .OrderBy(item => item.IdRol)
                .ToListAsync();

        public async Task<IReadOnlyCollection<EstadoTicketParametro>> ObtenerEstadosTicketAsync(
            bool incluirInactivos) =>
            await _dbContext.EstadosTicket
                .AsNoTracking()
                .Where(item => incluirInactivos || item.Activo)
                .OrderBy(item => item.IdEstado)
                .ToListAsync();

        public async Task<IReadOnlyCollection<OrigenTicketParametro>> ObtenerOrigenesTicketAsync(
            bool incluirInactivos) =>
            await _dbContext.OrigenesTicket
                .AsNoTracking()
                .Where(item => incluirInactivos || item.Activo)
                .OrderBy(item => item.IdOrigen)
                .ToListAsync();

        public async Task<IReadOnlyCollection<AreaTicketParametro>> ObtenerAreasTicketAsync(
            bool incluirInactivos) =>
            await _dbContext.AreasTicket
                .AsNoTracking()
                .Where(item => incluirInactivos || item.Activo)
                .OrderBy(item => item.IdArea)
                .ToListAsync();
    }
}
