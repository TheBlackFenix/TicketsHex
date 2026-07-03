using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.Servicios;

namespace TicketsHex.Application.CasosUso.AutenticacionCasosUso
{
    public sealed class AutenticacionService : IAutenticacionService
    {
        private readonly IAutenticacionRepository _repository;
        private readonly IContrasenaHasher _contrasenaHasher;
        private readonly IGeneradorJwtSesion _jwtGenerator;

        public AutenticacionService(
            IAutenticacionRepository repository,
            IContrasenaHasher contrasenaHasher,
            IGeneradorJwtSesion jwtGenerator)
        {
            _repository = repository;
            _contrasenaHasher = contrasenaHasher;
            _jwtGenerator = jwtGenerator;
        }

        public async Task InicializarAsync(InicializarAutenticacionRequest request)
        {
            if (await _repository.ExisteUsuarioConContrasenaAsync())
                throw new ConflictoException("La autenticación ya fue inicializada.");

            ValidadorContrasena.Validar(request.Contrasena);
            var hash = _contrasenaHasher.CrearHash(request.Contrasena);
            var usuario = await _repository.ObtenerUsuarioPorIdAsync(request.IdUsuario);

            if (usuario is not null)
            {
                if (usuario.IdRol != Rol.Planner)
                    throw new InvalidOperationException("El usuario inicial debe tener rol Planner.");
                if (!string.Equals(
                        usuario.NombreUsuario,
                        request.NombreUsuario,
                        StringComparison.OrdinalIgnoreCase))
                    throw new ConflictoException("El ID y el nombre de usuario no corresponden al mismo usuario.");

                usuario.CambiarContrasena(hash, DateTimeOffset.UtcNow);
                usuario.Activar();
                await _repository.GuardarCambiosAsync();
                return;
            }

            var usuarioPorNombre = await _repository.ObtenerUsuarioPorNombreAsync(request.NombreUsuario);
            if (usuarioPorNombre is not null)
                throw new ConflictoException("El nombre de usuario ya está registrado con otro ID.");

            usuario = new Usuario(
                request.IdUsuario,
                request.NombreUsuario,
                request.Nombres,
                request.Apellidos,
                Rol.Planner,
                request.IdArea,
                hash);

            await _repository.CrearUsuarioAsync(usuario);
        }

        public async Task<LoginResponse> IniciarSesionAsync(LoginRequest request)
        {
            var usuario = await _repository.ObtenerUsuarioPorNombreAsync(request.NombreUsuario);
            if (usuario is null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.ContrasenaHash))
                throw CredencialesInvalidas();

            if (usuario.Bloqueado)
                throw new CuentaBloqueadaException(
                    "La cuenta está bloqueada. Un Planner debe desbloquearla.");

            var resultado = _contrasenaHasher.Verificar(
                usuario.ContrasenaHash,
                request.Contrasena);

            if (resultado == ResultadoVerificacionContrasena.Fallida)
            {
                await _repository.RegistrarIntentoFallidoAsync(
                    usuario.IdUsuario,
                    DateTimeOffset.UtcNow);
                throw CredencialesInvalidas();
            }

            var ahora = DateTimeOffset.UtcNow;
            usuario.ReiniciarIntentosFallidos();

            if (usuario.ContrasenaEstaExpirada(ahora))
            {
                await _repository.GuardarCambiosAsync();
                throw new ContrasenaExpiradaException(
                    "La contraseña expiró. Debe cambiarla antes de iniciar sesión.");
            }

            if (resultado == ResultadoVerificacionContrasena.ExitosaRequiereRehash)
                usuario.ActualizarHashContrasena(_contrasenaHasher.CrearHash(request.Contrasena));

            var jti = Guid.NewGuid().ToString("N");
            var jwt = _jwtGenerator.Generar(
                usuario.IdUsuario,
                usuario.NombreUsuario,
                usuario.IdRol,
                jti,
                ahora);
            var sesion = new SesionUsuario(
                usuario.IdUsuario,
                jti,
                ahora,
                jwt.FechaExpiracion);

