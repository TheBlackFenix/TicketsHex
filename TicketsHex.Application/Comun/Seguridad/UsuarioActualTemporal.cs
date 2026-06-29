using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Comun.Seguridad
{
    public sealed class UsuarioActualTemporal : IUsuarioActual
    {
        private long? _idUsuario;
        private Rol? _rol;

        public long IdUsuario => _idUsuario
            ?? throw new UsuarioNoAutenticadoException("No fue posible determinar el usuario actual.");

        public Rol Rol => _rol
            ?? throw new UsuarioNoAutenticadoException("No fue posible determinar el rol actual.");

        public void Establecer(long idUsuario, Rol rol)
        {
            if (idUsuario <= 0)
                throw new UsuarioNoAutenticadoException("El ID del usuario actual no es válido.");
            if (!Enum.IsDefined(rol))
                throw new UsuarioNoAutenticadoException("El rol del usuario actual no es válido.");

            _idUsuario = idUsuario;
            _rol = rol;
        }
    }
}
