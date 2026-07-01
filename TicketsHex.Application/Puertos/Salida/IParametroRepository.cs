using TicketsHex.Domain.Entidades.Parametros;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IParametroRepository
    {
        Task<IReadOnlyCollection<RolParametro>> ObtenerRolesAsync();
        Task<IReadOnlyCollection<EstadoTicketParametro>> ObtenerEstadosTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<OrigenTicketParametro>> ObtenerOrigenesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<AreaTicketParametro>> ObtenerAreasTicketAsync(bool incluirInactivos);
    }
}
