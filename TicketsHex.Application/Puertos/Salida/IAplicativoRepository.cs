using TicketsHex.Domain.Entidades.Aplicativos;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IAplicativoRepository
    {
        Task<IReadOnlyCollection<Aplicativo>> ObtenerAplicativosAsync(bool incluirInactivos);
        Task<Aplicativo?> ObtenerAplicativoAsync(Guid idAplicativo);
        Task<Aplicativo?> ObtenerAplicativoPorNombreAsync(string nombre);
        Task<IReadOnlyCollection<AplicativoTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket);
        Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idAplicativo);
        Task GuardarAplicativoAsync(Aplicativo aplicativo);
        Task GuardarAsignacionAsync(AplicativoTicket asignacion);
        Task EliminarAsignacionAsync(Guid idTicket, Guid idAplicativo);
    }
}
