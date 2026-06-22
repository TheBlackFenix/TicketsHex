using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public record CrearTicketRequest(
        int CodigoCaso,
        string Titulo,
        string Descripcion,
        int IdUsuarioAsignado
    );
}
