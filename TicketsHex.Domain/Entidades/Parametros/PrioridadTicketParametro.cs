using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class PrioridadTicketParametro
    {
        public TicketPrioridad IdPrioridad { get; private set; }
        public string Prioridad { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private PrioridadTicketParametro() { }
    }
}
