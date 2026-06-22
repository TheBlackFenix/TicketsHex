using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public record TicketDTO(
        Guid Id,
        string CodigoCaso,
        string Titulo,
        string Descripcion,
        TicketEstado TicketEstado,
        int IdUsuarioAsignado,
        DateTimeOffset FechaAsignacion
    );
}
