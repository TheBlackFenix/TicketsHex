namespace TicketsHex.Application.Comun.Excepciones
{
    public sealed class UsuarioNoAutenticadoException : Exception
    {
        public UsuarioNoAutenticadoException(string mensaje) : base(mensaje) { }
    }
}
