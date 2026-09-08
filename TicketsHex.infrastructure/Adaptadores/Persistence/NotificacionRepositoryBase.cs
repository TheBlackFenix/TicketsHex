using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Entidades.Notificacion;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;

namespace TicketsHex.infrastructure.Adaptadores.Persistence
{
    public abstract class NotificacionRepositoryBase<TContext>
        : INotificacionRepository, INotificacionUsuarioRepository
        where TContext : DbContext
    {
        protected readonly TContext DbContext;

        protected NotificacionRepositoryBase(TContext dbContext)
        {
            DbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinHuAsync() =>
            await TicketsDesarrollo()
                .Where(ticket =>
                    string.IsNullOrWhiteSpace(ticket.NombreHu) ||
                    string.IsNullOrWhiteSpace(ticket.UrlHu))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosAsync() =>
            await TicketsDesarrollo()
                .Where(ticket => string.IsNullOrWhiteSpace(ticket.CarpetaMedios))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinRamasAsync() =>
            await TicketsDesarrollo()
                .Where(ticket => !DbContext.Set<RamaTicket>().Any(rama => rama.IdTicket == ticket.IdTicket))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosORamasAsync() =>
            await TicketsDesarrollo()
                .Where(ticket =>
                    string.IsNullOrWhiteSpace(ticket.CarpetaMedios) ||
                    !DbContext.Set<RamaTicket>().Any(rama => rama.IdTicket == ticket.IdTicket))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task PrepararNotificacionesAsync(
            IReadOnlyCollection<NotificacionUsuario> notificaciones)
        {
            if (notificaciones.Count > 0)
                await DbContext.Set<NotificacionUsuario>().AddRangeAsync(notificaciones);
        }

        public async Task<PaginaResultado<NotificacionUsuarioDTO>> ObtenerPaginaUsuarioAsync(
            long idUsuario,
            NotificacionFiltroRequest filtro)
        {
            var fechaActual = DateTimeOffset.UtcNow;
            var query =
                from notificacion in DbContext.Set<NotificacionUsuario>().AsNoTracking()
                join ticket in DbContext.Set<Ticket>().AsNoTracking()
                    on notificacion.IdTicket equals ticket.IdTicket
                where notificacion.IdUsuarioDestinatario == idUsuario &&
                      notificacion.FechaExpiracion > fechaActual &&
                      ticket.Activo &&
                      ticket.IdEstado != TicketEstado.Finalizado
                select new { notificacion, ticket };

            var total = await query.CountAsync();
            var elementos = await query
                .OrderByDescending(item => item.notificacion.FechaCreacion)
                .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
                .Take(filtro.TamanoPagina)
                .Select(item => new NotificacionUsuarioDTO(
                    item.notificacion.IdNotificacion,
                    item.notificacion.IdTipoNotificacion,
                    item.notificacion.Mensaje,
                    item.notificacion.FechaCreacion,
                    item.notificacion.FechaLectura.HasValue,
                    item.ticket.IdTicket,
                    item.ticket.CodigoCaso.Valor,
                    item.ticket.Titulo.Value))
                .ToArrayAsync();

            return new PaginaResultado<NotificacionUsuarioDTO>(
                elementos,
                filtro.Pagina,
                filtro.TamanoPagina,
                total);
        }

        public Task<int> ObtenerCantidadNoLeidasAsync(long idUsuario)
        {
            var fechaActual = DateTimeOffset.UtcNow;
            return DbContext.Set<NotificacionUsuario>()
                .AsNoTracking()
                .CountAsync(item =>
                    item.IdUsuarioDestinatario == idUsuario &&
                    !item.FechaLectura.HasValue &&
                    item.FechaExpiracion > fechaActual &&
                    DbContext.Set<Ticket>().Any(ticket =>
                        ticket.IdTicket == item.IdTicket &&
                        ticket.Activo &&
                        ticket.IdEstado != TicketEstado.Finalizado));
        }

        public Task<NotificacionUsuario?> ObtenerPorIdUsuarioAsync(
            Guid idNotificacion,
            long idUsuario)
        {
            var fechaActual = DateTimeOffset.UtcNow;
            return DbContext.Set<NotificacionUsuario>()
                .FirstOrDefaultAsync(item =>
                    item.IdNotificacion == idNotificacion &&
                    item.IdUsuarioDestinatario == idUsuario &&
                    item.FechaExpiracion > fechaActual &&
                    DbContext.Set<Ticket>().Any(ticket =>
                        ticket.IdTicket == item.IdTicket &&
                        ticket.Activo &&
                        ticket.IdEstado != TicketEstado.Finalizado));
        }

        public async Task<IReadOnlyCollection<NotificacionUsuario>> ObtenerNoLeidasUsuarioAsync(
            long idUsuario)
        {
            var fechaActual = DateTimeOffset.UtcNow;
            return await DbContext.Set<NotificacionUsuario>()
                .Where(item =>
                    item.IdUsuarioDestinatario == idUsuario &&
                    !item.FechaLectura.HasValue &&
                    item.FechaExpiracion > fechaActual &&
                    DbContext.Set<Ticket>().Any(ticket =>
                        ticket.IdTicket == item.IdTicket &&
                        ticket.Activo &&
                        ticket.IdEstado != TicketEstado.Finalizado))
                .ToArrayAsync();
        }

        public async Task<IReadOnlyCollection<long>> ObtenerIdsUsuariosActivosPorRolesAsync(
            IReadOnlyCollection<Rol> roles)
        {
            var rolesSolicitados = roles.Distinct().ToArray();
            return await DbContext.Set<Usuario>()
                .AsNoTracking()
                .Where(usuario => usuario.Activo && rolesSolicitados.Contains(usuario.IdRol))
                .Select(usuario => usuario.IdUsuario)
                .ToArrayAsync();
        }

        public async Task<IReadOnlyCollection<long>> PrepararEliminacionPorTicketAsync(Guid idTicket)
        {
            var notificaciones = await DbContext.Set<NotificacionUsuario>()
                .Where(item => item.IdTicket == idTicket)
                .ToArrayAsync();
            var destinatariosNoLeidos = notificaciones
                .Where(item => !item.Leida)
                .Select(item => item.IdUsuarioDestinatario)
                .Distinct()
                .ToArray();
            DbContext.Set<NotificacionUsuario>().RemoveRange(notificaciones);
            return destinatariosNoLeidos;
        }

        public async Task<IReadOnlyCollection<long>> EliminarExpiradasOFinalizadasAsync(
            DateTimeOffset fechaActual)
        {
            var query = DbContext.Set<NotificacionUsuario>()
                .Where(item =>
                    item.FechaExpiracion <= fechaActual ||
                    DbContext.Set<Ticket>().Any(ticket =>
                        ticket.IdTicket == item.IdTicket &&
                        (!ticket.Activo || ticket.IdEstado == TicketEstado.Finalizado)));
            var destinatariosNoLeidos = await query
                .Where(item => !item.FechaLectura.HasValue)
                .Select(item => item.IdUsuarioDestinatario)
                .Distinct()
                .ToArrayAsync();
            await query.ExecuteDeleteAsync();
            return destinatariosNoLeidos;
        }

        public Task GuardarCambiosAsync() => DbContext.SaveChangesAsync();

        private IQueryable<Ticket> TicketsDesarrollo() =>
            DbContext.Set<Ticket>()
                .AsNoTracking()
                .Where(ticket =>
                    ticket.Activo &&
                    ticket.EsDesarrollo &&
                    ticket.IdEstado != TicketEstado.Finalizado);

    }
}
