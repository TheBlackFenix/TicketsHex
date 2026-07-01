namespace TicketsHex.Domain.Entidades.Usuario
{
    public sealed class SesionUsuario
    {
        public Guid IdSesion { get; private set; }
        public long IdUsuario { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public DateTimeOffset FechaCreacion { get; private set; }
        public DateTimeOffset FechaExpiracion { get; private set; }
        public DateTimeOffset? FechaRevocacion { get; private set; }

        private SesionUsuario() { }

        public SesionUsuario(
            long idUsuario,
            string tokenHash,
            DateTimeOffset fechaCreacion,
            DateTimeOffset fechaExpiracion)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("El ID del usuario debe ser positivo.", nameof(idUsuario));
            if (string.IsNullOrWhiteSpace(tokenHash))
                throw new ArgumentException("El hash del token es obligatorio.", nameof(tokenHash));
            if (fechaExpiracion <= fechaCreacion)
                throw new ArgumentException("La sesión debe expirar después de su creación.");

            IdSesion = Guid.NewGuid();
            IdUsuario = idUsuario;
            TokenHash = tokenHash;
            FechaCreacion = fechaCreacion;
            FechaExpiracion = fechaExpiracion;
        }

        public bool EstaVigente(DateTimeOffset fechaActual) =>
            FechaRevocacion is null && FechaExpiracion > fechaActual;

        public void Revocar(DateTimeOffset fecha)
        {
            FechaRevocacion ??= fecha;
        }
    }
}
