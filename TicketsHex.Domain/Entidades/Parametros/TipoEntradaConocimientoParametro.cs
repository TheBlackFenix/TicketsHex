namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class TipoEntradaConocimientoParametro
    {
        public int IdTipoEntrada { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private TipoEntradaConocimientoParametro() { }
    }
}
