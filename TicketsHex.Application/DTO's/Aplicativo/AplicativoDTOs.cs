namespace TicketsHex.Application.DTO_s.Aplicativo
{
    public sealed record AplicativoDTO(
        Guid IdAplicativo,
        string Nombre,
        string? Descripcion,
        bool Activo);

    public sealed record AplicativoTicketDTO(
        Guid IdAplicativoTicket,
        Guid IdTicket,
        Guid IdAplicativo,
        string Aplicativo,
        DateTimeOffset FechaAsignacion);

    public sealed record CrearAplicativoRequest(
        string Nombre,
        string? Descripcion);

    public sealed record AsignarAplicativoTicketRequest(Guid IdAplicativo);
}
