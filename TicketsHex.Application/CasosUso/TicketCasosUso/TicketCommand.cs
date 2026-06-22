using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.TicketCasosUso
{
    public class TicketCommand : ITicketCommand
    {
        private readonly ITicketRepository _ticketRepository;
        public TicketCommand(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }
        public async Task ActualizarEstadoAsync(Guid ticketId, TicketEstado nuevoEstado, int idUsuario, Rol rol, string? comentario)
        {
            var ticket = await _ticketRepository.ObtenerPorIdAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket no encontrado");

            // El dominio sigue ejecutando la lógica SOLID y la máquina de estados
            ticket.ActualizarEstado(nuevoEstado, idUsuario, rol, comentario);

            await _ticketRepository.ActualizarAsync(ticket);
        }

        public async Task<Guid> CrearTicketAsync(CrearTicketRequest request)
        {

            var ticket = new Ticket(request.CodigoCaso, request.Titulo, request.Descripcion, request.IdUsuarioAsignado);
            await _ticketRepository.GuardarAsync(ticket);
            return ticket.Id;
        }

        public async Task ReasignarTicketAsync(Guid ticketId, int nuevoUsuarioId, int idUsuarioAction, Rol rol, string? comentario)
        {
            var ticket = await _ticketRepository.ObtenerPorIdAsync(ticketId);
            if (ticket == null) throw new KeyNotFoundException("Ticket no encontrado");

            ticket.ReasignarTicket(nuevoUsuarioId, idUsuarioAction, rol, comentario);

            await _ticketRepository.ActualizarAsync(ticket);
        }
    }
}
