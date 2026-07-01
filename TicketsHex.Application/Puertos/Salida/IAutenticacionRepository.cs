using TicketsHex.Domain.Entidades.Usuario;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IAutenticacionRepository
    {
        Task<Usuario?> ObtenerUsuarioPorIdAsync(long idUsuario);
        Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario);
        Task<bool> ExisteUsuarioConContrasenaAsync();
        Task<SesionUsuario?> ObtenerSesionNoRevocadaAsync(long idUsuario);
        Task<SesionUsuario?> ObtenerSesionPorJtiAsync(string jti);
        Task RegistrarIntentoFallidoAsync(long idUsuario, DateTimeOffset fecha);
        Task CrearUsuarioAsync(Usuario usuario);
        Task CrearSesionAsync(SesionUsuario sesion);
        Task RevocarSesionesAsync(long idUsuario, DateTimeOffset fecha);
        Task GuardarCambiosAsync();
    }
}
