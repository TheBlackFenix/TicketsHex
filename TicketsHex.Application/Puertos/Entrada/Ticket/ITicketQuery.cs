using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsHex.Application.Puertos.Entrada.Ticket
{
    internal interface ITicketQuery
    {
        Task<TicketDTO?> ObtenerTicketPorIdAsync(Guid id);
        Task<IEnumerable<TicketDTO>> ObtenerListaTicketsAsync();
    }
}
