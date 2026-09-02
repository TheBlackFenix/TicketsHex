namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class AmbienteTicketParametro
    {
        public int IdAmbiente { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private AmbienteTicketParametro() { }
    }
}
