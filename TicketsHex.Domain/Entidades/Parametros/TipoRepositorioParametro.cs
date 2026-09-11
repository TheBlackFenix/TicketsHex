using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Parametros
{
    public sealed class TipoRepositorioParametro
    {
        public TipoRepositorio IdTipoRepositorio { get; private set; }
        public string Tipo { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; }

        private TipoRepositorioParametro() { }
    }
}
