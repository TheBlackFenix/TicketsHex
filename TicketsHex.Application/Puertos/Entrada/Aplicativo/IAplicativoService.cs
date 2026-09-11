using TicketsHex.Application.DTO_s.Aplicativo;
using TicketsHex.Application.DTO_s.Repositorio;

namespace TicketsHex.Application.Puertos.Entrada.Aplicativo
{
    public interface IAplicativoService
    {
        Task<IReadOnlyCollection<AplicativoDTO>> ObtenerAplicativosAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<AplicativoTicketDTO>> ObtenerAplicativosTicketAsync(Guid idTicket);
        Task<IReadOnlyCollection<RepositorioAplicativoDTO>> ObtenerRepositoriosAplicativoAsync(Guid idAplicativo);
        Task<Guid> CrearAplicativoAsync(CrearAplicativoRequest request);
        Task<Guid> AsignarRepositorioAsync(Guid idAplicativo, AsignarRepositorioAplicativoRequest request);
        Task DesasignarRepositorioAsync(Guid idAplicativo, Guid idRepositorio);
        Task<Guid> AsignarAplicativoAsync(Guid idTicket, AsignarAplicativoTicketRequest request);
        Task DesasignarAplicativoAsync(Guid idTicket, Guid idAplicativo);
    }
}
