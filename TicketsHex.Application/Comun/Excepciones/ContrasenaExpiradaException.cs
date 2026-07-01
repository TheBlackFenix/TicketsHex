namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class ContrasenaExpiradaException : Exception
    {
        public ContrasenaExpiradaException(string mensaje) : base(mensaje) { }
    }
}
