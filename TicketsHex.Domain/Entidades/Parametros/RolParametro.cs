namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class RolParametro
    {
        public int IdRol { get; private set; }
        public string NombreRol { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }

        private RolParametro() { }
    }
}
