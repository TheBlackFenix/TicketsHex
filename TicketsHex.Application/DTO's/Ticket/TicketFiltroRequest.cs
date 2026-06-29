using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record TicketFiltroRequest(
        int Pagina = 1,
        int TamanoPagina = 20,
        TicketEstado? Estado = null,
        TicketOrigen? Origen = null,
        long? IdUsuarioAsignado = null,
        string? CodigoCaso = null,
        DateTimeOffset? Desde = null,
        DateTimeOffset? Hasta = null,
        bool IncluirEliminados = false)
    {
        public TicketFiltroRequest Normalizar()
        {
            if (Pagina < 1)
                throw new ArgumentException("La página debe ser mayor o igual a 1.", nameof(Pagina));
            if (TamanoPagina is < 1 or > 100)
                throw new ArgumentException("El tamaño de página debe estar entre 1 y 100.", nameof(TamanoPagina));
            if (Desde.HasValue && Hasta.HasValue && Desde > Hasta)
                throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");

            return this with { CodigoCaso = CodigoCaso?.Trim() };
        }
    }
}
