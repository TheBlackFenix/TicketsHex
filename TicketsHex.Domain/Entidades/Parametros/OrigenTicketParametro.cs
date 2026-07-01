namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class OrigenTicketParametro
    {
        public int IdOrigen { get; private set; }
        public string Origen { get; private set; } = string.Empty;
        public bool Activo { get; private set; }

        private OrigenTicketParametro() { }
    }
}
