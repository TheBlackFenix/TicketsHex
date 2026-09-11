using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Entidades.ConfiguracionGit;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IAplicativoRepository
    {
        Task<IReadOnlyCollection<Aplicativo>> ObtenerAplicativosAsync(bool incluirInactivos);
        Task<Aplicativo?> ObtenerAplicativoAsync(Guid idAplicativo);
        Task<Aplicativo?> ObtenerAplicativoPorNombreAsync(string nombre);
        Task<IReadOnlyCollection<AplicativoTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket);
        Task<IReadOnlyCollection<RepositorioAplicativo>> ObtenerRelacionesRepositorioAsync(Guid idAplicativo);
        Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAplicativoAsync(Guid idAplicativo);
        Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idAplicativo);
        Task<bool> ExisteRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio);
        Task<bool> TieneRamasTicketSinRespaldoAsync(Guid idTicket, Guid idAplicativo);
        Task<bool> TieneTicketsActivosDependientesAsync(Guid idAplicativo, Guid idRepositorio);
        Task GuardarAplicativoAsync(
            Aplicativo aplicativo,
            IReadOnlyCollection<RepositorioAplicativo> relaciones);
        Task GuardarRelacionRepositorioAsync(RepositorioAplicativo relacion);
        Task EliminarRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio);
        Task GuardarAsignacionAsync(AplicativoTicket asignacion);
        Task EliminarAsignacionAsync(Guid idTicket, Guid idAplicativo);
    }
}
