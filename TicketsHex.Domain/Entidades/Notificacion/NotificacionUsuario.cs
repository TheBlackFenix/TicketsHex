using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Notificacion
{
    public sealed class NotificacionUsuario
    {
        public const int DiasRetencion = 14;

        public Guid IdNotificacion { get; private set; }
        public long IdUsuarioDestinatario { get; private set; }
        public Guid IdTicket { get; private set; }
        public TipoNotificacion IdTipoNotificacion { get; private set; }
        public string Mensaje { get; private set; } = string.Empty;
        public DateTimeOffset FechaCreacion { get; private set; }
        public DateTimeOffset? FechaLectura { get; private set; }
        public DateTimeOffset FechaExpiracion { get; private set; }

        private NotificacionUsuario() { }

        public NotificacionUsuario(
            long idUsuarioDestinatario,
            Guid idTicket,
            TipoNotificacion tipo,
            string mensaje,
            DateTimeOffset? fechaCreacion = null)
        {
            if (idUsuarioDestinatario <= 0)
                throw new ArgumentException("El destinatario debe ser válido.", nameof(idUsuarioDestinatario));
            if (idTicket == Guid.Empty)
                throw new ArgumentException("El ticket es obligatorio.", nameof(idTicket));
            if (!Enum.IsDefined(tipo))
                throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "El tipo de notificación no es válido.");
            if (string.IsNullOrWhiteSpace(mensaje))
                throw new ArgumentException("El mensaje es obligatorio.", nameof(mensaje));
            if (mensaje.Trim().Length > 250)
                throw new ArgumentException("El mensaje no puede superar 250 caracteres.", nameof(mensaje));

            IdNotificacion = Guid.NewGuid();
            IdUsuarioDestinatario = idUsuarioDestinatario;
            IdTicket = idTicket;
            IdTipoNotificacion = tipo;
            Mensaje = mensaje.Trim();
            FechaCreacion = fechaCreacion ?? DateTimeOffset.UtcNow;
            FechaExpiracion = FechaCreacion.AddDays(DiasRetencion);
        }

        public bool Leida => FechaLectura.HasValue;

        public void MarcarComoLeida(DateTimeOffset fechaLectura)
        {
            if (!FechaLectura.HasValue)
                FechaLectura = fechaLectura;
        }
    }
}
