using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class UsuarioNoAutenticadoException : Exception
    {
        public UsuarioNoAutenticadoException(
            string mensaje,
            string codigo = CodigosError.CredencialesInvalidas) : base(mensaje)
        {
            this.ConCodigo(codigo);
        }
    }
}
