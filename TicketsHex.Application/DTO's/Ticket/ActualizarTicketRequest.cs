using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record ActualizarTicketRequest(
        string? Titulo,
        string? Descripcion,
        TicketEstado? NuevoEstado,
        string? CausaRaiz,
        string? SolucionPropuesta,
        string? Comentario,
        TicketTipo? Tipo = null,
        TicketPrioridad? Prioridad = null,
        TicketImpacto? Impacto = null,
        bool? EsDesarrollo = null,
        string? NombreHu = null,
        string? UrlHu = null,
        string? CarpetaMedios = null);
}
