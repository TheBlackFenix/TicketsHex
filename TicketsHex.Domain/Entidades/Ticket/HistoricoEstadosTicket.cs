using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public class HistoricoEstadosTicket
    {
        public Guid IdHistorico { get; set; }
        public Guid IdTicket { get; set; }
        public TicketEstado? IdEstadoOrigen { get; set; }
        public TicketEstado IdEstadoDestino { get; set; }
        public long IdUsuarioAccion { get; set; }
        public string? Comentario { get; set; }
        public DateTimeOffset FechaCambio { get; set; }
    }
}
