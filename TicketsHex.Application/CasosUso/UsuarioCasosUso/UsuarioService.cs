using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Usuario;
using TicketsHex.Application.Puertos.Entrada.Usuario;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.Servicios;

namespace TicketsHex.Application.CasosUso.UsuarioCasosUso
{
    public sealed class UsuarioService : IUsuarioService
    {
        private readonly IUsuarioRepository _repository;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IAutenticacionRepository _autenticacionRepository;
        private readonly IContrasenaHasher _contrasenaHasher;

        public UsuarioService(
            IUsuarioRepository repository,
            IUsuarioActual usuarioActual,
            IAutenticacionRepository autenticacionRepository,
            IContrasenaHasher contrasenaHasher)
        {
            _repository = repository;
            _usuarioActual = usuarioActual;
            _autenticacionRepository = autenticacionRepository;
            _contrasenaHasher = contrasenaHasher;
        }

        public async Task<IReadOnlyCollection<UsuarioDTO>> ObtenerTodosAsync(bool incluirInactivos)
        {
            ValidarPlanner();
            var usuarios = await _repository.ObtenerTodosAsync(incluirInactivos);
            return usuarios.Select(Mapear).ToArray();
        }

        public async Task<UsuarioDTO> ObtenerPorIdAsync(long idUsuario)
        {
            ValidarPlanner();
            var usuario = await ObtenerEntidadAsync(idUsuario);
            return Mapear(usuario);
        }

        public async Task CrearAsync(CrearUsuarioRequest request)
        {
            ValidarPlanner();
            if (await _repository.ObtenerPorIdAsync(request.IdUsuario) is not null)
                throw new ConflictoException($"El usuario {request.IdUsuario} ya existe.");
            if (await _autenticacionRepository.ObtenerUsuarioPorNombreAsync(request.NombreUsuario) is not null)
                throw new ConflictoException($"El nombre de usuario {request.NombreUsuario} ya existe.");

            ValidadorContrasena.Validar(request.Contrasena);
            var usuario = new Usuario(
                request.IdUsuario,
                request.NombreUsuario,
                request.Nombres,
                request.Apellidos,
                request.Rol,
                request.IdArea,
                _contrasenaHasher.CrearHash(request.Contrasena));

            await _repository.GuardarAsync(usuario);
        }

        public async Task ActualizarAsync(long idUsuario, ActualizarUsuarioRequest request)
        {
            ValidarPlanner();
            var usuario = await ObtenerEntidadAsync(idUsuario);
            var usuarioMismoNombre = await _autenticacionRepository
                .ObtenerUsuarioPorNombreAsync(request.NombreUsuario);
            if (usuarioMismoNombre is not null && usuarioMismoNombre.IdUsuario != idUsuario)
                throw new ConflictoException($"El nombre de usuario {request.NombreUsuario} ya existe.");

            usuario.Actualizar(
                request.NombreUsuario,
                request.Nombres,
                request.Apellidos,
                request.Rol,
                request.IdArea);

            if (request.Activo)
                usuario.Activar();
            else
            {
                usuario.Desactivar();
                await _autenticacionRepository.RevocarSesionesAsync(
                    idUsuario,
                    DateTimeOffset.UtcNow);
            }

            await _repository.ActualizarAsync(usuario);
        }

        public async Task DesactivarAsync(long idUsuario)
        {
            ValidarPlanner();
            if (idUsuario == _usuarioActual.IdUsuario)
                throw new InvalidOperationException("El Planner no puede desactivar su propio usuario.");

            var usuario = await ObtenerEntidadAsync(idUsuario);
            usuario.Desactivar();
            await _autenticacionRepository.RevocarSesionesAsync(
                idUsuario,
                DateTimeOffset.UtcNow);
            await _repository.ActualizarAsync(usuario);
        }

        public async Task DesbloquearAsync(long idUsuario)
        {
            ValidarPlanner();
            var usuario = await ObtenerEntidadAsync(idUsuario);
            usuario.Desbloquear();
            await _repository.ActualizarAsync(usuario);
        }

        private void ValidarPlanner()
        {
            if (_usuarioActual.Rol != Rol.Planner)
                throw new UnauthorizedAccessException("Solo el Planner puede administrar usuarios.");
        }

        private async Task<Usuario> ObtenerEntidadAsync(long idUsuario)
        {
            return await _repository.ObtenerPorIdAsync(idUsuario)
                ?? throw new RecursoNoEncontradoException("Usuario no encontrado.");
        }

        private static UsuarioDTO Mapear(Usuario usuario) => new(
            usuario.IdUsuario,
            usuario.NombreUsuario,
            usuario.Nombres,
            usuario.Apellidos,
            usuario.IdRol,
            usuario.IdArea,
            usuario.Activo,
            usuario.Bloqueado,
            usuario.IntentosFallidos,
            usuario.FechaBloqueo,
            usuario.ContrasenaExpiraEn);
    }
}
