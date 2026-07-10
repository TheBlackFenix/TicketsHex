using TicketsHex.Application.DTO_s.Aplicativo;

namespace TicketsHex.Application.Puertos.Entrada.Aplicativo
{
    public interface IAplicativoService
    {
        Task<IReadOnlyCollection<AplicativoDTO>> ObtenerAplicativosAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<AplicativoTicketDTO>> ObtenerAplicativosTicketAsync(Guid idTicket);
        Task<Guid> CrearAplicativoAsync(CrearAplicativoRequest request);
        Task<Guid> AsignarAplicativoAsync(Guid idTicket, AsignarAplicativoTicketRequest request);
        Task DesasignarAplicativoAsync(Guid idTicket, Guid idAplicativo);
    }
}
