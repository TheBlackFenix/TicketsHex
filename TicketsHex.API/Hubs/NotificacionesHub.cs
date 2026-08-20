using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketsHex.Application.Comun.Seguridad;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Domain.Enums; // Aquí vive tu UsuarioActualTemporal

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
        // 1. Declaramos la variable privada para el estado temporal
        private readonly UsuarioActualTemporal _usuarioActual;

        // 2. Inyectamos la dependencia en el constructor
        public NotificacionesHub(INotificacionQuery query, UsuarioActualTemporal usuarioActual)
        {
            _query = query;
            _usuarioActual = usuarioActual;
        }

        public override async Task OnConnectedAsync()
        {
            // 3. Alimentamos el servicio Scoped antes de hacer la consulta
            if (!EstablecerUsuarioActual())
            {
                Context.Abort();
                return;
            }

            if (Context.User?.IsInRole("Planner") == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPlanner);
            if (Context.User?.IsInRole("LiderTecnico") == true)
                await Groups.AddToGroupAsync(Context.ConnectionId, GrupoLiderTecnico);

            // Ahora esta línea funcionará perfectamente porque el usuario ya está establecido
            await Clients.Caller.SendAsync(EventoResumenActualizado, await _query.ObtenerResumenAsync());
            await base.OnConnectedAsync();
        }

        public async Task ObtenerResumen()
        {
            // Alimentamos el servicio temporal también aquí por si el cliente invoca 
            // este método manualmente después de haberse conectado
            if (!EstablecerUsuarioActual())
            {
                Context.Abort();
                return;
            }

            await Clients.Caller.SendAsync(EventoResumenActualizado, await _query.ObtenerResumenAsync());
        }

        // Método auxiliar para evitar repetir la misma lógica
        private bool EstablecerUsuarioActual()
        {
            var userIdString = Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var rol = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

            if (long.TryParse(userIdString, out var userId) && !string.IsNullOrWhiteSpace(rol))
            {
                Enum.TryParse<Rol>(rol, true, out var rolEnum);
                _usuarioActual.Establecer(userId, rolEnum);
                return true;
            }

            return false;
        }
    }
}
