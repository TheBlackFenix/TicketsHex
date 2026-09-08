using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class CuentaBloqueadaException : Exception
    {
        public CuentaBloqueadaException(
            string mensaje,
            string codigo = CodigosError.CuentaBloqueada) : base(mensaje)
        {
            this.ConCodigo(codigo);
        }
    }
}
