using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public class ResponsableTicket
    {
        public Guid IdResponsableTicket { get; private set; }
        public Guid IdTicket { get; private set; }
        public TipoResponsabilidadTicket IdTipoResponsabilidad { get; private set; }
        public long IdUsuario { get; private set; }
        public long IdUsuarioAsignador { get; private set; }

        private ResponsableTicket() { }

        public ResponsableTicket(
            Guid idTicket,
            TipoResponsabilidadTicket tipoResponsabilidad,
            long idUsuario,
            long idUsuarioAsignador)
        {
            if (idTicket == Guid.Empty)
                throw new ArgumentException("El ticket es obligatorio.", nameof(idTicket));
            if (idUsuario <= 0)
                throw new ArgumentException("El usuario responsable debe ser positivo.", nameof(idUsuario));
            if (idUsuarioAsignador <= 0)
                throw new ArgumentException("El usuario asignador debe ser positivo.", nameof(idUsuarioAsignador));

            IdResponsableTicket = Guid.NewGuid();
            IdTicket = idTicket;
            IdTipoResponsabilidad = tipoResponsabilidad;
            IdUsuario = idUsuario;
            IdUsuarioAsignador = idUsuarioAsignador;
        }

        public void Reemplazar(long idUsuario, long idUsuarioAsignador)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("El usuario responsable debe ser positivo.", nameof(idUsuario));
            if (idUsuarioAsignador <= 0)
                throw new ArgumentException("El usuario asignador debe ser positivo.", nameof(idUsuarioAsignador));

            IdUsuario = idUsuario;
            IdUsuarioAsignador = idUsuarioAsignador;
        }
    }
}
