using TicketsHex.Domain.Entidades.Notificacion;
using TicketsHex.Domain.Enums;
using TicketEntity = TicketsHex.Domain.Entidades.Ticket.Ticket;

namespace TicketsHex.Application.Puertos.Entrada.Notificacion
{
    public sealed record ContextoNotificacionTicket(
        TicketEstado Estado,
        bool TieneHu,
        bool TieneCarpetaMedios);

    public sealed record PreparacionNotificacionTicket(
        IReadOnlyCollection<NotificacionUsuario> Notificaciones,
        IReadOnlyCollection<long> DestinatariosConteo)
    {
        public static PreparacionNotificacionTicket Vacia { get; } = new([], []);
    }

    public interface INotificacionTicketService
    {
        ContextoNotificacionTicket CapturarContexto(TicketEntity ticket);
        Task<PreparacionNotificacionTicket> PrepararCreacionAsync(TicketEntity ticket);
        Task<PreparacionNotificacionTicket> PrepararActualizacionAsync(
            TicketEntity ticket,
            ContextoNotificacionTicket contextoAnterior);
        Task<PreparacionNotificacionTicket> PrepararAsignacionAsync(
            TicketEntity ticket,
            long idUsuario,
            string mensaje);
        Task<PreparacionNotificacionTicket> PrepararEliminacionAsync(TicketEntity ticket);
        Task PublicarAsync(
            TicketEntity ticket,
            PreparacionNotificacionTicket preparacion,
            bool publicarResumen = true);
    }
}
