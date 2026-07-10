using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.NotificacionCasosUso
{
    public sealed class NotificacionQuery : INotificacionQuery
    {
        private readonly INotificacionRepository _repository;
        private readonly IUsuarioActual _usuarioActual;

        public NotificacionQuery(
            INotificacionRepository repository,
            IUsuarioActual usuarioActual)
        {
            _repository = repository;
            _usuarioActual = usuarioActual;
        }

        public async Task<NotificacionResumenDTO> ObtenerResumenAsync()
        {
            var planner = _usuarioActual.Rol == Rol.Planner
                ? new NotificacionPlannerDTO(
                    CrearDetalle(await _repository.ObtenerTicketsDesarrolloSinHuAsync()))
                : null;

            var liderTecnico = _usuarioActual.Rol == Rol.LiderTecnico
                ? new NotificacionLiderTecnicoDTO(
                    CrearDetalle(await _repository.ObtenerTicketsDesarrolloSinCarpetaMediosAsync()),
                    CrearDetalle(await _repository.ObtenerTicketsDesarrolloSinRamasAsync()),
                    CrearDetalle(await _repository.ObtenerTicketsDesarrolloSinCarpetaMediosORamasAsync()))
                : null;

            return new NotificacionResumenDTO(planner, liderTecnico);
        }

        private static NotificacionDetalleDTO CrearDetalle(
            IReadOnlyCollection<TicketNotificacionDTO> tickets) =>
            new(tickets.Count, tickets);
    }
}
