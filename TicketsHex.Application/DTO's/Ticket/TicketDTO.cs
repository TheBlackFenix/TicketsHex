using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public record TicketDTO(
        Guid IdTicket,
        string IdCaso,
        string Titulo,
        string Descripcion,
        TicketEstado TicketEstado,
        TicketOrigen Origen,
        long? IdUsuarioAsignado,
        string? CausaRaiz,
        string? SolucionPropuesta,
        bool EsDesarrollo,
        string? NombreHu,
        string? UrlHu,
        string? CarpetaMedios,
        DateTimeOffset FechaCreacion,
        DateTimeOffset? FechaUltimaActualizacion,
        bool Activo,
        DateTimeOffset? FechaEliminacion,
        IEnumerable<TicketHistoryDTO> Comentarios
    );
}
