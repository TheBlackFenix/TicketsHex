using TicketsHex.Application.DTO_s.Notificacion;

namespace TicketsHex.Application.Puertos.Entrada.Notificacion
{
    public interface INotificacionQuery
    {
        Task<NotificacionResumenDTO> ObtenerResumenAsync();
    }
}