            await _repository.ReemplazarSesionAsync(sesion, ahora);

            return new LoginResponse(
                jwt.Token,
                jwt.FechaExpiracion,
                MapearUsuario(usuario));
        }

        public async Task<UsuarioAutenticadoDTO> ValidarSesionAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                throw SesionInvalida();

            var sesion = await _repository.ObtenerSesionPorJtiAsync(jti);
            if (sesion is null)
                throw SesionInvalida();

            var ahora = DateTimeOffset.UtcNow;
            if (!sesion.EstaVigente(ahora))
            {
                sesion.Revocar(ahora);
                await _repository.GuardarCambiosAsync();
                throw SesionInvalida();
            }

            var usuario = await _repository.ObtenerUsuarioPorIdAsync(sesion.IdUsuario);
            if (usuario is null || !usuario.Activo)
            {
                sesion.Revocar(ahora);
                await _repository.GuardarCambiosAsync();
                throw SesionInvalida();
            }
            if (usuario.Bloqueado)
            {
                sesion.Revocar(ahora);
                await _repository.GuardarCambiosAsync();
                throw new CuentaBloqueadaException("La cuenta está bloqueada.");
            }
            if (usuario.ContrasenaEstaExpirada(ahora))
            {
                sesion.Revocar(ahora);
                await _repository.GuardarCambiosAsync();
                throw new ContrasenaExpiradaException("La contraseña expiró.");
            }

            return MapearUsuario(usuario);
        }

        public async Task CerrarSesionAsync(string jti)
        {
            if (string.IsNullOrWhiteSpace(jti))
                throw SesionInvalida();

            var sesion = await _repository.ObtenerSesionPorJtiAsync(jti);
            if (sesion is null)
                throw SesionInvalida();

            sesion.Revocar(DateTimeOffset.UtcNow);
            await _repository.GuardarCambiosAsync();
        }

        public async Task CambiarContrasenaAsync(CambiarContrasenaRequest request)
        {
            var usuario = await _repository.ObtenerUsuarioPorNombreAsync(request.NombreUsuario);
            if (usuario is null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.ContrasenaHash))
                throw CredencialesInvalidas();
            if (usuario.Bloqueado)
                throw new CuentaBloqueadaException(
                    "La cuenta está bloqueada. Un Planner debe desbloquearla.");

            var resultado = _contrasenaHasher.Verificar(
                usuario.ContrasenaHash,
                request.ContrasenaActual);
            if (resultado == ResultadoVerificacionContrasena.Fallida)
            {
                await _repository.RegistrarIntentoFallidoAsync(
                    usuario.IdUsuario,
                    DateTimeOffset.UtcNow);
                throw CredencialesInvalidas();
            }

            ValidadorContrasena.Validar(request.NuevaContrasena);
            if (_contrasenaHasher.Verificar(
                    usuario.ContrasenaHash,
                    request.NuevaContrasena) != ResultadoVerificacionContrasena.Fallida)
                throw new ArgumentException("La nueva contraseña debe ser diferente a la actual.");

            var ahora = DateTimeOffset.UtcNow;
            usuario.CambiarContrasena(
                _contrasenaHasher.CrearHash(request.NuevaContrasena),
                ahora);
            await _repository.RevocarSesionesAsync(usuario.IdUsuario, ahora);
            await _repository.GuardarCambiosAsync();
        }

        private static UsuarioAutenticadoDTO MapearUsuario(Usuario usuario) => new(
            usuario.IdUsuario,
            usuario.NombreUsuario,
            usuario.Nombres,
            usuario.IdRol,
            usuario.IdArea);

        private static UsuarioNoAutenticadoException CredencialesInvalidas() =>
            new("Usuario o contraseña inválidos.");

        private static UsuarioNoAutenticadoException SesionInvalida() =>
            new("La sesión no es válida o expiró.");
    }
}
