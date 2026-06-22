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
        Task<TicketDTO> ObtenerPorIdAsync(Guid id);
        Task<IEnumerable<TicketDTO>> ObtenerTodosAsync();
        Task<TicketDTO?> ObtenerTicketPorCodigoYUsuerioAsync(Guid id, int idUsuarioAsignado);
        Task<IEnumerable<TicketDTO>> ObtenerTodosPorIdUsuarioAsignadoAsync(int idUsuarioAsignado);
        Task GuardarAsync(Ticket ticket);
        Task ActualizarAsync(Ticket ticket);
        Task EliminarAsync(Guid id);
    }
}
