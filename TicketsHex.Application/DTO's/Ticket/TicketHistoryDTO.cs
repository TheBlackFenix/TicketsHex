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
        public TicketEstado EstadoOrigen { get; set; }
        public TicketEstado EstadoDestino { get; set; }
        public long? IdUsuarioAccion { get; set; }
        public string? Comentario { get; set; }
        public DateTimeOffset FechaAccion { get; set; }
    }
}
