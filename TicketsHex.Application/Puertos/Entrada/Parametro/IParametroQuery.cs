using TicketsHex.Application.DTO_s.Parametro;

namespace TicketsHex.Application.Puertos.Entrada.Parametro
{
    public interface IParametroQuery
    {
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerRolesAsync();
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerEstadosTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerOrigenesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerAreasTicketAsync(bool incluirInactivos);
    }
}
