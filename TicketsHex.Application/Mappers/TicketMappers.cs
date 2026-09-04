using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Mappers
{
    public static class TicketMappingExtensions
    {
        // Mapea una sola entidad a DTO
        public static TicketDTO ToDto(this Ticket ticket, long idUsuarioActual, Rol rolActual)
        {
            if (ticket == null) throw new ArgumentNullException(nameof(ticket));

            return new TicketDTO(
                IdTicket: ticket.IdTicket,
                IdCaso: ticket.CodigoCaso.Valor, // Extrayendo el tipo primitivo del Value Object
                Titulo: ticket.Titulo.Value,
                Descripcion: ticket.Descripcion.Value,
                TicketEstado: ticket.IdEstado,
                Origen: ticket.IdOrigen,
                Tipo: ticket.IdTipo,
                Prioridad: ticket.IdPrioridad,
                Impacto: ticket.IdImpacto,
                IdUsuarioAsignado: ticket.IdUsuarioAsignado,
                IdDesarrolladorResponsable: ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo),
                IdQaResponsable: ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.QA),
                CausaRaiz: ticket.CausaRaiz,
                SolucionPropuesta: ticket.SolucionPropuesta,
                EsDesarrollo: ticket.EsDesarrollo,
                NombreHu: ticket.NombreHu,
                UrlHu: ticket.UrlHu,
                CarpetaMedios: ticket.CarpetaMedios,
                FechaUltimaActualizacion: ticket.FechaUltimaActualizacion,
                FechaCreacion: ticket.FechaAsignacion,
                Activo: ticket.Activo,
                FechaEliminacion: ticket.FechaEliminacion,
                Comentarios: ticket.HistoricoEstados
                    .OrderByDescending(h => h.FechaCambio)
                    .Select(h => h.ToHistoryDto())
                    .ToArray(),
                Capacidades: new CapacidadesTicketDTO(
                    ticket.ObtenerAccionesPermitidas(idUsuarioActual, rolActual),
                    ticket.ObtenerTransicionesDisponibles(idUsuarioActual, rolActual)
                        .Select(item => new TransicionDisponibleDTO(
                            item.EstadoDestino,
                            item.Tipo,
                            item.RequiereComentario))
                        .ToArray())
            );
        }

        // Mapea una colección enumerable (ideal para las consultas de listas)
        public static IEnumerable<TicketDTO> ToDtoList(
            this IEnumerable<Ticket> tickets,
            long idUsuarioActual,
            Rol rolActual)
        {
            if (tickets == null) throw new ArgumentNullException(nameof(tickets));

            return tickets.Select(ticket => ticket.ToDto(idUsuarioActual, rolActual));
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
