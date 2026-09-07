using TicketsHex.Application.DTO_s.Notificacion;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface INotificacionPublisher
    {
        Task PublicarResumenAsync();
        Task PublicarNotificacionesAsync(IReadOnlyCollection<NotificacionEventoDTO> notificaciones) =>
            Task.CompletedTask;
        Task PublicarConteosNoLeidasAsync(IReadOnlyCollection<long> idsUsuarios) =>
            Task.CompletedTask;
    }
}
