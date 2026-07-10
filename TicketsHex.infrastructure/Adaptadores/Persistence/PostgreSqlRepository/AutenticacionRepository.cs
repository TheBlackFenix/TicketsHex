using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository
{
    public sealed class AutenticacionRepository : IAutenticacionRepository
    {
        private readonly MantenimientoContext _dbContext;

        public AutenticacionRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Usuario?> ObtenerUsuarioPorIdAsync(long idUsuario) =>
            _dbContext.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);

        public Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario)
        {
            var nombreNormalizado = nombreUsuario.Trim().ToLower();
            return _dbContext.Usuarios.FirstOrDefaultAsync(
                u => u.NombreUsuario.ToLower() == nombreNormalizado);
        }

        public Task<bool> ExisteUsuarioConContrasenaAsync() =>
            _dbContext.Usuarios.AnyAsync(
                u => u.ContrasenaHash != null && u.ContrasenaHash != string.Empty);

        public Task<SesionUsuario?> ObtenerSesionPorJtiAsync(string jti) =>
            _dbContext.SesionesUsuario
                .FirstOrDefaultAsync(s =>
                    s.Jti == jti &&
                    s.FechaRevocacion == null);

        public async Task RegistrarIntentoFallidoAsync(long idUsuario, DateTimeOffset fecha)
        {
            await _dbContext.Usuarios
                .Where(u => u.IdUsuario == idUsuario && !u.Bloqueado)
                .ExecuteUpdateAsync(actualizacion => actualizacion
                    .SetProperty(
                        u => u.IntentosFallidos,
                        u => u.IntentosFallidos + 1)
                    .SetProperty(
                        u => u.Bloqueado,
                        u => u.IntentosFallidos + 1 >= Usuario.MaximoIntentosFallidos)
                    .SetProperty(
                        u => u.FechaBloqueo,
                        u => u.IntentosFallidos + 1 >= Usuario.MaximoIntentosFallidos
                            ? fecha
                            : u.FechaBloqueo));
        }

        public async Task CrearUsuarioAsync(Usuario usuario)
        {
            await _dbContext.Usuarios.AddAsync(usuario);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ReemplazarSesionAsync(
            SesionUsuario nuevaSesion,
            DateTimeOffset fechaRevocacion)
        {
            await using var transaccion =
                await _dbContext.Database.BeginTransactionAsync();
            try
            {
                await _dbContext.SesionesUsuario
                    .Where(s =>
                        s.IdUsuario == nuevaSesion.IdUsuario &&
                        s.FechaRevocacion == null)
                    .ExecuteUpdateAsync(actualizacion =>
                        actualizacion.SetProperty(
                            s => s.FechaRevocacion,
                            fechaRevocacion));

                await _dbContext.SesionesUsuario.AddAsync(nuevaSesion);
                await _dbContext.SaveChangesAsync();
                await transaccion.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();
                throw new ConflictoException(
                    "No fue posible establecer la nueva sesión.");
            }
        }

        public Task RevocarSesionesAsync(long idUsuario, DateTimeOffset fecha) =>
            _dbContext.SesionesUsuario
                .Where(s => s.IdUsuario == idUsuario && s.FechaRevocacion == null)
                .ExecuteUpdateAsync(actualizacion =>
                    actualizacion.SetProperty(s => s.FechaRevocacion, fecha));

        public Task GuardarCambiosAsync() => _dbContext.SaveChangesAsync();
    }
}
