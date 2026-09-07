using TicketsHex.Domain.Entidades.Parametros;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IParametroRepository
    {
        Task<IReadOnlyCollection<RolParametro>> ObtenerRolesAsync();
        Task<IReadOnlyCollection<EstadoTicketParametro>> ObtenerEstadosTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<OrigenTicketParametro>> ObtenerOrigenesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<AreaTicketParametro>> ObtenerAreasTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<TipoEntradaConocimientoParametro>> ObtenerTiposEntradaConocimientoAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ResultadoEntradaConocimientoParametro>> ObtenerResultadosEntradaConocimientoAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<AmbienteTicketParametro>> ObtenerAmbientesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<TipoTicketParametro>> ObtenerTiposTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<PrioridadTicketParametro>> ObtenerPrioridadesTicketAsync(bool incluirInactivos);
        Task<IReadOnlyCollection<ImpactoTicketParametro>> ObtenerImpactosTicketAsync(bool incluirInactivos);
    }
}
