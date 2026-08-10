namespace TicketsHex.Domain.Entidades.Ticket
{
    public class HistoricoAsignacionTicket
    {
        public Guid IdHistoricoAsignacion { get; set; }
        public Guid IdTicket { get; set; }
        public long IdUsuarioAsignado { get; set; }
        public long IdUsuarioAccion { get; set; }
        public string? Comentario { get; set; }
        public DateTimeOffset FechaAsignacion { get; set; }
    }
}
