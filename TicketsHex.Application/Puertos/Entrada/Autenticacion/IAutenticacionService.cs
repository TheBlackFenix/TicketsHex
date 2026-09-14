using TicketsHex.Application.DTO_s.Autenticacion;

namespace TicketsHex.Application.Puertos.Entrada.Autenticacion
{
    public interface IAutenticacionService
    {
        Task InicializarAsync(InicializarAutenticacionRequest request);
        Task<LoginResponse> IniciarSesionAsync(LoginRequest request);
        Task<LoginResponse> RenovarSesionAsync(string refreshToken);
        Task<UsuarioAutenticadoDTO> ValidarSesionAsync(string jti);
        Task CerrarSesionAsync(string jti);
        Task CerrarSesionConRefreshAsync(string refreshToken);
        Task CambiarContrasenaAsync(long idUsuario, CambiarContrasenaRequest request);
    }
}
