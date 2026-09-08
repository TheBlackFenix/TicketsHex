using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class ContrasenaExpiradaException : Exception
    {
        public ContrasenaExpiradaException(
            string mensaje,
            string codigo = CodigosError.CambioContrasenaRequerido) : base(mensaje)
        {
            this.ConCodigo(codigo);
        }
    }
}
