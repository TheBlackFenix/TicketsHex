using TicketsHex.API.Reponses;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Comun.Paginacion;

namespace TicketsHex.API.Endpoints
{
    public static class NotificacionesEndpoints
    {
        public static IEndpointRouteBuilder MapNotificacionesEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/notificaciones")
                .WithTags("Notificaciones")
                .WithOpenApi()
                .RequireAuthorization();

            group.MapGet("/resumen", async (INotificacionQuery query) =>
            {
                var resumen = await query.ObtenerResumenAsync();
                return Results.Ok(ApiResponse<NotificacionResumenDTO>.Ok(
                    resumen,
                    "Resumen de notificaciones consultado correctamente."));
            });

            group.MapGet("/", async (
                [AsParameters] NotificacionFiltroRequest filtro,
                INotificacionQuery query) =>
            {
                var notificaciones = await query.ObtenerNotificacionesAsync(filtro);
                return Results.Ok(ApiResponse<PaginaResultado<NotificacionUsuarioDTO>>.Ok(
                    notificaciones,
                    "Notificaciones consultadas correctamente."));
            });

            group.MapGet("/no-leidas", async (INotificacionQuery query) =>
            {
                var conteo = await query.ObtenerConteoNoLeidasAsync();
                return Results.Ok(ApiResponse<ConteoNotificacionesDTO>.Ok(
                    conteo,
                    "Conteo de notificaciones consultado correctamente."));
            });

            group.MapPatch("/{id:guid}/leer", async (
                Guid id,
                INotificacionCommand command) =>
            {
                await command.MarcarLeidaAsync(id);
                return Results.Ok(ApiResponse<bool>.Ok(
                    true,
                    "Notificación marcada como leída."));
            });

            group.MapPatch("/leer-todas", async (INotificacionCommand command) =>
            {
                var cantidad = await command.MarcarTodasLeidasAsync();
                return Results.Ok(ApiResponse<int>.Ok(
                    cantidad,
                    "Notificaciones marcadas como leídas."));
            });

            return app;
        }
    }
}
