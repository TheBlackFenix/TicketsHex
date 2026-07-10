using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TicketsHex.Application.Puertos.Entrada.Notificacion;

namespace TicketsHex.API.Hubs
{
    [Authorize]
    public sealed class NotificacionesHub : Hub
    {
        public const string Ruta = "/hubs/notificaciones";
        public const string GrupoPlanner = "notificaciones-planner";
        public const string GrupoLiderTecnico = "notificaciones-lider-tecnico";
        public const string EventoResumenActualizado = "notificacionesActualizadas";

        private readonly INotificacionQuery _query;

        public NotificacionesHub(INotificacionQuery query)
        {
            _query = query;
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.User?.IsInRole("Planner") == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPlanner);
            if (Context.User?.IsInRole("LiderTecnico") == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoLiderTecnico);

            await Clients.Caller.SendAsync(EventoResumenActualizado, await _query.ObtenerResumenAsync());
            await base.OnConnectedAsync();
        }

        public async Task ObtenerResumen()
        {
            await Clients.Caller.SendAsync(EventoResumenActualizado, await _query.ObtenerResumenAsync());
        }
    }
}
