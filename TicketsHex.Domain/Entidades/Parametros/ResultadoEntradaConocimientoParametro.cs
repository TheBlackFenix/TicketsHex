namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class ResultadoEntradaConocimientoParametro
    {
        public int IdResultado { get; private set; }
        public int IdTipoEntrada { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private ResultadoEntradaConocimientoParametro() { }
    }
}
