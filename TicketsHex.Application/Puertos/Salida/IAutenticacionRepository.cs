using TicketsHex.Domain.Entidades.Usuario;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IAutenticacionRepository
    {
        Task<Usuario?> ObtenerUsuarioPorIdAsync(long idUsuario);
        Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario);
        Task<bool> ExisteUsuarioConContrasenaAsync();
        Task<SesionUsuario?> ObtenerSesionPorJtiAsync(string jti);
        Task<SesionUsuario?> ObtenerSesionPorIdAsync(Guid idSesion);
        Task<bool> RotarSesionAsync(
            Guid idSesion,
            string hashActual,
            string hashNuevo,
            string nuevoJti,
            DateTimeOffset fechaActual);
        Task<bool> RevocarSesionPorRefreshAsync(
            Guid idSesion,
            string refreshTokenHash,
            DateTimeOffset fechaActual);
        Task RegistrarIntentoFallidoAsync(long idUsuario, DateTimeOffset fecha);
        Task CrearUsuarioAsync(Usuario usuario);
        Task ReemplazarSesionAsync(
            SesionUsuario nuevaSesion,
            DateTimeOffset fechaRevocacion);
        Task RevocarSesionesAsync(long idUsuario, DateTimeOffset fecha);
        Task GuardarCambiosAsync();
    }
}
