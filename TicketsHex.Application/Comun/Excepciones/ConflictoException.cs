namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class ConflictoException : Exception
    {
        public ConflictoException(string mensaje) : base(mensaje) { }
    }
}
