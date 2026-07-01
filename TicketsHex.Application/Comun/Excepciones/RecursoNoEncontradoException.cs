namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class RecursoNoEncontradoException : Exception
    {
        public RecursoNoEncontradoException(string mensaje) : base(mensaje) { }
    }
}
