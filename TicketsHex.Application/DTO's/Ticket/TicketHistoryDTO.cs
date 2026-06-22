using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public record TicketHistoryDTO
    {
        public TicketEstado EstadoOrigen { get; private set; }
        public TicketEstado EstadoDestino { get; private set; }
        public int? IdUsuarioAccion { get; private set; }
        public Rol RolUsuario { get; private set; }
        public string? Comentario { get; private set; }
        public DateTimeOffset FechaAccion { get; private set; }
    }
}
