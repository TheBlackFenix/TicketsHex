using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.API.Servicios
{
    public sealed class NotificacionesCleanupService : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacionesCleanupService> _logger;

        public NotificacionesCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificacionesCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LimpiarAsync(stoppingToken);
            using var timer = new PeriodicTimer(Intervalo);
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
