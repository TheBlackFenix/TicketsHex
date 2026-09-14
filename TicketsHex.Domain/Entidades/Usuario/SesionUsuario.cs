namespace TicketsHex.Domain.Entidades.Usuario
{
    public sealed class SesionUsuario
    {
        public Guid IdSesion { get; private set; }
        public long IdUsuario { get; private set; }
        public string Jti { get; private set; } = string.Empty;
        public DateTimeOffset FechaCreacion { get; private set; }
        public DateTimeOffset FechaExpiracion { get; private set; }
        public DateTimeOffset? FechaRevocacion { get; private set; }
        public string? RefreshTokenHash { get; private set; }

        private SesionUsuario() { }

        public SesionUsuario(
            long idUsuario,
            string jti,
            DateTimeOffset fechaCreacion,
            DateTimeOffset fechaExpiracion,
            string? refreshTokenHash = null)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("El ID del usuario debe ser positivo.", nameof(idUsuario));
            if (string.IsNullOrWhiteSpace(jti))
                throw new ArgumentException("El identificador JWT es obligatorio.", nameof(jti));
            if (fechaExpiracion <= fechaCreacion)
                throw new ArgumentException("La sesión debe expirar después de su creación.");

            IdSesion = Guid.NewGuid();
            IdUsuario = idUsuario;
            Jti = jti;
            FechaCreacion = fechaCreacion;
            FechaExpiracion = fechaExpiracion;
            RefreshTokenHash = refreshTokenHash;
        }

        public bool EstaVigente(DateTimeOffset fechaActual) =>
            FechaRevocacion is null && FechaExpiracion > fechaActual;

        public void Revocar(DateTimeOffset fecha)
        {
            FechaRevocacion ??= fecha;
        }

        public void Rotar(string nuevoJti, string nuevoRefreshTokenHash)
        {
            if (string.IsNullOrWhiteSpace(nuevoJti) ||
                string.IsNullOrWhiteSpace(nuevoRefreshTokenHash))
                throw new ArgumentException("La sesión renovada requiere credenciales válidas.");
            Jti = nuevoJti;
            RefreshTokenHash = nuevoRefreshTokenHash;
        }
    }
}
