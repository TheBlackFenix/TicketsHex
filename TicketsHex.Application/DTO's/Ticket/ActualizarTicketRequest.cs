using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Ticket
{
    public sealed record ActualizarTicketRequest(
        string? Titulo,
        string? Descripcion,
        TicketEstado? NuevoEstado,
        long? IdUsuarioAsignado,
        string? CausaRaiz,
        string? SolucionPropuesta,
        string? Comentario,
        bool? EsDesarrollo = null,
        string? NombreHu = null,
        string? UrlHu = null,
        string? CarpetaMedios = null);
}
