using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record CapacidadesTicketDTO(
        IReadOnlyCollection<AccionTicketPermitida> AccionesPermitidas,
        IReadOnlyCollection<TransicionDisponibleDTO> TransicionesDisponibles);

    public sealed record TransicionDisponibleDTO(
        TicketEstado EstadoDestino,
        TipoTransicionDisponible Tipo,
        bool RequiereComentario);
}
