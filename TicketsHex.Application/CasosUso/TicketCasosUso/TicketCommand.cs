using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.ValueObjects.Ticket;

namespace TicketsHex.Application.CasosUso.TicketCasosUso
{
    public class TicketCommand : ITicketCommand
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IUsuarioActual _usuarioActual;
        private readonly INotificacionPublisher _notificacionPublisher;

        public TicketCommand(
            ITicketRepository ticketRepository,
            IUsuarioRepository usuarioRepository,
            IUsuarioActual usuarioActual,
            INotificacionPublisher notificacionPublisher)
        {
            _ticketRepository = ticketRepository;
            _usuarioRepository = usuarioRepository;
            _usuarioActual = usuarioActual;
            _notificacionPublisher = notificacionPublisher;
        }

        public async Task<Guid> CrearTicketAsync(CrearTicketRequest request)
        {
            if (_usuarioActual.Rol == Rol.QA)
                throw new UnauthorizedAccessException("QA no puede crear tickets.");

            var idDesarrollador = _usuarioActual.Rol == Rol.Desarrollador
                ? _usuarioActual.IdUsuario
                : request.IdUsuarioAsignado;

            if (_usuarioActual.Rol == Rol.Desarrollador &&
                request.IdUsuarioAsignado != _usuarioActual.IdUsuario)
            {
                throw new UnauthorizedAccessException("El desarrollador que crea el ticket debe quedar como responsable de desarrollo.");
            }

            await ValidarUsuarioRolAsync(idDesarrollador, Rol.Desarrollador);
            if (request.IdQaResponsable.HasValue)
                await ValidarUsuarioRolAsync(request.IdQaResponsable.Value, Rol.QA);

            var ticket = new Ticket(
                request.CodigoCaso,
                request.Titulo,
                request.Descripcion,
                idDesarrollador,
                _usuarioActual.IdUsuario,
                request.OrigenTicket,
                request.EsDesarrollo,
                request.IdQaResponsable);

            await _ticketRepository.GuardarAsync(ticket);
            if (ticket.EsDesarrollo)
                await _notificacionPublisher.PublicarResumenAsync();
            return ticket.IdTicket;
        }

        public async Task ActualizarTicketAsync(Guid ticketId, ActualizarTicketRequest request)
        {
            var ticket = await ObtenerTicketActivoAsync(ticketId);
            var huboCambios = false;

            if (request.Titulo is not null)
            {
                ticket.ActualizarTitulo(request.Titulo, _usuarioActual.IdUsuario, _usuarioActual.Rol);
                huboCambios = true;
            }

            if (request.Descripcion is not null)
            {
                ticket.ActualizarDescripcion(
                    new DescripcionVO(request.Descripcion),
                    _usuarioActual.IdUsuario,
                    _usuarioActual.Rol);
                huboCambios = true;
            }

            if (request.CausaRaiz is not null || request.SolucionPropuesta is not null)
            {
                ticket.ActualizarDiagnostico(
                    request.CausaRaiz,
                    request.SolucionPropuesta,
                    _usuarioActual.IdUsuario,
                    _usuarioActual.Rol);
                huboCambios = true;
            }

            if (request.EsDesarrollo.HasValue ||
                request.NombreHu is not null ||
                request.UrlHu is not null ||
                request.CarpetaMedios is not null)
            {
                ticket.ActualizarDatosDesarrollo(
                    request.EsDesarrollo,
                    request.NombreHu,
                    request.UrlHu,
                    request.CarpetaMedios,
                    _usuarioActual.IdUsuario,
                    _usuarioActual.Rol);
                huboCambios = true;
            }

            if (request.NuevoEstado.HasValue)
            {
                ticket.ActualizarEstado(
                    request.NuevoEstado.Value,
                    _usuarioActual.IdUsuario,
                    _usuarioActual.Rol,
                    request.Comentario);
                huboCambios = true;
            }

            if (!huboCambios && !string.IsNullOrWhiteSpace(request.Comentario))
            {
                ticket.AgregarComentarioLibre(
                    request.Comentario,
                    _usuarioActual.IdUsuario,
                    _usuarioActual.Rol);
                huboCambios = true;
            }

            if (!huboCambios)
                throw new ArgumentException("Debe indicar al menos un campo para actualizar.");

            await _ticketRepository.ActualizarAsync(ticket);
            await _notificacionPublisher.PublicarResumenAsync();
        }

