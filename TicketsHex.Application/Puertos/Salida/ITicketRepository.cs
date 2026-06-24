using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface ITicketRepository
    {
        Task<Ticket> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<Ticket>> ObtenerTodosAsync();
        Task<Ticket> ObtenerTicketPorCodigoYUsuerioAsync(Guid id, int idUsuarioAsignado);
        Task<IEnumerable<Ticket>> ObtenerTodosPorIdUsuarioAsignadoAsync(int idUsuarioAsignado);
        Task GuardarAsync(Ticket ticket);
        Task ActualizarAsync(Ticket ticket);
        Task EliminarAsync(Guid id);
    }
}
