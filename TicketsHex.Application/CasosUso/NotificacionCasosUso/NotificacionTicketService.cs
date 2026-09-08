using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Notificacion;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;

using TicketsHex.Application.Comun.Configuracion;

namespace TicketsHex.Application.CasosUso.NotificacionCasosUso
{
    public sealed class NotificacionTicketService : INotificacionTicketService
    {
        private readonly INotificacionUsuarioRepository _repository;
        private readonly INotificacionPublisher _publisher;
        private readonly IUsuarioActual _usuarioActual;
        private readonly NotificacionesOptions _options;

        public NotificacionTicketService(
            INotificacionUsuarioRepository repository,
            INotificacionPublisher publisher,
            IUsuarioActual usuarioActual,
            NotificacionesOptions? options = null)
        {
            _repository = repository;
            _publisher = publisher;
            _usuarioActual = usuarioActual;
            _options = options ?? new NotificacionesOptions();
        }

        public ContextoNotificacionTicket CapturarContexto(Ticket ticket) => new(
            ticket.IdEstado,
            TieneHu(ticket),
            !string.IsNullOrWhiteSpace(ticket.CarpetaMedios));

        public async Task<PreparacionNotificacionTicket> PrepararCreacionAsync(Ticket ticket)
        {
            var resultado = new List<NotificacionUsuario>();
            var mensajeDesarrollador = (TieneHu(ticket), !string.IsNullOrWhiteSpace(ticket.CarpetaMedios)) switch
            {
                (true, true) => "Se te asignó el ticket con HU y carpeta de medios disponibles.",
                (true, false) => "Se te asignó el ticket con HU disponible.",
                (false, true) => "Se te asignó el ticket con carpeta de medios disponible.",
                _ => "Se te asignó como responsable de desarrollo del ticket."
            };
            Agregar(
                resultado,
                ticket,
                ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo),
                TipoNotificacion.Asignacion,
                mensajeDesarrollador);
            Agregar(
                resultado,
                ticket,
                ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.QA),
                TipoNotificacion.Asignacion,
                "Se te asignó como responsable de QA del ticket.");
            await _repository.PrepararNotificacionesAsync(resultado);
            return new PreparacionNotificacionTicket(resultado, []);
        }

        public async Task<PreparacionNotificacionTicket> PrepararActualizacionAsync(
            Ticket ticket,
            ContextoNotificacionTicket contextoAnterior)
        {
            if (ticket.IdEstado == TicketEstado.Finalizado)
                return await PrepararEliminacionAsync(ticket);

            var resultado = new List<NotificacionUsuario>();
            if (contextoAnterior.Estado != ticket.IdEstado)
                await AgregarCambioEstadoAsync(resultado, ticket, contextoAnterior.Estado);

            var agregoHu = !contextoAnterior.TieneHu && TieneHu(ticket);
            var agregoCarpeta = !contextoAnterior.TieneCarpetaMedios &&
                !string.IsNullOrWhiteSpace(ticket.CarpetaMedios);
            if (agregoHu || agregoCarpeta)
            {
                var mensaje = (agregoHu, agregoCarpeta) switch
                {
                    (true, true) => "El ticket ya tiene HU y carpeta de medios disponibles.",
                    (true, false) => "El ticket ya tiene HU disponible.",
                    _ => "El ticket ya tiene carpeta de medios disponible."
                };
                Agregar(
                    resultado,
                    ticket,
                    ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo),
                    TipoNotificacion.DatosDesarrolloDisponibles,
                    mensaje);
            }

            await _repository.PrepararNotificacionesAsync(resultado);
            return new PreparacionNotificacionTicket(resultado, []);
        }

        public async Task<PreparacionNotificacionTicket> PrepararAsignacionAsync(
            Ticket ticket,
            long idUsuario,
            string mensaje)
        {
            var resultado = new List<NotificacionUsuario>();
            Agregar(
                resultado,
                ticket,
                idUsuario,
                TipoNotificacion.Asignacion,
                mensaje);
            await _repository.PrepararNotificacionesAsync(resultado);
            return new PreparacionNotificacionTicket(resultado, []);
        }

        public async Task<PreparacionNotificacionTicket> PrepararEliminacionAsync(Ticket ticket)
        {
            var destinatarios = await _repository.PrepararEliminacionPorTicketAsync(ticket.IdTicket);
            return new PreparacionNotificacionTicket([], destinatarios);
        }

        public async Task PublicarAsync(
            Ticket ticket,
            PreparacionNotificacionTicket preparacion,
            bool publicarResumen = true)
        {
            var eventos = preparacion.Notificaciones
                .Select(item => new NotificacionEventoDTO(
                    item.IdUsuarioDestinatario,
                    new NotificacionUsuarioDTO(
                        item.IdNotificacion,
                        item.IdTipoNotificacion,
                        item.Mensaje,
                        item.FechaCreacion,
                        item.Leida,
                        ticket.IdTicket,
                        ticket.CodigoCaso.Valor,
                        ticket.Titulo.Value)))
                .ToArray();
            await _publisher.PublicarNotificacionesAsync(eventos);
            await _publisher.PublicarConteosNoLeidasAsync(preparacion.DestinatariosConteo);
            if (publicarResumen)
                await _publisher.PublicarResumenAsync();
        }

        private async Task AgregarCambioEstadoAsync(
            ICollection<NotificacionUsuario> resultado,
            Ticket ticket,
            TicketEstado estadoAnterior)
        {
            if (ticket.IdEstado == TicketEstado.Bloqueado)
            {
                var supervisores = await _repository.ObtenerIdsUsuariosActivosPorRolesAsync(
                    [Rol.Planner, Rol.LiderTecnico]);
                foreach (var idUsuario in supervisores)
                {
                    Agregar(
                        resultado,
                        ticket,
                        idUsuario,
                        TipoNotificacion.Bloqueo,
                        "El ticket fue bloqueado por un stopper.");
                }
            }
            else if (ticket.IdEstado == TicketEstado.BUG)
            {
                AgregarAResponsables(
                    resultado,
                    ticket,
                    TipoNotificacion.Bug,
                    "El ticket fue marcado como BUG.");
            }
            else if (ticket.IdEstado == TicketEstado.Rollback)
            {
                AgregarAResponsables(
                    resultado,
                    ticket,
                    TipoNotificacion.Rollback,
                    "El ticket fue enviado a Rollback.");
            }
            else if (estadoAnterior == TicketEstado.EnReplicaQA &&
                     ticket.IdEstado == TicketEstado.EnAnalisis)
            {
                AgregarAResponsables(
                    resultado,
                    ticket,
                    TipoNotificacion.DevolucionQa,
                    "El ticket regresó de réplica QA a desarrollo.");
            }
            else if (ticket.IdEstado is TicketEstado.EnReplicaQA or
                     TicketEstado.EnRevisionApitesting or
                     TicketEstado.EnRevisionQA)
            {
                Agregar(
                    resultado,
                    ticket,
                    ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.QA),
                    TipoNotificacion.SolicitudQa,
                    $"El ticket requiere atención de QA en {ticket.IdEstado}.");
            }
        }

        private void AgregarAResponsables(
            ICollection<NotificacionUsuario> resultado,
            Ticket ticket,
            TipoNotificacion tipo,
            string mensaje)
        {
            Agregar(
                resultado,
                ticket,
                ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo),
                tipo,
                mensaje);
            Agregar(
                resultado,
                ticket,
                ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.QA),
                tipo,
                mensaje);
        }

        private void Agregar(
            ICollection<NotificacionUsuario> resultado,
            Ticket ticket,
            long? idUsuario,
            TipoNotificacion tipo,
            string mensaje)
        {
            if (!idUsuario.HasValue || idUsuario.Value == _usuarioActual.IdUsuario)
                return;
            if (resultado.Any(item =>
                    item.IdUsuarioDestinatario == idUsuario.Value &&
                    item.IdTipoNotificacion == tipo))
                return;

            resultado.Add(new NotificacionUsuario(
                idUsuario.Value,
                ticket.IdTicket,
                tipo,
                mensaje,
                diasRetencion: _options.DiasRetencion));
        }

        private static bool TieneHu(Ticket ticket) =>
            !string.IsNullOrWhiteSpace(ticket.NombreHu) &&
            !string.IsNullOrWhiteSpace(ticket.UrlHu);
    }
}
