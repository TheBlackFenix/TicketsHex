using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Usuario
{
    public class Usuario
    {
        public const int MaximoIntentosFallidos = 5;
        public const int DiasVigenciaContrasena = 30;

        public long IdUsuario { get; private set; }
        public string NombreUsuario { get; private set; } = string.Empty;
        public string Nombres { get; private set; } = string.Empty;
        public string? Apellidos { get; private set; }
        public Rol IdRol { get; private set; }
        public Area? IdArea { get; private set; }
        public string? ImagenPerfilBase64 { get; private set; }
        public bool Activo { get; private set; } = true;
        public string? ContrasenaHash { get; private set; }
        public int IntentosFallidos { get; private set; }
        public bool Bloqueado { get; private set; }
        public DateTimeOffset? FechaBloqueo { get; private set; }
        public DateTimeOffset? FechaCambioContrasena { get; private set; }

        private Usuario() { }

        public Usuario(
            long idUsuario,
            string nombreUsuario,
            string nombres,
            string? apellidos,
            Rol rol,
            Area? idArea,
            string contrasenaHash,
            string? imagenPerfilBase64 = null)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("El ID del usuario debe ser positivo.", nameof(idUsuario));

            IdUsuario = idUsuario;
            Actualizar(nombreUsuario, nombres, apellidos, rol, idArea);
            ActualizarImagenPerfilBase64(imagenPerfilBase64);
            CambiarContrasena(contrasenaHash, DateTimeOffset.UtcNow);
        }

        public void Actualizar(
            string nombreUsuario,
            string nombres,
            string? apellidos,
            Rol rol,
            Area? idArea)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                throw new ArgumentException("El nombre de usuario es obligatorio.", nameof(nombreUsuario));
            if (nombreUsuario.Length > 50)
                throw new ArgumentException("El nombre de usuario no puede superar 50 caracteres.", nameof(nombreUsuario));
            if (string.IsNullOrWhiteSpace(nombres))
                throw new ArgumentException("Los nombres son obligatorios.", nameof(nombres));
            if (nombres.Length > 100)
                throw new ArgumentException("Los nombres no pueden superar 100 caracteres.", nameof(nombres));
            if (apellidos?.Length > 100)
                throw new ArgumentException("Los apellidos no pueden superar 100 caracteres.", nameof(apellidos));
            if (idArea is <= 0)
                throw new ArgumentException("El ID del área debe ser positivo.", nameof(idArea));

            NombreUsuario = nombreUsuario.Trim();
            Nombres = nombres.Trim();
            Apellidos = apellidos?.Trim();
            IdRol = rol;
            IdArea = idArea;
        }

        public void ActualizarImagenPerfilBase64(string? imagenPerfilBase64)
        {
            if (string.IsNullOrWhiteSpace(imagenPerfilBase64))
            {
                ImagenPerfilBase64 = null;
                return;
            }

            var imagenNormalizada = imagenPerfilBase64.Trim();
            var base64 = ObtenerContenidoBase64(imagenNormalizada);
            Span<byte> buffer = new byte[base64.Length];
            if (!Convert.TryFromBase64String(base64, buffer, out _))
                throw new ArgumentException("La imagen de perfil debe estar en formato Base64.", nameof(imagenPerfilBase64));

            ImagenPerfilBase64 = imagenNormalizada;
        }

        private static string ObtenerContenidoBase64(string valor)
        {
            var separadorDataUri = valor.IndexOf(',');
            if (valor.StartsWith("data:", StringComparison.OrdinalIgnoreCase) &&
                separadorDataUri >= 0)
                return valor[(separadorDataUri + 1)..];

            return valor;
        }

        public void CambiarContrasena(string contrasenaHash, DateTimeOffset fechaCambio)
        {
            if (string.IsNullOrWhiteSpace(contrasenaHash))
                throw new ArgumentException("El hash de la contraseña es obligatorio.", nameof(contrasenaHash));

            ContrasenaHash = contrasenaHash;
            FechaCambioContrasena = fechaCambio;
            ReiniciarIntentosFallidos();
        }

        public void ActualizarHashContrasena(string contrasenaHash)
        {
            if (string.IsNullOrWhiteSpace(contrasenaHash))
                throw new ArgumentException("El hash de la contraseña es obligatorio.", nameof(contrasenaHash));

            ContrasenaHash = contrasenaHash;
        }

        public void RegistrarIntentoFallido(DateTimeOffset fecha)
        {
            if (Bloqueado)
                return;

            IntentosFallidos++;
            if (IntentosFallidos >= MaximoIntentosFallidos)
            {
                Bloqueado = true;
                FechaBloqueo = fecha;
            }
        }

        public void ReiniciarIntentosFallidos()
        {
            IntentosFallidos = 0;
            Bloqueado = false;
            FechaBloqueo = null;
        }

        public void Desbloquear() => ReiniciarIntentosFallidos();

        public bool ContrasenaEstaExpirada(DateTimeOffset fechaActual) =>
            !FechaCambioContrasena.HasValue ||
            FechaCambioContrasena.Value.AddDays(DiasVigenciaContrasena) <= fechaActual;

        public DateTimeOffset? ContrasenaExpiraEn =>
            FechaCambioContrasena?.AddDays(DiasVigenciaContrasena);

        public void Desactivar() => Activo = false;
        public void Activar() => Activo = true;
    }
}
