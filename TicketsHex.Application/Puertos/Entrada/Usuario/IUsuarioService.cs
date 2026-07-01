using TicketsHex.Application.DTO_s.Usuario;

namespace TicketsHex.Application.Puertos.Entrada.Usuario
{
    public interface IUsuarioService
    {
        Task<IReadOnlyCollection<UsuarioDTO>> ObtenerTodosAsync(bool incluirInactivos);
        Task<UsuarioDTO> ObtenerPorIdAsync(long idUsuario);
        Task CrearAsync(CrearUsuarioRequest request);
        Task ActualizarAsync(long idUsuario, ActualizarUsuarioRequest request);
        Task DesactivarAsync(long idUsuario);
        Task DesbloquearAsync(long idUsuario);
    }
}
