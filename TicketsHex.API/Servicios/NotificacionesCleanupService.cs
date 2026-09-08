using TicketsHex.Application.Puertos.Salida;

using TicketsHex.Application.Comun.Configuracion;

namespace TicketsHex.API.Servicios
{
    public sealed class NotificacionesCleanupService : BackgroundService
    {
        private readonly TimeSpan _intervalo;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacionesCleanupService> _logger;

        public NotificacionesCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificacionesCleanupService> logger,
            NotificacionesOptions options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _intervalo = TimeSpan.FromHours(options.HorasIntervaloLimpieza);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LimpiarAsync(stoppingToken);
            using var timer = new PeriodicTimer(_intervalo);
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await LimpiarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
        }

        private async Task LimpiarAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<INotificacionUsuarioRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<INotificacionPublisher>();
                var destinatarios = await repository.EliminarExpiradasOFinalizadasAsync(
                    DateTimeOffset.UtcNow);
                if (!cancellationToken.IsCancellationRequested)
                    await publisher.PublicarConteosNoLeidasAsync(destinatarios);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "No se pudo completar la limpieza de notificaciones.");
            }
        }
    }
}
