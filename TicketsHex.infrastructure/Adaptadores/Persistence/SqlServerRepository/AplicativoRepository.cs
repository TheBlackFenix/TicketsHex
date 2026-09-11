using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Enums;
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

        public async Task<IReadOnlyCollection<RepositorioAplicativo>> ObtenerRelacionesRepositorioAsync(
            Guid idAplicativo) =>
            await _dbContext.RepositoriosAplicativo
                .AsNoTracking()
                .Where(item => item.IdAplicativo == idAplicativo)
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAplicativoAsync(
            Guid idAplicativo) =>
            await _dbContext.Repositorios
                .AsNoTracking()
                .Where(repositorio => _dbContext.RepositoriosAplicativo.Any(relacion =>
                    relacion.IdAplicativo == idAplicativo &&
                    relacion.IdRepositorio == repositorio.IdRepositorio))
                .OrderBy(item => item.Nombre)
                .ToArrayAsync();

        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idAplicativo) =>
            _dbContext.AplicativosTicket.AnyAsync(item =>
                item.IdTicket == idTicket && item.IdAplicativo == idAplicativo);

        public Task<bool> ExisteRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio) =>
            _dbContext.RepositoriosAplicativo.AnyAsync(item =>
                item.IdAplicativo == idAplicativo && item.IdRepositorio == idRepositorio);

        public Task<bool> TieneRamasTicketSinRespaldoAsync(Guid idTicket, Guid idAplicativo) =>
            _dbContext.RamasTicket.AnyAsync(ramaTicket =>
                ramaTicket.IdTicket == idTicket &&
                _dbContext.Ramas.Any(rama =>
                    rama.IdRama == ramaTicket.IdRama &&
                    _dbContext.RepositoriosAplicativo.Any(relacion =>
                        relacion.IdRepositorio == rama.IdRepositorio &&
                        relacion.IdAplicativo == idAplicativo) &&
                    !_dbContext.RepositoriosAplicativo.Any(otraRelacion =>
                        otraRelacion.IdRepositorio == rama.IdRepositorio &&
                        otraRelacion.IdAplicativo != idAplicativo &&
                        _dbContext.AplicativosTicket.Any(aplicativoTicket =>
                            aplicativoTicket.IdTicket == idTicket &&
                            aplicativoTicket.IdAplicativo == otraRelacion.IdAplicativo))));

        public Task<bool> TieneTicketsActivosDependientesAsync(
            Guid idAplicativo,
            Guid idRepositorio) =>
            _dbContext.RamasTicket.AnyAsync(ramaTicket =>
                _dbContext.Tickets.Any(ticket =>
                    ticket.IdTicket == ramaTicket.IdTicket &&
                    ticket.IdEstado != TicketEstado.Finalizado) &&
                _dbContext.AplicativosTicket.Any(aplicativoTicket =>
                    aplicativoTicket.IdTicket == ramaTicket.IdTicket &&
                    aplicativoTicket.IdAplicativo == idAplicativo) &&
                _dbContext.Ramas.Any(rama =>
                    rama.IdRama == ramaTicket.IdRama &&
                    rama.IdRepositorio == idRepositorio) &&
                !_dbContext.RepositoriosAplicativo.Any(otraRelacion =>
                    otraRelacion.IdRepositorio == idRepositorio &&
                    otraRelacion.IdAplicativo != idAplicativo &&
                    _dbContext.AplicativosTicket.Any(otroAplicativoTicket =>
                        otroAplicativoTicket.IdTicket == ramaTicket.IdTicket &&
                        otroAplicativoTicket.IdAplicativo == otraRelacion.IdAplicativo)));

        public async Task GuardarAplicativoAsync(
            Aplicativo aplicativo,
            IReadOnlyCollection<RepositorioAplicativo> relaciones)
        {
            await _dbContext.Aplicativos.AddAsync(aplicativo);
            await _dbContext.RepositoriosAplicativo.AddRangeAsync(relaciones);
            await _dbContext.SaveChangesAsync();
        }

        public async Task GuardarRelacionRepositorioAsync(RepositorioAplicativo relacion)
        {
            await _dbContext.RepositoriosAplicativo.AddAsync(relacion);
            await _dbContext.SaveChangesAsync();
        }

        public async Task EliminarRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio)
        {
            await _dbContext.RepositoriosAplicativo
                .Where(item =>
                    item.IdAplicativo == idAplicativo &&
                    item.IdRepositorio == idRepositorio)
                .ExecuteDeleteAsync();
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
