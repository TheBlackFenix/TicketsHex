namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class AreaTicketParametro
    {
        public int IdArea { get; private set; }
        public string Area { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private AreaTicketParametro() { }
    }
}
