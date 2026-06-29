using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Usuario
{
    public class Usuario
    {
        public long IdUsuario { get; private set; }
        public string NombreUsuario { get; private set; } = string.Empty;
        public string Nombres { get; private set; } = string.Empty;
        public string? Apellidos { get; private set; }
        public Rol IdRol { get; private set; }
        public int? IdArea { get; private set; }
        public bool Activo { get; private set; } = true;

        private Usuario() { }

        public Usuario(
            long idUsuario,
            string nombreUsuario,
            string nombres,
            string? apellidos,
            Rol rol,
            int? idArea)
        {
            if (idUsuario <= 0)
                throw new ArgumentException("El ID del usuario debe ser positivo.", nameof(idUsuario));

            IdUsuario = idUsuario;
            Actualizar(nombreUsuario, nombres, apellidos, rol, idArea);
        }

        public void Actualizar(
            string nombreUsuario,
            string nombres,
            string? apellidos,
            Rol rol,
            int? idArea)
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

            NombreUsuario = nombreUsuario;
            Nombres = nombres;
            Apellidos = apellidos;
            IdRol = rol;
            IdArea = idArea;
        }

        public void Desactivar() => Activo = false;
        public void Activar() => Activo = true;
    }
}
