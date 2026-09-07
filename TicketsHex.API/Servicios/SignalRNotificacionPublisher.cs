using Microsoft.AspNetCore.SignalR;
using TicketsHex.API.Hubs;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Application.DTO_s.Notificacion;

namespace TicketsHex.API.Servicios
{
    public sealed class SignalRNotificacionPublisher : INotificacionPublisher
    {
        private readonly IHubContext<NotificacionesHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SignalRNotificacionPublisher> _logger;

        public SignalRNotificacionPublisher(
            IHubContext<NotificacionesHub> hubContext,
            IServiceScopeFactory scopeFactory,
            ILogger<SignalRNotificacionPublisher> logger)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task PublicarResumenAsync()
        {
            await PublicarResumenRolAsync(
                Domain.Enums.Rol.Planner,
                NotificacionesHub.GrupoPlanner);
            await PublicarResumenRolAsync(
                Domain.Enums.Rol.LiderTecnico,
                NotificacionesHub.GrupoLiderTecnico);
        }

        public async Task PublicarNotificacionesAsync(
            IReadOnlyCollection<NotificacionEventoDTO> notificaciones)
        {
            foreach (var evento in notificaciones)
            {
                try
                {
                    await _hubContext.Clients
                        .Group(NotificacionesHub.GrupoUsuario(evento.IdUsuarioDestinatario))
                        .SendAsync(
                            NotificacionesHub.EventoNotificacionRecibida,
                            evento.Notificacion);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "No se pudo emitir en tiempo real la notificación {IdNotificacion}.",
                        evento.Notificacion.IdNotificacion);
                }
            }

            await PublicarConteosNoLeidasAsync(
                notificaciones.Select(item => item.IdUsuarioDestinatario).ToArray());
        }

        public async Task PublicarConteosNoLeidasAsync(IReadOnlyCollection<long> idsUsuarios)
        {
            foreach (var idUsuario in idsUsuarios.Where(item => item > 0).Distinct())
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider
                        .GetRequiredService<INotificacionUsuarioRepository>();
                    var conteo = new ConteoNotificacionesDTO(
                        await repository.ObtenerCantidadNoLeidasAsync(idUsuario));
                    await _hubContext.Clients
                        .Group(NotificacionesHub.GrupoUsuario(idUsuario))
                        .SendAsync(
                            NotificacionesHub.EventoConteoNoLeidasActualizado,
                            conteo);
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "No se pudo actualizar el conteo de notificaciones del usuario {IdUsuario}.",
                        idUsuario);
                }
            }
        }

        private async Task PublicarResumenRolAsync(
            Domain.Enums.Rol rol,
            string grupo)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var usuario = scope.ServiceProvider
                    .GetRequiredService<Application.Comun.Seguridad.UsuarioActualTemporal>();
                usuario.Establecer(1, rol);
                var resumen = await scope.ServiceProvider
                    .GetRequiredService<INotificacionQuery>()
                    .ObtenerResumenAsync();
                await _hubContext.Clients
                    .Group(grupo)
                    .SendAsync(NotificacionesHub.EventoResumenActualizado, resumen);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "No se pudo emitir el resumen de notificaciones para el rol {Rol}.",
                    rol);
            }
        }
    }
}
