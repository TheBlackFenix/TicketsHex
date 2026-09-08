using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Notificacion
{
    public sealed record TicketNotificacionDTO(
        Guid IdTicket,
        string CodigoCaso,
        string Titulo);

    public sealed record NotificacionDetalleDTO(
        int Cantidad,
        IReadOnlyCollection<TicketNotificacionDTO> Tickets);

    public sealed record NotificacionPlannerDTO(
        NotificacionDetalleDTO TicketsDesarrolloSinHu);

    public sealed record NotificacionLiderTecnicoDTO(
        NotificacionDetalleDTO TicketsDesarrolloSinCarpetaMedios,
        NotificacionDetalleDTO TicketsDesarrolloSinRamas,
        NotificacionDetalleDTO TicketsDesarrolloSinCarpetaMediosORamas);

    public sealed record NotificacionResumenDTO(
        NotificacionPlannerDTO? Planner,
        NotificacionLiderTecnicoDTO? LiderTecnico);

    public sealed record NotificacionUsuarioDTO(
        Guid IdNotificacion,
        TipoNotificacion Tipo,
        string Mensaje,
        DateTimeOffset Fecha,
        bool Leida,
        Guid IdTicket,
        string CodigoCaso,
        string Titulo);

    public sealed record NotificacionEventoDTO(
        long IdUsuarioDestinatario,
        NotificacionUsuarioDTO Notificacion);

    public sealed record ConteoNotificacionesDTO(int NoLeidas);

    public sealed record NotificacionFiltroRequest(
        int Pagina = 1,
        int TamanoPagina = 20)
    {
        public NotificacionFiltroRequest Normalizar()
        {
            if (Pagina < 1)
                throw new ArgumentException("La página debe ser mayor o igual a 1.", nameof(Pagina));
            if (TamanoPagina is < 1 or > 100)
                throw new ArgumentException("El tamaño de página debe estar entre 1 y 100.", nameof(TamanoPagina));

            return this;
        }
    }
}
