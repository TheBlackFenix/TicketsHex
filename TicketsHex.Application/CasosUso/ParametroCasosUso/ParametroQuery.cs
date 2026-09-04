using TicketsHex.Application.DTO_s.Parametro;
using TicketsHex.Application.Puertos.Entrada.Parametro;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.Application.CasosUso.ParametroCasosUso
{
    public sealed class ParametroQuery : IParametroQuery
    {
        private readonly IParametroRepository _repository;

        public ParametroQuery(IParametroRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerRolesAsync() =>
            (await _repository.ObtenerRolesAsync())
                .Select(item => new ParametroDTO(
                    item.IdRol,
                    item.NombreRol,
                    item.Descripcion,
                    true))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerEstadosTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerEstadosTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    item.IdEstado,
                    item.Estado,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerOrigenesTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerOrigenesTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    item.IdOrigen,
                    item.Origen,
                    null,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerAreasTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerAreasTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    item.IdArea,
                    item.Area,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerTiposEntradaConocimientoAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerTiposEntradaConocimientoAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    item.IdTipoEntrada,
                    item.Nombre,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ResultadoEntradaParametroDTO>> ObtenerResultadosEntradaConocimientoAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerResultadosEntradaConocimientoAsync(incluirInactivos))
                .Select(item => new ResultadoEntradaParametroDTO(
                    item.IdResultado,
                    item.IdTipoEntrada,
                    item.Nombre,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerAmbientesTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerAmbientesTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    item.IdAmbiente,
                    item.Nombre,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerTiposTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerTiposTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    (int)item.IdTipo,
                    item.Tipo,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerPrioridadesTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerPrioridadesTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    (int)item.IdPrioridad,
                    item.Prioridad,
                    item.Descripcion,
                    item.Activo))
                .ToArray();

        public async Task<IReadOnlyCollection<ParametroDTO>> ObtenerImpactosTicketAsync(
            bool incluirInactivos) =>
            (await _repository.ObtenerImpactosTicketAsync(incluirInactivos))
                .Select(item => new ParametroDTO(
                    (int)item.IdImpacto,
                    item.Impacto,
                    item.Descripcion,
                    item.Activo))
                .ToArray();
    }
}
