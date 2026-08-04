using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Usuario;
using TicketsHex.Application.Puertos.Entrada.Usuario;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.Servicios;
using Microsoft.Extensions.Configuration;

namespace TicketsHex.Application.CasosUso.UsuarioCasosUso
{
    public sealed class UsuarioService : IUsuarioService
    {
        private const string ContrasenaPorDefectoKey = "Usuarios:ContrasenaPorDefecto";

        private readonly IUsuarioRepository _repository;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IAutenticacionRepository _autenticacionRepository;
        private readonly IContrasenaHasher _contrasenaHasher;
        private readonly IConfiguration _configuration;

        public UsuarioService(
            IUsuarioRepository repository,
            IUsuarioActual usuarioActual,
            IAutenticacionRepository autenticacionRepository,
            IContrasenaHasher contrasenaHasher,
            IConfiguration configuration)
        {
            _repository = repository;
            _usuarioActual = usuarioActual;
            _autenticacionRepository = autenticacionRepository;
            _contrasenaHasher = contrasenaHasher;
            _configuration = configuration;
        }

        public async Task<IReadOnlyCollection<UsuarioDTO>> ObtenerTodosAsync(bool incluirInactivos)
        {
            var usuarios = await _repository.ObtenerTodosAsync(incluirInactivos);
            return usuarios.Select(Mapear).ToArray();
        }

        public async Task<UsuarioDTO> ObtenerPorIdAsync(long idUsuario)
        {
            var usuario = await ObtenerEntidadAsync(idUsuario);
            return Mapear(usuario);
        }

        public async Task CrearAsync(CrearUsuarioRequest request)
        {
            ValidarPlannerOLiderTecnico();
            if (await _repository.ObtenerPorIdAsync(request.IdUsuario) is not null)
                throw new ConflictoException($"El usuario {request.IdUsuario} ya existe.");
            if (await _autenticacionRepository.ObtenerUsuarioPorNombreAsync(request.NombreUsuario) is not null)
                throw new ConflictoException($"El nombre de usuario {request.NombreUsuario} ya existe.");

            var contrasenaPorDefecto = ObtenerContrasenaPorDefecto();
            ValidadorContrasena.Validar(contrasenaPorDefecto);
            var usuario = new Usuario(
                request.IdUsuario,
                request.NombreUsuario,
                request.Nombres,
                request.Apellidos,
                request.Rol,
                request.IdArea,
                _contrasenaHasher.CrearHash(contrasenaPorDefecto),
                request.ImagenPerfilBase64,
                debeCambiarContrasena: true);

            await _repository.GuardarAsync(usuario);
        }

        public async Task ActualizarAsync(long idUsuario, ActualizarUsuarioRequest request)
        {
            ValidarPlannerOLiderTecnico();
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
            if (request.ImagenPerfilBase64 is not null)
                usuario.ActualizarImagenPerfilBase64(request.ImagenPerfilBase64);

            if (request.Activo)
                usuario.Activar();
            else
                usuario.Desactivar();

            await _autenticacionRepository.RevocarSesionesAsync(
                idUsuario,
                DateTimeOffset.UtcNow);
            await _repository.ActualizarAsync(usuario);
        }

        public async Task<UsuarioDTO> ActualizarPerfilPropioAsync(ActualizarPerfilPropioRequest request)
        {
            var actualizaImagen = request.ImagenPerfilBase64 is not null;
            var actualizaContrasena = request.ContrasenaActual is not null ||
                request.NuevaContrasena is not null;

            if (!actualizaImagen && !actualizaContrasena)
                throw new ArgumentException("Debe indicar la imagen de perfil o los datos para cambiar la contraseÃ±a.");

            if (actualizaContrasena &&
                (string.IsNullOrWhiteSpace(request.ContrasenaActual) ||
                 string.IsNullOrWhiteSpace(request.NuevaContrasena)))
            {
                throw new ArgumentException(
                    "La contraseÃ±a actual y la nueva contraseÃ±a son obligatorias para realizar el cambio.");
            }

            var usuario = await ObtenerEntidadAsync(_usuarioActual.IdUsuario);

            if (actualizaImagen)
                usuario.ActualizarImagenPerfilBase64(request.ImagenPerfilBase64);

            if (actualizaContrasena)
            {
                if (usuario.Bloqueado || string.IsNullOrWhiteSpace(usuario.ContrasenaHash))
                    throw new UsuarioNoAutenticadoException("No fue posible validar la contraseÃ±a actual.");

                var resultado = _contrasenaHasher.Verificar(
                    usuario.ContrasenaHash,
                    request.ContrasenaActual!);
                if (resultado == ResultadoVerificacionContrasena.Fallida)
                {
                    await _autenticacionRepository.RegistrarIntentoFallidoAsync(
                        usuario.IdUsuario,
                        DateTimeOffset.UtcNow);
                    throw new UsuarioNoAutenticadoException("La contraseÃ±a actual no es correcta.");
                }

                ValidadorContrasena.Validar(request.NuevaContrasena!);
                if (_contrasenaHasher.Verificar(
                        usuario.ContrasenaHash,
                        request.NuevaContrasena!) != ResultadoVerificacionContrasena.Fallida)
                {
                    throw new ArgumentException("La nueva contraseÃ±a debe ser diferente a la actual.");
                }

                var ahora = DateTimeOffset.UtcNow;
                usuario.CambiarContrasena(
                    _contrasenaHasher.CrearHash(request.NuevaContrasena!),
                    ahora);
                await _autenticacionRepository.RevocarSesionesAsync(usuario.IdUsuario, ahora);
            }

            await _repository.ActualizarAsync(usuario);
            return Mapear(usuario);
        }

        public async Task DesactivarAsync(long idUsuario)
        {
            ValidarPlannerOLiderTecnico();
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
            ValidarPlannerOLiderTecnico();
            var usuario = await ObtenerEntidadAsync(idUsuario);
            usuario.Desbloquear();
            await _repository.ActualizarAsync(usuario);
        }

        private void ValidarPlannerOLiderTecnico()
        {
            if (_usuarioActual.Rol is not Rol.Planner and not Rol.LiderTecnico)
                throw new UnauthorizedAccessException("Solo Planner o Lider Tecnico pueden administrar usuarios.");
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
            usuario.ImagenPerfilBase64,
            usuario.Activo,
            usuario.Bloqueado,
            usuario.IntentosFallidos,
            usuario.FechaBloqueo,
            usuario.ContrasenaExpiraEn);

        private string ObtenerContrasenaPorDefecto()
        {
            var contrasenaPorDefecto = _configuration[ContrasenaPorDefectoKey];
            if (string.IsNullOrWhiteSpace(contrasenaPorDefecto))
                throw new InvalidOperationException(
                    $"No existe la configuraciÃ³n obligatoria {ContrasenaPorDefectoKey}.");

            return contrasenaPorDefecto;
        }
    }
}
