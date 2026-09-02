using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    public sealed class UsuarioRepository : IUsuarioRepository
    {
        private readonly MantenimientoContext _dbContext;

        public UsuarioRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Usuario?> ObtenerPorIdAsync(long idUsuario) =>
            _dbContext.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

        public async Task<IReadOnlyCollection<Usuario>> ObtenerTodosAsync(bool incluirInactivos) =>
            await _dbContext.Usuarios
                .AsNoTracking()
                .Where(u => incluirInactivos || u.Activo)
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

        public Task<bool> ExisteAsync(long idUsuario) =>
            _dbContext.Usuarios.AnyAsync(u => u.IdUsuario == idUsuario && u.Activo);

        public async Task GuardarAsync(Usuario usuario)
        {
            await _dbContext.Usuarios.AddAsync(usuario);
            await _dbContext.SaveChangesAsync();
        }

        public Task ActualizarAsync(Usuario usuario) => _dbContext.SaveChangesAsync();
    }
}
