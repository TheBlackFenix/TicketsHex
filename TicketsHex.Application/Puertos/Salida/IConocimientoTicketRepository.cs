using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Conocimiento;
using TicketsHex.Domain.Entidades.Conocimiento;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IConocimientoTicketRepository
    {
        Task<IReadOnlyCollection<EntradaConocimientoTicket>> ObtenerEntradasTicketAsync(Guid idTicket);
        Task<EntradaConocimientoTicket?> ObtenerEntradaAsync(Guid idEntrada);
        Task<IReadOnlyCollection<RevisionEntradaConocimiento>> ObtenerRevisionesAsync(Guid idEntrada);
        Task<PaginaResultado<EntradaConocimientoTicket>> BuscarAsync(ConocimientoFiltroRequest filtro);
        Task<IReadOnlyCollection<TagConocimiento>> ObtenerTagsTicketAsync(Guid idTicket);
        Task<bool> ExisteResultadoActivoAsync(TipoEntradaConocimiento tipo, int idResultado);
        Task<bool> ExisteAmbienteActivoAsync(int idAmbiente);
        Task GuardarEntradaAsync(
            EntradaConocimientoTicket entrada,
            IReadOnlyCollection<string>? tags,
            IReadOnlyCollection<Guid>? idsAplicativos);
        Task ActualizarEntradaAsync(
            EntradaConocimientoTicket entrada,
            IReadOnlyCollection<string>? tags,
            IReadOnlyCollection<Guid>? idsAplicativos);
    }
}
