using TicketsHex.Application.DTO_s.Ticket;

namespace TicketsHex.Application.Puertos.Entrada.Ticket
{
    internal interface ITicketQuery
    {
        Task<TicketDTO> ObtenerTicketPorIdAsync(Guid id);
        Task<IEnumerable<TicketDTO>> ObtenerListaTicketsAsync();
        Task<TicketDTO> ObtenerTicketPorCodigoYUsuerioAsync(Guid id, int idUsuarioAsignado);
        Task<IEnumerable<TicketDTO>> ObtenerTicketsPorUsuarioAsignadoAsync(int idUsuarioAsignado);
    }
}