        public Task AsignarResponsableDesarrolloAsync(
            Guid ticketId,
            AsignarResponsableTicketRequest request) =>
            AsignarResponsableAsync(
                ticketId,
                request,
                TipoResponsabilidadTicket.Desarrollo,
                Rol.Desarrollador);

        public Task AsignarResponsableQaAsync(
            Guid ticketId,
            AsignarResponsableTicketRequest request) =>
            AsignarResponsableAsync(
                ticketId,
                request,
                TipoResponsabilidadTicket.QA,
                Rol.QA);

        public async Task CambiarResponsableActualAsync(
            Guid ticketId,
            AsignarResponsableTicketRequest request)
        {
            ValidarPlannerOLiderTecnico();
            await ValidarUsuarioExisteAsync(request.IdUsuario);
            var ticket = await ObtenerTicketActivoAsync(ticketId);
            ticket.ReasignarTicket(
                request.IdUsuario,
                _usuarioActual.IdUsuario,
                _usuarioActual.Rol,
                request.Comentario);
            await _ticketRepository.ActualizarAsync(ticket);
            await _notificacionPublisher.PublicarResumenAsync();
        }

        public async Task EliminarTicketAsync(Guid ticketId, string? comentario)
        {
            var ticket = await ObtenerTicketActivoAsync(ticketId);
            ticket.EliminarLogicamente(_usuarioActual.IdUsuario, _usuarioActual.Rol, comentario);
            await _ticketRepository.ActualizarAsync(ticket);
            await _notificacionPublisher.PublicarResumenAsync();
        }

        private async Task<Ticket> ObtenerTicketActivoAsync(Guid ticketId)
        {
            return await _ticketRepository.ObtenerPorIdAsync(ticketId)
                ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");
        }

        private async Task ValidarUsuarioExisteAsync(long idUsuario)
        {
            if (!await _usuarioRepository.ExisteAsync(idUsuario))
                throw new RecursoNoEncontradoException($"El usuario {idUsuario} no existe o está inactivo.");
        }

        private async Task AsignarResponsableAsync(
            Guid ticketId,
            AsignarResponsableTicketRequest request,
            TipoResponsabilidadTicket tipoResponsabilidad,
            Rol rolEsperado)
        {
            ValidarPlannerOLiderTecnico();
            await ValidarUsuarioRolAsync(request.IdUsuario, rolEsperado);
            var ticket = await ObtenerTicketActivoAsync(ticketId);
            ticket.AsignarResponsable(
                tipoResponsabilidad,
                request.IdUsuario,
                _usuarioActual.IdUsuario,
                _usuarioActual.Rol,
                request.Comentario);
            await _ticketRepository.ActualizarAsync(ticket);
            await _notificacionPublisher.PublicarResumenAsync();
        }

        private async Task<Usuario> ValidarUsuarioRolAsync(long idUsuario, Rol rolEsperado)
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(idUsuario)
                ?? throw new RecursoNoEncontradoException($"El usuario {idUsuario} no existe.");
            if (!usuario.Activo)
                throw new InvalidOperationException($"El usuario {idUsuario} está inactivo.");
            if (usuario.IdRol != rolEsperado)
                throw new ArgumentException($"El usuario {idUsuario} debe tener rol {rolEsperado}.");
            return usuario;
        }

        private void ValidarPlannerOLiderTecnico()
        {
            if (_usuarioActual.Rol is not Rol.Planner and not Rol.LiderTecnico)
                throw new UnauthorizedAccessException("Solo Planner o Líder Técnico pueden asignar responsables.");
        }
    }
}
