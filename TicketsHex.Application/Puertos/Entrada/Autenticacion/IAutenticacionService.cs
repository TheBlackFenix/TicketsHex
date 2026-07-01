using TicketsHex.Application.DTO_s.Autenticacion;

namespace TicketsHex.Application.Puertos.Entrada.Autenticacion
{
    public interface IAutenticacionService
    {
        Task InicializarAsync(InicializarAutenticacionRequest request);
        Task<LoginResponse> IniciarSesionAsync(LoginRequest request);
        Task<UsuarioAutenticadoDTO> ValidarSesionAsync(string token);
        Task CerrarSesionAsync(string token);
        Task CambiarContrasenaAsync(CambiarContrasenaRequest request);
    }
}
