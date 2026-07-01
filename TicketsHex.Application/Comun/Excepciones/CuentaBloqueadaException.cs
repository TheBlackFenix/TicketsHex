namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class CuentaBloqueadaException : Exception
    {
        public CuentaBloqueadaException(string mensaje) : base(mensaje) { }
    }
}
