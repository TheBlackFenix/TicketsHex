using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Entidades.Ticket;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface ITicketRepository
    {
        Task<Ticket?> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Ticket>> ObtenerTodosAsync();
        Task GuardarAsync(Ticket ticket);
        Task ActualizarAsync(Ticket ticket);
        Task EliminarAsync(Guid id);
    }
}
