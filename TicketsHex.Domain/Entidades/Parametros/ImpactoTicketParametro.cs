using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class ImpactoTicketParametro
    {
        public TicketImpacto IdImpacto { get; private set; }
        public string Impacto { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private ImpactoTicketParametro() { }
    }
}
