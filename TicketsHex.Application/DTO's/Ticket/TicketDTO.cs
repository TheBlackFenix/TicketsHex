using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public record TicketDTO(
        Guid IdTicket,
        string IdCaso,
        string Titulo,
        string Descripcion,
        TicketEstado TicketEstado,
        long? IdUsuarioAsignado,
        DateTimeOffset FechaCreacion,
        DateTimeOffset?FechaUltimaActualizacion,
        IEnumerable<TicketHistoryDTO> Comentarios
    );
}
