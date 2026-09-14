using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.Servicios;

using TicketsHex.Domain.Comun.Errores;
using TicketsHex.Application.Comun.Configuracion;
using System.Security.Cryptography;
using System.Text;

namespace TicketsHex.Application.CasosUso.AutenticacionCasosUso
{
    public sealed class AutenticacionService : IAutenticacionService
    {
        private readonly IAutenticacionRepository _repository;
        private readonly IContrasenaHasher _contrasenaHasher;
        private readonly IGeneradorJwtSesion _jwtGenerator;
        private readonly UsuariosOptions _usuariosOptions;
        private readonly RefreshOptions _refreshOptions;

        public AutenticacionService(
            IAutenticacionRepository repository,
            IContrasenaHasher contrasenaHasher,
            IGeneradorJwtSesion jwtGenerator,
            UsuariosOptions? usuariosOptions = null,
            RefreshOptions? refreshOptions = null)
        {
            _repository = repository;
            _contrasenaHasher = contrasenaHasher;
            _jwtGenerator = jwtGenerator;
            _usuariosOptions = usuariosOptions ?? new UsuariosOptions();
            _refreshOptions = refreshOptions ?? new RefreshOptions();
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

            if (resultado == ResultadoVerificacionContrasena.ExitosaRequiereRehash)
                usuario.ActualizarHashContrasena(_contrasenaHasher.CrearHash(request.Contrasena));

            var jti = Guid.NewGuid().ToString("N");
            var debeCambiarContrasena = usuario.RequiereCambioContrasena(
                ahora,
                _usuariosOptions.DiasVigenciaContrasena);
            var jwt = _jwtGenerator.Generar(
                usuario.IdUsuario,
                usuario.NombreUsuario,
                usuario.IdRol,
                jti,
                ahora,
                debeCambiarContrasena);
            var secretoRefresh = CrearSecretoRefresh();
            var expiracionRefresh = ahora.AddDays(_refreshOptions.DiasVigencia);
            var sesion = new SesionUsuario(
                usuario.IdUsuario,
                jti,
                ahora,
                expiracionRefresh,
                CalcularHash(secretoRefresh));

            await _repository.ReemplazarSesionAsync(sesion, ahora);

            return new LoginResponse(
                jwt.Token,
                jwt.FechaExpiracion,
                MapearUsuario(usuario, ahora))
            {
                FechaExpiracionRefresh = expiracionRefresh,
                RefreshToken = $"{sesion.IdSesion:N}.{secretoRefresh}"
            };
        }

        public async Task<LoginResponse> RenovarSesionAsync(string refreshToken)
        {
            var (idSesion, secreto) = LeerRefreshToken(refreshToken);
            var sesion = await _repository.ObtenerSesionPorIdAsync(idSesion);
            var ahora = DateTimeOffset.UtcNow;
            if (sesion is null || !sesion.EstaVigente(ahora) ||
                string.IsNullOrWhiteSpace(sesion.RefreshTokenHash))
                throw SesionInvalida();

            var hashActual = CalcularHash(secreto);
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromHexString(sesion.RefreshTokenHash),
                    Convert.FromHexString(hashActual)))
                throw SesionInvalida();

            var usuario = await _repository.ObtenerUsuarioPorIdAsync(sesion.IdUsuario);
            if (usuario is null || !usuario.Activo || usuario.Bloqueado)
                throw SesionInvalida();

            var jti = Guid.NewGuid().ToString("N");
            var debeCambiarContrasena = usuario.RequiereCambioContrasena(
                ahora,
                _usuariosOptions.DiasVigenciaContrasena);
            var jwt = _jwtGenerator.Generar(
                usuario.IdUsuario,
                usuario.NombreUsuario,
                usuario.IdRol,
                jti,
                ahora,
                debeCambiarContrasena);
            var nuevoSecreto = CrearSecretoRefresh();
            if (!await _repository.RotarSesionAsync(
                    idSesion,
                    hashActual,
                    CalcularHash(nuevoSecreto),
                    jti,
                    ahora))
                throw SesionInvalida();

            return new LoginResponse(jwt.Token, jwt.FechaExpiracion, MapearUsuario(usuario, ahora))
            {
                FechaExpiracionRefresh = sesion.FechaExpiracion,
                RefreshToken = $"{idSesion:N}.{nuevoSecreto}"
            };
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
            return MapearUsuario(usuario, ahora);
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

        public async Task CerrarSesionConRefreshAsync(string refreshToken)
        {
            var (idSesion, secreto) = LeerRefreshToken(refreshToken);
            if (!await _repository.RevocarSesionPorRefreshAsync(
                    idSesion,
                    CalcularHash(secreto),
                    DateTimeOffset.UtcNow))
                throw SesionInvalida();
        }

        public async Task CambiarContrasenaAsync(long idUsuario, CambiarContrasenaRequest request)
        {
            var usuario = await _repository.ObtenerUsuarioPorIdAsync(idUsuario);
            if (usuario is null || !usuario.Activo || string.IsNullOrWhiteSpace(usuario.ContrasenaHash))
                throw new UsuarioNoAutenticadoException(
                    "La contraseña actual no es correcta.",
                    CodigosError.ContrasenaActualInvalida);
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
                throw new UsuarioNoAutenticadoException(
                    "La contraseña actual no es correcta.",
                    CodigosError.ContrasenaActualInvalida);
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

        private UsuarioAutenticadoDTO MapearUsuario(
            Usuario usuario,
            DateTimeOffset fechaActual) => new(
            usuario.IdUsuario,
            usuario.NombreUsuario,
            usuario.Nombres,
            usuario.IdRol,
            usuario.IdArea,
            usuario.RequiereCambioContrasena(
                fechaActual,
                _usuariosOptions.DiasVigenciaContrasena));

        private static UsuarioNoAutenticadoException CredencialesInvalidas() =>
            new("Usuario o contraseña inválidos.");

        private static UsuarioNoAutenticadoException SesionInvalida() =>
            new("La sesión no es válida o expiró.", CodigosError.SesionInvalida);

        private static string CrearSecretoRefresh() =>
            Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        private static string CalcularHash(string secreto) =>
            Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(secreto)));

        private static (Guid IdSesion, string Secreto) LeerRefreshToken(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                throw SesionInvalida();
            var partes = refreshToken.Split('.', 2);
            if (partes.Length != 2 || partes[0].Length != 32 || partes[1].Length != 64 ||
                !Guid.TryParseExact(partes[0], "N", out var idSesion) ||
                !partes[1].All(Uri.IsHexDigit))
                throw SesionInvalida();
            return (idSesion, partes[1]);
        }
    }
}
