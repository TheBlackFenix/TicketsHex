using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Domain.Entidades.Notificacion;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface INotificacionUsuarioRepository
    {
        Task PrepararNotificacionesAsync(IReadOnlyCollection<NotificacionUsuario> notificaciones);
        Task<PaginaResultado<NotificacionUsuarioDTO>> ObtenerPaginaUsuarioAsync(
            long idUsuario,
            NotificacionFiltroRequest filtro);
        Task<int> ObtenerCantidadNoLeidasAsync(long idUsuario);
        Task<NotificacionUsuario?> ObtenerPorIdUsuarioAsync(Guid idNotificacion, long idUsuario);
        Task<IReadOnlyCollection<NotificacionUsuario>> ObtenerNoLeidasUsuarioAsync(long idUsuario);
        Task<IReadOnlyCollection<long>> ObtenerIdsUsuariosActivosPorRolesAsync(
            IReadOnlyCollection<Rol> roles);
        Task<IReadOnlyCollection<long>> PrepararEliminacionPorTicketAsync(Guid idTicket);
        Task<IReadOnlyCollection<long>> EliminarExpiradasOFinalizadasAsync(DateTimeOffset fechaActual);
        Task GuardarCambiosAsync();
    }
}
