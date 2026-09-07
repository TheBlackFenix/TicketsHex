using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record UsuarioReferenciaTicketDTO(
        long IdUsuario,
        string Nombre);

    public sealed record HistorialAsignacionTicketDTO(
        Guid IdMovimiento,
        UsuarioReferenciaTicketDTO? UsuarioAnterior,
        UsuarioReferenciaTicketDTO UsuarioNuevo,
        UsuarioReferenciaTicketDTO UsuarioAccion,
        TicketEstado? Estado,
        TipoMovimientoAsignacionTicket? TipoMovimiento,
        string? Comentario,
        DateTimeOffset Fecha)
    {
        public bool EsRegistroMigrado =>
            !Estado.HasValue || !TipoMovimiento.HasValue;
    }
}
