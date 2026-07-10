using TicketsHex.API.Reponses;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Entrada.Notificacion;

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

            return app;
        }
    }
}
