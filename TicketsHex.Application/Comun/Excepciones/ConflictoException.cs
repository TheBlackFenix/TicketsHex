using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class ConflictoException : Exception
    {
        public ConflictoException(
            string mensaje,
            string codigo = CodigosError.Conflicto) : base(mensaje)
        {
            this.ConCodigo(codigo);
        }
    }
}
