namespace TicketsHex.Application.Comun.Paginacion
{
    public sealed record PaginaResultado<T>(
        IReadOnlyCollection<T> Elementos,
        int Pagina,
        int TamanoPagina,
        int TotalElementos)
    {
        public int TotalPaginas => TotalElementos == 0
            ? 0
            : (int)Math.Ceiling(TotalElementos / (double)TamanoPagina);
    }
}
