using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Conocimiento
{
    public sealed class RevisionEntradaConocimiento
    {
        public Guid IdRevision { get; private set; }
        public Guid IdEntrada { get; private set; }
        public string ContenidoAnterior { get; private set; } = string.Empty;
        public long IdUsuarioAccion { get; private set; }
        public Rol RolUsuarioAccion { get; private set; }
        public TicketEstado EstadoTicket { get; private set; }
        public DateTimeOffset FechaRevision { get; private set; }

        private RevisionEntradaConocimiento() { }

        public RevisionEntradaConocimiento(
            Guid idEntrada,
            string contenidoAnterior,
            long idUsuarioAccion,
            Rol rolUsuarioAccion,
            TicketEstado estadoTicket)
        {
            if (idEntrada == Guid.Empty)
                throw new ArgumentException("La entrada es obligatoria.", nameof(idEntrada));
            if (string.IsNullOrWhiteSpace(contenidoAnterior))
                throw new ArgumentException("El contenido anterior es obligatorio.", nameof(contenidoAnterior));
            if (idUsuarioAccion <= 0)
                throw new ArgumentException("El usuario de la acción debe ser válido.", nameof(idUsuarioAccion));

            IdRevision = Guid.NewGuid();
            IdEntrada = idEntrada;
            ContenidoAnterior = contenidoAnterior;
            IdUsuarioAccion = idUsuarioAccion;
            RolUsuarioAccion = rolUsuarioAccion;
            EstadoTicket = estadoTicket;
            FechaRevision = DateTimeOffset.UtcNow;
        }
    }
}
