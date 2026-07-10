using Microsoft.AspNetCore.SignalR;
using TicketsHex.API.Hubs;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.API.Servicios
{
    public sealed class SignalRNotificacionPublisher : INotificacionPublisher
    {
        private readonly IHubContext<NotificacionesHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;

        public SignalRNotificacionPublisher(
            IHubContext<NotificacionesHub> hubContext,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
        }

        public async Task PublicarResumenAsync()
        {
            using var scopePlanner = _scopeFactory.CreateScope();
            var usuarioPlanner = scopePlanner.ServiceProvider
                .GetRequiredService<Application.Comun.Seguridad.UsuarioActualTemporal>();
            usuarioPlanner.Establecer(1, Domain.Enums.Rol.Planner);
            var resumenPlanner = await scopePlanner.ServiceProvider
                .GetRequiredService<INotificacionQuery>()
                .ObtenerResumenAsync();
            await _hubContext.Clients
                .Group(NotificacionesHub.GrupoPlanner)
                .SendAsync(NotificacionesHub.EventoResumenActualizado, resumenPlanner);

            using var scopeLider = _scopeFactory.CreateScope();
            var usuarioLider = scopeLider.ServiceProvider
                .GetRequiredService<Application.Comun.Seguridad.UsuarioActualTemporal>();
            usuarioLider.Establecer(1, Domain.Enums.Rol.LiderTecnico);
            var resumenLider = await scopeLider.ServiceProvider
                .GetRequiredService<INotificacionQuery>()
                .ObtenerResumenAsync();
            await _hubContext.Clients
                .Group(NotificacionesHub.GrupoLiderTecnico)
                .SendAsync(NotificacionesHub.EventoResumenActualizado, resumenLider);
        }
    }
}
