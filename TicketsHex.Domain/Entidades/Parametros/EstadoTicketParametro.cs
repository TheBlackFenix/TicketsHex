namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class EstadoTicketParametro
    {
        public int IdEstado { get; private set; }
        public string Estado { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private EstadoTicketParametro() { }
    }
}
