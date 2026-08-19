using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Conocimiento;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Entidades.Conocimiento;
using TicketsHex.Domain.Entidades.Parametros;
using TicketsHex.Domain.Enums;

namespace TicketsHex.infrastructure.Adaptadores.Persistence
{
    internal abstract class ConocimientoTicketRepositoryBase<TContext> : IConocimientoTicketRepository
        where TContext : DbContext
    {
        protected readonly TContext DbContext;

        protected ConocimientoTicketRepositoryBase(TContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<EntradaConocimientoTicket>> ObtenerEntradasTicketAsync(
            Guid idTicket) =>
            await DbContext.Set<EntradaConocimientoTicket>()
                .AsNoTracking()
                .Include(item => item.Referencias)
                .Where(item => item.IdTicket == idTicket && item.Activo)
                .OrderByDescending(item => item.FechaCreacion)
                .ToListAsync();

        public async Task<EntradaConocimientoTicket?> ObtenerEntradaAsync(Guid idEntrada) =>
            await DbContext.Set<EntradaConocimientoTicket>()
                .Include(item => item.Referencias)
                .Include(item => item.Revisiones)
                .FirstOrDefaultAsync(item => item.IdEntrada == idEntrada && item.Activo);

        public async Task<IReadOnlyCollection<RevisionEntradaConocimiento>> ObtenerRevisionesAsync(
            Guid idEntrada) =>
            await DbContext.Set<RevisionEntradaConocimiento>()
                .AsNoTracking()
                .Where(item => item.IdEntrada == idEntrada)
                .OrderByDescending(item => item.FechaRevision)
                .ToListAsync();

        public async Task<PaginaResultado<EntradaConocimientoTicket>> BuscarAsync(
            ConocimientoFiltroRequest filtro)
        {
            var query = DbContext.Set<EntradaConocimientoTicket>()
                .AsNoTracking()
                .Where(item => item.Activo);

            if (filtro.Tipo.HasValue)
                query = query.Where(item => item.Tipo == filtro.Tipo.Value);
            if (filtro.IdResultado.HasValue)
                query = query.Where(item => item.IdResultado == filtro.IdResultado.Value);
            if (filtro.IdAmbiente.HasValue)
                query = query.Where(item => item.IdAmbiente == filtro.IdAmbiente.Value);
            if (!string.IsNullOrWhiteSpace(filtro.Texto))
            {
                var texto = filtro.Texto;
                query = query.Where(item =>
                    item.Resumen.Contains(texto) ||
                    (item.Sintomas != null && item.Sintomas.Contains(texto)) ||
                    (item.Comprobaciones != null && item.Comprobaciones.Contains(texto)) ||
                    (item.PasosReproduccion != null && item.PasosReproduccion.Contains(texto)) ||
                    (item.Observaciones != null && item.Observaciones.Contains(texto)));
            }
            if (filtro.IdAplicativo.HasValue)
            {
                var idAplicativo = filtro.IdAplicativo.Value;
                query = query.Where(item => DbContext.Set<AplicativoTicket>().Any(relacion =>
                    relacion.IdTicket == item.IdTicket && relacion.IdAplicativo == idAplicativo));
            }
            if (!string.IsNullOrWhiteSpace(filtro.Tag))
            {
                var tagNormalizado = TagConocimiento.NormalizarNombre(filtro.Tag);
                query = query.Where(item => DbContext.Set<TagTicket>().Any(relacion =>
                    relacion.IdTicket == item.IdTicket &&
                    DbContext.Set<TagConocimiento>().Any(tag =>
                        tag.IdTag == relacion.IdTag &&
                        tag.NombreNormalizado == tagNormalizado &&
                        tag.Activo)));
            }

            var total = await query.CountAsync();
            var elementos = await query
                .Include(item => item.Referencias)
                .OrderByDescending(item => item.FechaCreacion)
                .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
                .Take(filtro.TamanoPagina)
                .ToListAsync();

            return new PaginaResultado<EntradaConocimientoTicket>(
                elementos,
                filtro.Pagina,
                filtro.TamanoPagina,
                total);
        }

        public async Task<IReadOnlyCollection<TagConocimiento>> ObtenerTagsTicketAsync(Guid idTicket) =>
            await DbContext.Set<TagConocimiento>()
                .AsNoTracking()
                .Where(tag => tag.Activo && DbContext.Set<TagTicket>().Any(relacion =>
                    relacion.IdTicket == idTicket && relacion.IdTag == tag.IdTag))
                .OrderBy(tag => tag.Nombre)
                .ToListAsync();

        public Task<bool> ExisteResultadoActivoAsync(
            TipoEntradaConocimiento tipo,
            int idResultado) =>
            DbContext.Set<ResultadoEntradaConocimientoParametro>().AnyAsync(item =>
                item.IdResultado == idResultado &&
                item.IdTipoEntrada == (int)tipo &&
                item.Activo);

        public Task<bool> ExisteAmbienteActivoAsync(int idAmbiente) =>
            DbContext.Set<AmbienteTicketParametro>().AnyAsync(item =>
                item.IdAmbiente == idAmbiente && item.Activo);

        public async Task GuardarEntradaAsync(
            EntradaConocimientoTicket entrada,
            IReadOnlyCollection<string>? tags,
            IReadOnlyCollection<Guid>? idsAplicativos)
        {
            await DbContext.Set<EntradaConocimientoTicket>().AddAsync(entrada);
            await SincronizarContextoTicketAsync(entrada.IdTicket, tags, idsAplicativos);
            await DbContext.SaveChangesAsync();
        }

        public async Task ActualizarEntradaAsync(
            EntradaConocimientoTicket entrada,
            IReadOnlyCollection<string>? tags,
            IReadOnlyCollection<Guid>? idsAplicativos)
        {
            await SincronizarContextoTicketAsync(entrada.IdTicket, tags, idsAplicativos);
            await DbContext.SaveChangesAsync();
        }

        private async Task SincronizarContextoTicketAsync(
            Guid idTicket,
            IReadOnlyCollection<string>? tags,
            IReadOnlyCollection<Guid>? idsAplicativos)
        {
            if (tags is not null)
                await SincronizarTagsAsync(idTicket, tags);
            if (idsAplicativos is not null)
                await SincronizarAplicativosAsync(idTicket, idsAplicativos);
        }

        private async Task SincronizarTagsAsync(Guid idTicket, IReadOnlyCollection<string> nombres)
        {
            var nombresLimpios = nombres
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .GroupBy(TagConocimiento.NormalizarNombre)
                .Select(group => group.First())
                .ToArray();
            var normalizados = nombresLimpios.Select(TagConocimiento.NormalizarNombre).ToArray();
            var tagsExistentes = await DbContext.Set<TagConocimiento>()
                .Where(item => normalizados.Contains(item.NombreNormalizado))
                .ToListAsync();

            foreach (var nombre in nombresLimpios)
            {
                var normalizado = TagConocimiento.NormalizarNombre(nombre);
                if (tagsExistentes.All(item => item.NombreNormalizado != normalizado))
                {
                    var nuevoTag = new TagConocimiento(nombre);
                    tagsExistentes.Add(nuevoTag);
                    await DbContext.Set<TagConocimiento>().AddAsync(nuevoTag);
                }
            }

            var idsSolicitados = tagsExistentes.Select(item => item.IdTag).ToHashSet();
            var relaciones = await DbContext.Set<TagTicket>()
                .Where(item => item.IdTicket == idTicket)
                .ToListAsync();
            DbContext.Set<TagTicket>().RemoveRange(
                relaciones.Where(item => !idsSolicitados.Contains(item.IdTag)));

            var idsActuales = relaciones.Select(item => item.IdTag).ToHashSet();
            foreach (var idTag in idsSolicitados.Where(item => !idsActuales.Contains(item)))
                await DbContext.Set<TagTicket>().AddAsync(new TagTicket(idTicket, idTag));
        }

        private async Task SincronizarAplicativosAsync(
            Guid idTicket,
            IReadOnlyCollection<Guid> idsAplicativos)
        {
            var idsSolicitados = idsAplicativos.Distinct().ToHashSet();
            var relaciones = await DbContext.Set<AplicativoTicket>()
                .Where(item => item.IdTicket == idTicket)
                .ToListAsync();
            DbContext.Set<AplicativoTicket>().RemoveRange(
                relaciones.Where(item => !idsSolicitados.Contains(item.IdAplicativo)));

            var idsActuales = relaciones.Select(item => item.IdAplicativo).ToHashSet();
            foreach (var idAplicativo in idsSolicitados.Where(item => !idsActuales.Contains(item)))
            {
                await DbContext.Set<AplicativoTicket>()
                    .AddAsync(new AplicativoTicket(idTicket, idAplicativo));
            }
        }
    }
}
