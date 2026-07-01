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
    }
}
