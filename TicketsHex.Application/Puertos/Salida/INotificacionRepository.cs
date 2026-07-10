using TicketsHex.Application.DTO_s.Notificacion;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface INotificacionRepository
    {
        Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinHuAsync();
        Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosAsync();
        Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinRamasAsync();
        Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosORamasAsync();
    }
}
