using TicketsHex.Application.CasosUso.AutenticacionCasosUso;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.infrastructure.Seguridad;
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
    }

    [Fact]
    public async Task Rechaza_login_cuando_contrasena_cumple_treinta_dias()
    {
        var contexto = CrearContexto();
        contexto.Usuario.CambiarContrasena(
            contexto.Hasher.CrearHash("Valida#2026"),
            DateTimeOffset.UtcNow.AddDays(-30));

        await Assert.ThrowsAsync<ContrasenaExpiradaException>(() =>
            contexto.Service.IniciarSesionAsync(
                new LoginRequest("planner", "Valida#2026")));
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
        var service = new AutenticacionService(
            repository,
            hasher,
            new GeneradorJwtFake());

        return new ContextoPrueba(service, hasher, usuario);
    }

    private static string ObtenerJti(string token) => token.Split('.')[1];

    private sealed record ContextoPrueba(
        AutenticacionService Service,
        ContrasenaHasher Hasher,
        Usuario Usuario);

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
        public TokenJwtGenerado Generar(
            long idUsuario,
            string nombreUsuario,
            Rol rol,
            string jti,
            DateTimeOffset fechaCreacion) =>
            new($"jwt.{jti}.firmado", fechaCreacion.AddMinutes(15));
    }
}
