using TicketsHex.Application.CasosUso.AutenticacionCasosUso;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.infrastructure.Seguridad;
using System.Text.Json;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class AutenticacionServiceTests
{
    [Fact]
    public async Task Bloquea_usuario_despues_de_cinco_contrasenas_incorrectas()
    {
        var contexto = CrearContexto();

        for (var intento = 0; intento < 5; intento++)
        {
            await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
                contexto.Service.IniciarSesionAsync(new LoginRequest("planner", "Incorrecta#1")));
        }

        Assert.True(contexto.Usuario.Bloqueado);
        Assert.Equal(5, contexto.Usuario.IntentosFallidos);
    }

    [Fact]
    public async Task Segundo_login_revoca_token_anterior_y_crea_una_nueva_sesion()
    {
        var contexto = CrearContexto();

        var primerLogin = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));
        var segundoLogin = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        Assert.NotEqual(primerLogin.Token, segundoLogin.Token);
        await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.ValidarSesionAsync(ObtenerJti(primerLogin.Token)));

        var usuario = await contexto.Service.ValidarSesionAsync(
            ObtenerJti(segundoLogin.Token));
        Assert.Equal(contexto.Usuario.IdUsuario, usuario.IdUsuario);
        await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.RenovarSesionAsync(primerLogin.RefreshToken!));
    }

    [Fact]
    public async Task Refresh_rota_credencial_e_invalida_el_access_token_anterior()
    {
        var contexto = CrearContexto();
        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        var renovado = await contexto.Service.RenovarSesionAsync(login.RefreshToken!);

        Assert.NotEqual(login.Token, renovado.Token);
        Assert.NotEqual(login.RefreshToken, renovado.RefreshToken);
        Assert.Equal(login.FechaExpiracionRefresh, renovado.FechaExpiracionRefresh);
        await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.ValidarSesionAsync(ObtenerJti(login.Token)));
        await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.RenovarSesionAsync(login.RefreshToken!));
        Assert.NotNull(await contexto.Service.ValidarSesionAsync(ObtenerJti(renovado.Token)));
    }

    [Fact]
    public async Task Refresh_no_expone_credencial_en_respuesta_json()
    {
        var contexto = CrearContexto();
        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        var json = JsonSerializer.Serialize(login);

        Assert.NotNull(login.RefreshToken);
        Assert.DoesNotContain(login.RefreshToken!, json);
        Assert.DoesNotContain("RefreshToken", json);
    }

    [Fact]
    public async Task Refresh_con_contrasena_expirada_sigue_emitiendo_token_restringido()
    {
        var contexto = CrearContexto();
        contexto.Usuario.CambiarContrasena(
            contexto.Hasher.CrearHash("Valida#2026"),
            DateTimeOffset.UtcNow.AddDays(-30));
        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        var renovado = await contexto.Service.RenovarSesionAsync(login.RefreshToken!);

        Assert.True(renovado.Usuario.DebeCambiarContrasena);
        Assert.True(contexto.GeneradorJwt.UltimoSoloCambioContrasena);
    }

    [Fact]
    public async Task Logout_con_refresh_revoca_la_sesion_sin_access_token_vigente()
    {
        var contexto = CrearContexto();
        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        await contexto.Service.CerrarSesionConRefreshAsync(login.RefreshToken!);

        await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.RenovarSesionAsync(login.RefreshToken!));
    }

    [Fact]
    public async Task Login_con_contrasena_expirada_entrega_token_restringido()
    {
        var contexto = CrearContexto();
        contexto.Usuario.CambiarContrasena(
            contexto.Hasher.CrearHash("Valida#2026"),
            DateTimeOffset.UtcNow.AddDays(-30));

        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        Assert.NotEmpty(login.Token);
        Assert.True(login.Usuario.DebeCambiarContrasena);
        Assert.True(contexto.GeneradorJwt.UltimoSoloCambioContrasena);
        var sesion = await contexto.Service.ValidarSesionAsync(ObtenerJti(login.Token));
        Assert.True(sesion.DebeCambiarContrasena);
    }

    [Fact]
    public async Task Login_devuelve_si_usuario_debe_cambiar_contrasena()
    {
        var contexto = CrearContexto();
        contexto.Usuario.CambiarContrasena(
            contexto.Hasher.CrearHash("Valida#2026"),
            DateTimeOffset.UtcNow,
            debeCambiarContrasena: true);

        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        Assert.True(login.Usuario.DebeCambiarContrasena);
    }

    [Fact]
    public async Task Cambiar_contrasena_desactiva_debe_cambiar_contrasena()
    {
        var contexto = CrearContexto();
        contexto.Usuario.CambiarContrasena(
            contexto.Hasher.CrearHash("Valida#2026"),
            DateTimeOffset.UtcNow,
            debeCambiarContrasena: true);

        await contexto.Service.CambiarContrasenaAsync(
            contexto.Usuario.IdUsuario,
            new CambiarContrasenaRequest("Valida#2026", "Nueva#2026"));

        Assert.False(contexto.Usuario.DebeCambiarContrasena);
    }

    [Fact]
    public async Task Cambiar_contrasena_revoca_refresh_token()
    {
        var contexto = CrearContexto();
        var login = await contexto.Service.IniciarSesionAsync(
            new LoginRequest("planner", "Valida#2026"));

        await contexto.Service.CambiarContrasenaAsync(
            contexto.Usuario.IdUsuario,
            new CambiarContrasenaRequest("Valida#2026", "Nueva#2026"));

        await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.RenovarSesionAsync(login.RefreshToken!));
    }

    [Fact]
    public async Task Cambiar_contrasena_rechaza_la_contrasena_actual_incorrecta()
    {
        var contexto = CrearContexto();
        var hashActual = contexto.Usuario.ContrasenaHash;

        var error = await Assert.ThrowsAsync<UsuarioNoAutenticadoException>(() =>
            contexto.Service.CambiarContrasenaAsync(
                contexto.Usuario.IdUsuario,
                new CambiarContrasenaRequest("Incorrecta#2026", "Nueva#2026")));

        Assert.Equal(
            TicketsHex.Domain.Comun.Errores.CodigosError.ContrasenaActualInvalida,
            TicketsHex.Domain.Comun.Errores.CodigosError.ObtenerCodigo(error));
        Assert.Equal(hashActual, contexto.Usuario.ContrasenaHash);
        Assert.Equal(1, contexto.Usuario.IntentosFallidos);
    }

    private static ContextoPrueba CrearContexto()
    {
        var hasher = new ContrasenaHasher();
        var usuario = new Usuario(
            1,
            "planner",
            "Usuario",
            "Planner",
            Rol.Planner,
            Area.Mantenimiento,
            hasher.CrearHash("Valida#2026"));
        var repository = new AutenticacionRepositoryFake(usuario);
        var generadorJwt = new GeneradorJwtFake();
        var service = new AutenticacionService(
            repository,
            hasher,
            generadorJwt);

        return new ContextoPrueba(service, hasher, usuario, generadorJwt);
    }

    private static string ObtenerJti(string token) => token.Split('.')[1];

    private sealed record ContextoPrueba(
        AutenticacionService Service,
        ContrasenaHasher Hasher,
        Usuario Usuario,
        GeneradorJwtFake GeneradorJwt);

    private sealed class AutenticacionRepositoryFake : IAutenticacionRepository
    {
        private readonly List<Usuario> _usuarios = [];
        private readonly List<SesionUsuario> _sesiones = [];

        public AutenticacionRepositoryFake(Usuario usuario)
        {
            _usuarios.Add(usuario);
        }

        public Task<Usuario?> ObtenerUsuarioPorIdAsync(long idUsuario) =>
            Task.FromResult(_usuarios.FirstOrDefault(u => u.IdUsuario == idUsuario));

        public Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario) =>
            Task.FromResult(_usuarios.FirstOrDefault(u =>
                string.Equals(
                    u.NombreUsuario,
                    nombreUsuario,
                    StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExisteUsuarioConContrasenaAsync() =>
            Task.FromResult(_usuarios.Any(u => !string.IsNullOrWhiteSpace(u.ContrasenaHash)));

        public Task<SesionUsuario?> ObtenerSesionPorJtiAsync(string jti) =>
            Task.FromResult(_sesiones.FirstOrDefault(s =>
                s.Jti == jti && s.FechaRevocacion is null));

        public Task<SesionUsuario?> ObtenerSesionPorIdAsync(Guid idSesion) =>
            Task.FromResult(_sesiones.FirstOrDefault(s => s.IdSesion == idSesion));

        public Task<bool> RotarSesionAsync(
            Guid idSesion,
            string hashActual,
            string hashNuevo,
            string nuevoJti,
            DateTimeOffset fechaActual)
        {
            var sesion = _sesiones.FirstOrDefault(s =>
                s.IdSesion == idSesion &&
                s.RefreshTokenHash == hashActual &&
                s.EstaVigente(fechaActual));
            if (sesion is null)
                return Task.FromResult(false);
            sesion.Rotar(nuevoJti, hashNuevo);
            return Task.FromResult(true);
        }

        public Task<bool> RevocarSesionPorRefreshAsync(
            Guid idSesion,
            string refreshTokenHash,
            DateTimeOffset fechaActual)
        {
            var sesion = _sesiones.FirstOrDefault(s =>
                s.IdSesion == idSesion &&
                s.RefreshTokenHash == refreshTokenHash &&
                s.FechaRevocacion is null);
            if (sesion is null)
                return Task.FromResult(false);
            sesion.Revocar(fechaActual);
            return Task.FromResult(true);
        }

        public Task RegistrarIntentoFallidoAsync(long idUsuario, DateTimeOffset fecha)
        {
            _usuarios.Single(u => u.IdUsuario == idUsuario).RegistrarIntentoFallido(fecha);
            return Task.CompletedTask;
        }

        public Task CrearUsuarioAsync(Usuario usuario)
        {
            _usuarios.Add(usuario);
            return Task.CompletedTask;
        }

        public Task ReemplazarSesionAsync(
            SesionUsuario nuevaSesion,
            DateTimeOffset fechaRevocacion)
        {
            foreach (var sesion in _sesiones.Where(s =>
                         s.IdUsuario == nuevaSesion.IdUsuario &&
                         s.FechaRevocacion is null))
                sesion.Revocar(fechaRevocacion);

            _sesiones.Add(nuevaSesion);
            return Task.CompletedTask;
        }

        public Task RevocarSesionesAsync(long idUsuario, DateTimeOffset fecha)
        {
            foreach (var sesion in _sesiones.Where(s =>
                         s.IdUsuario == idUsuario &&
                         s.FechaRevocacion is null))
                sesion.Revocar(fecha);

            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private sealed class GeneradorJwtFake : IGeneradorJwtSesion
    {
        public bool UltimoSoloCambioContrasena { get; private set; }

        public TokenJwtGenerado Generar(
            long idUsuario,
            string nombreUsuario,
            Rol rol,
            string jti,
            DateTimeOffset fechaCreacion,
            bool soloCambioContrasena)
        {
            UltimoSoloCambioContrasena = soloCambioContrasena;
            return new(
                $"jwt.{jti}.firmado",
                fechaCreacion.AddMinutes(soloCambioContrasena ? 10 : 15));
        }
    }
}
