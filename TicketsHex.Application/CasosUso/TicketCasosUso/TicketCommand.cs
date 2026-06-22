using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.TicketCasosUso
{
    public class TicketCommand : ITicketCommand
    {
        private readonly ITicketRepository ITicketRepository;
        public TicketCommand(ITicketRepository ticketRepository)
        {
            ITicketRepository = ticketRepository;
        }
        public Task ActualizarEstadoAsync(Guid ticketId, TicketEstado nuevoEstado, int idUsuario, Rol rol, string? comentario)
        {
            throw new NotImplementedException();
        }

        public async Task<Guid> CrearTicketAsync(CrearTicketRequest request)
        {

            var ticket = new Ticket(request.CodigoCaso, request.Titulo, request.Descripcion, request.IdUsuarioAsignado);
            await ITicketRepository.GuardarAsync(ticket);
            return ticket.Id;
        }

        public Task ReasignarTicketAsync(Guid ticketId, int nuevoUsuarioId, int idUsuarioAction, Rol rol, string? comentario)
        {
            throw new NotImplementedException();
        }
    }
}
