using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Mappers;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.Application.CasosUso.TicketCasosUso
{
    public class TicketQuery : ITicketQuery
    {
        private readonly ITicketRepository _ticketRepository;

        public TicketQuery(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<IEnumerable<TicketDTO>> ObtenerListaTicketsAsync()
        {
            var tickets = await _ticketRepository.ObtenerTodosAsync();
            var ticketDtos = tickets.Select(t => t.ToDto());
            return ticketDtos;
        }

        public async Task<TicketDTO?> ObtenerTicketPorCodigoYUsuerioAsync(Guid id, int idUsuarioAsignado)
        {
            var ticket = await _ticketRepository.ObtenerTicketPorCodigoYUsuerioAsync(id, idUsuarioAsignado);
            return ticket.ToDto();
        }

        public async Task<TicketDTO> ObtenerTicketPorIdAsync(Guid id)
        {
            var ticket = await _ticketRepository.ObtenerPorIdAsync(id);
            return ticket.ToDto();
        }

        public async Task<IEnumerable<TicketDTO>> ObtenerTicketsPorUsuarioAsignadoAsync(int idUsuarioAsignado)
        {
            var tickets = _ticketRepository.ObtenerTodosPorIdUsuarioAsignadoAsync(idUsuarioAsignado);
            var ticketDtos = tickets.Result.Select(t => t.ToDto());
            return ticketDtos;
        }
    }
}
