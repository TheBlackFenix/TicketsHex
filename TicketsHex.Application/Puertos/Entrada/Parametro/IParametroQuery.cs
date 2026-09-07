using TicketsHex.Application.DTO_s.Parametro;

namespace TicketsHex.Application.Puertos.Entrada.Parametro
{
    public interface IParametroQuery
    {
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerRolesAsync();
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerEstadosTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerOrigenesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerAreasTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerTiposEntradaConocimientoAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ResultadoEntradaParametroDTO>> ObtenerResultadosEntradaConocimientoAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerAmbientesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerTiposTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerPrioridadesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ParametroDTO>> ObtenerImpactosTicketAsync(bool incluirInactivos);
    }
}
