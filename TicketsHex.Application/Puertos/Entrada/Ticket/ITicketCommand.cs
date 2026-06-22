using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Puertos.Entrada.Ticket
{
    public interface ITicketCommand
    {
        Task<Guid> CrearTicketAsync(CrearTicketRequest request);
        Task ActualizarEstadoAsync(Guid ticketId, TicketEstado nuevoEstado, int idUsuario, Rol rol, string? comentario);
        Task ReasignarTicketAsync(Guid ticketId, int nuevoUsuarioId, int idUsuarioAction, Rol rol, string? comentario);
    }
}
