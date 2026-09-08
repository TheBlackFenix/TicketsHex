using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Comun.Paginacion;

namespace TicketsHex.Application.Puertos.Entrada.Notificacion
{
    public interface INotificacionQuery
    {
        Task<NotificacionResumenDTO> ObtenerResumenAsync();
        Task<PaginaResultado<NotificacionUsuarioDTO>> ObtenerNotificacionesAsync(
            NotificacionFiltroRequest filtro);
        Task<ConteoNotificacionesDTO> ObtenerConteoNoLeidasAsync();
    }
}
