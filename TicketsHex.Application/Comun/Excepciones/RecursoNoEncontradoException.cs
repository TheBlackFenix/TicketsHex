using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class RecursoNoEncontradoException : Exception
    {
        public RecursoNoEncontradoException(
            string mensaje,
            string codigo = CodigosError.RecursoNoEncontrado) : base(mensaje)
        {
            this.ConCodigo(codigo);
        }
    }
}
