using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.Ticket
{
    public class TicketCommand : ITicketCommand
    {
        public Task ActualizarEstadoAsync(Guid ticketId, TicketEstado nuevoEstado, int idUsuario, Roles rol, string? comentario)
        {
            throw new NotImplementedException();
        }

        public Task<Guid> CrearTicketAsync(CrearTicketRequest request)
        {
            throw new NotImplementedException();
        }

        public Task ReasignarTicketAsync(Guid ticketId, int nuevoUsuarioId, int idUsuarioAction, Roles rol, string? comentario)
        {
            throw new NotImplementedException();
        }
    }
}
