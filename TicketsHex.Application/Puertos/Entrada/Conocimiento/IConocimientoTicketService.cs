using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Conocimiento;

namespace TicketsHex.Application.Puertos.Entrada.Conocimiento
{
    public interface IConocimientoTicketService
    {
        Task<BaseConocimientoTicketDTO> ObtenerBaseAsync(Guid idTicket);
        Task<PaginaResultado<EntradaConocimientoDTO>> BuscarAsync(ConocimientoFiltroRequest filtro);
        Task<IReadOnlyCollection<RevisionEntradaConocimientoDTO>> ObtenerRevisionesAsync(Guid idTicket, Guid idEntrada);
        Task<Guid> CrearDiagnosticoAsync(Guid idTicket, CrearDiagnosticoRequest request);
        Task<Guid> CrearSolucionAsync(Guid idTicket, CrearSolucionRequest request);
        Task<Guid> CrearValidacionQaAsync(Guid idTicket, CrearValidacionQaRequest request);
        Task ActualizarEntradaAsync(Guid idTicket, Guid idEntrada, ActualizarEntradaConocimientoRequest request);
    }
}
