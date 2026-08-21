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
}
