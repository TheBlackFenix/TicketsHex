namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record HistorialAsignacionTicketFiltroRequest(
        int Pagina = 1,
        int TamanoPagina = 20)
    {
        public HistorialAsignacionTicketFiltroRequest Normalizar()
        {
            if (Pagina < 1)
                throw new ArgumentException("La página debe ser mayor o igual a 1.", nameof(Pagina));
            if (TamanoPagina is < 1 or > 100)
                throw new ArgumentException(
                    "El tamaño de página debe estar entre 1 y 100.",
                    nameof(TamanoPagina));

            return this;
        }
    }
}
