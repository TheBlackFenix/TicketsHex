using TicketsHex.Domain.Entidades.Usuario;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IUsuarioRepository
    {
        Task<Usuario?> ObtenerPorIdAsync(long idUsuario);
        Task<IReadOnlyCollection<Usuario>> ObtenerTodosAsync(bool incluirInactivos);
        Task<bool> ExisteAsync(long idUsuario);
        Task GuardarAsync(Usuario usuario);
        Task ActualizarAsync(Usuario usuario);
    }
}
