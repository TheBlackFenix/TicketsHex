using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public class TicketModificacion
    {
        public Guid Id { get; private set; }
        public TicketEstado EstadoOrigen { get; private set; }
        public TicketEstado EstadoDestino { get; private set; }
        public int? IdUsuarioAccion { get; private set; }
        public Rol RolUsuario { get; private set; }
        public string? Comentario { get; private set; }
        public DateTimeOffset FechaAccion { get; private set; }

        private TicketModificacion() { } // Requerido por ORMs como EF Core

        public TicketModificacion(TicketEstado origen, TicketEstado destino, int? idUsuario, Rol rol, string? comentario)
        {
            Id = Guid.NewGuid();
            EstadoOrigen = origen;
            EstadoDestino = destino;
            IdUsuarioAccion = idUsuario;
            RolUsuario = rol;
            Comentario = comentario;
            FechaAccion = DateTimeOffset.UtcNow;
        }
    }
}
