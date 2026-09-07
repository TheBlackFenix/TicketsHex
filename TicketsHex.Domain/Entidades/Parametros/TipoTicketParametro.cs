using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class TipoTicketParametro
    {
        public TicketTipo IdTipo { get; private set; }
        public string Tipo { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private TipoTicketParametro() { }
    }
}
