using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;

namespace TicketsHex.Application.Mappers
{
    public static class TicketMappingExtensions
    {
        // Mapea una sola entidad a DTO
        public static TicketDTO ToDto(this Ticket ticket)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            return new TicketDTO(
                IdTicket: ticket.IdTicket,
                IdCaso: ticket.CodigoCaso.Valor, // Extrayendo el tipo primitivo del Value Object
                Titulo: ticket.Titulo.Value,
                Descripcion: ticket.Descripcion.Value,
                TicketEstado: ticket.IdEstado,
                Origen: ticket.IdOrigen,
                IdUsuarioAsignado: ticket.IdUsuarioAsignado,
                CausaRaiz: ticket.CausaRaiz,
                SolucionPropuesta: ticket.SolucionPropuesta,
                FechaUltimaActualizacion: ticket.FechaUltimaActualizacion,
                FechaCreacion: ticket.FechaAsignacion,
                Activo: ticket.Activo,
                FechaEliminacion: ticket.FechaEliminacion,
                Comentarios: ticket.HistoricoEstados
                    .OrderByDescending(h => h.FechaCambio)
                    .Select(h => h.ToHistoryDto())
            );
        }

        // Mapea una colección enumerable (ideal para las consultas de listas)
        public static IEnumerable<TicketDTO> ToDtoList(this IEnumerable<Ticket> tickets)
        {
            if (tickets == null) throw new ArgumentNullException(nameof(tickets));

            return tickets.Select(ticket => ticket.ToDto());
        }

        public static TicketHistoryDTO ToHistoryDto(this HistoricoEstadosTicket historico)
        {
            if (historico == null) throw new ArgumentNullException(nameof(historico));
            return new TicketHistoryDTO
            {
                EstadoOrigen = historico.IdEstadoOrigen ?? default,
                EstadoDestino = historico.IdEstadoDestino,
                IdUsuarioAccion = historico.IdUsuarioAccion,
                Comentario = historico.Comentario,
                FechaAccion = historico.FechaCambio
            };
        }

    }
}
