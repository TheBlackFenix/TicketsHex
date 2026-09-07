using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.Application.CasosUso.NotificacionCasosUso
{
    public sealed class NotificacionCommand : INotificacionCommand
    {
        private readonly INotificacionUsuarioRepository _repository;
        private readonly INotificacionPublisher _publisher;
        private readonly IUsuarioActual _usuarioActual;

        public NotificacionCommand(
            INotificacionUsuarioRepository repository,
            INotificacionPublisher publisher,
            IUsuarioActual usuarioActual)
        {
            _repository = repository;
            _publisher = publisher;
            _usuarioActual = usuarioActual;
        }

        public async Task MarcarLeidaAsync(Guid idNotificacion)
        {
            var notificacion = await _repository.ObtenerPorIdUsuarioAsync(
                    idNotificacion,
                    _usuarioActual.IdUsuario)
                ?? throw new RecursoNoEncontradoException("Notificación no encontrada.");

            notificacion.MarcarComoLeida(DateTimeOffset.UtcNow);
            await _repository.GuardarCambiosAsync();
            await _publisher.PublicarConteosNoLeidasAsync([_usuarioActual.IdUsuario]);
        }

        public async Task<int> MarcarTodasLeidasAsync()
        {
            var notificaciones = await _repository.ObtenerNoLeidasUsuarioAsync(
                _usuarioActual.IdUsuario);
            var fechaLectura = DateTimeOffset.UtcNow;
            foreach (var notificacion in notificaciones)
                notificacion.MarcarComoLeida(fechaLectura);

            if (notificaciones.Count > 0)
                await _repository.GuardarCambiosAsync();
            await _publisher.PublicarConteosNoLeidasAsync([_usuarioActual.IdUsuario]);
            return notificaciones.Count;
        }
    }
}
