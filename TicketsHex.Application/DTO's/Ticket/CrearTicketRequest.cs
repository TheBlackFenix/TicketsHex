using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public record CrearTicketRequest(
        string CodigoCaso,
        TicketOrigen OrigenTicket,
        string Titulo,
        string Descripcion,
        long IdUsuarioAsignado,
        TicketTipo Tipo,
        TicketPrioridad Prioridad,
        TicketImpacto Impacto,
        bool EsDesarrollo = false,
        long? IdQaResponsable = null,
        string? NombreHu = null,
        string? UrlHu = null,
        string? CarpetaMedios = null
    );
}
