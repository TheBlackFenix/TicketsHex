using Microsoft.Extensions.Configuration;
using TicketsHex.Application.CasosUso.UsuarioCasosUso;
using TicketsHex.Application.DTO_s.Usuario;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class UsuarioServiceTests
{
    [Fact]
    public async Task Crear_usuario_usa_contrasena_por_defecto_configurada()
    {
        var usuarios = new UsuarioRepositoryFake();
        var autenticacion = new AutenticacionRepositoryFake();
        var hasher = new ContrasenaHasherFake();
        var service = CrearServicio(usuarios, autenticacion, hasher);

        await service.CrearAsync(new CrearUsuarioRequest(
            10,
            "usuario.nuevo",
            "Usuario",
            "Nuevo",
            Rol.Desarrollador,
            Area.Mantenimiento,
            "aW1hZ2Vu"));

        Assert.Equal("Cambiar#2026", hasher.UltimaContrasena);
        Assert.NotNull(usuarios.UsuarioGuardado);
        Assert.Equal("hash-Cambiar#2026", usuarios.UsuarioGuardado.ContrasenaHash);
        Assert.Equal("aW1hZ2Vu", usuarios.UsuarioGuardado.ImagenPerfilBase64);
        Assert.True(usuarios.UsuarioGuardado.DebeCambiarContrasena);
    }

    [Fact]
    public async Task Crear_usuario_rechaza_imagen_perfil_que_no_es_base64()
    {
        var service = CrearServicio(
            new UsuarioRepositoryFake(),
            new AutenticacionRepositoryFake(),
            new ContrasenaHasherFake());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CrearAsync(
            new CrearUsuarioRequest(
                10,
                "usuario.nuevo",
                "Usuario",
                "Nuevo",
                Rol.Desarrollador,
                Area.Mantenimiento,
                "no-es-base64")));
    }

    [Fact]
    public async Task Actualizar_usuario_actualiza_imagen_perfil_base64()
    {
        var usuario = new Usuario(
            10,
            "usuario.actual",
            "Usuario",
            "Actual",
            Rol.Desarrollador,
            Area.Mantenimiento,
            "hash");
        var usuarios = new UsuarioRepositoryFake(usuario);
        var service = CrearServicio(
            usuarios,
            new AutenticacionRepositoryFake(),
            new ContrasenaHasherFake());

        await service.ActualizarAsync(10, new ActualizarUsuarioRequest(
            "usuario.actual",
            "Usuario",
            "Actualizado",
            Rol.Desarrollador,
            Area.Mantenimiento,
            true,
            "data:image/png;base64,aW1hZ2Vu"));

        Assert.Equal("data:image/png;base64,aW1hZ2Vu", usuario.ImagenPerfilBase64);
        Assert.True(usuarios.Actualizado);
    }

    [Fact]
    public async Task Actualizar_usuario_sin_imagen_no_borra_imagen_existente()
    {
        var usuario = new Usuario(
            10,
            "usuario.actual",
            "Usuario",
            "Actual",
            Rol.Desarrollador,
            Area.Mantenimiento,
            "hash",
            "aW1hZ2Vu");
        var service = CrearServicio(
            new UsuarioRepositoryFake(usuario),
            new AutenticacionRepositoryFake(),
            new ContrasenaHasherFake());

        await service.ActualizarAsync(10, new ActualizarUsuarioRequest(
            "usuario.actual",
            "Usuario",
            "Actualizado",
            Rol.Desarrollador,
            Area.Mantenimiento,
            true));

        Assert.Equal("aW1hZ2Vu", usuario.ImagenPerfilBase64);
    }

    private static UsuarioService CrearServicio(
        UsuarioRepositoryFake usuarios,
        AutenticacionRepositoryFake autenticacion,
        ContrasenaHasherFake hasher)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Usuarios:ContrasenaPorDefecto"] = "Cambiar#2026"
            })
            .Build();

        return new UsuarioService(
            usuarios,
            new UsuarioActualFake(),
            autenticacion,
            hasher,
            configuration);
    }

    private sealed class UsuarioActualFake : IUsuarioActual
    {
        public long IdUsuario => 1;
        public Rol Rol => Rol.Planner;
    }

    private sealed class ContrasenaHasherFake : IContrasenaHasher
    {
        public string? UltimaContrasena { get; private set; }

        public string CrearHash(string contrasena)
        {
            UltimaContrasena = contrasena;
            return $"hash-{contrasena}";
        }

        public ResultadoVerificacionContrasena Verificar(string hash, string contrasena) =>
            ResultadoVerificacionContrasena.Fallida;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        private readonly Usuario? _usuario;

        public UsuarioRepositoryFake(Usuario? usuario = null)
        {
            _usuario = usuario;
        }

        public Usuario? UsuarioGuardado { get; private set; }
        public bool Actualizado { get; private set; }

        public Task<Usuario?> ObtenerPorIdAsync(long idUsuario) =>
            Task.FromResult(_usuario?.IdUsuario == idUsuario ? _usuario : null);

        public Task<IReadOnlyCollection<Usuario>> ObtenerTodosAsync(bool incluirInactivos) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>(_usuario is null ? [] : [_usuario]);

        public Task<bool> ExisteAsync(long idUsuario) => Task.FromResult(_usuario?.IdUsuario == idUsuario);

        public Task GuardarAsync(Usuario usuario)
        {
            UsuarioGuardado = usuario;
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Usuario usuario)
        {
            Actualizado = true;
            return Task.CompletedTask;
        }
    }

    private sealed class AutenticacionRepositoryFake : IAutenticacionRepository
    {
        public Task<Usuario?> ObtenerUsuarioPorIdAsync(long idUsuario) => Task.FromResult<Usuario?>(null);
        public Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario) => Task.FromResult<Usuario?>(null);
        public Task<bool> ExisteUsuarioConContrasenaAsync() => Task.FromResult(false);
        public Task<SesionUsuario?> ObtenerSesionPorJtiAsync(string jti) => Task.FromResult<SesionUsuario?>(null);
        public Task RegistrarIntentoFallidoAsync(long idUsuario, DateTimeOffset fecha) => Task.CompletedTask;
        public Task CrearUsuarioAsync(Usuario usuario) => Task.CompletedTask;
        public Task ReemplazarSesionAsync(SesionUsuario nuevaSesion, DateTimeOffset fechaRevocacion) => Task.CompletedTask;
        public Task RevocarSesionesAsync(long idUsuario, DateTimeOffset fecha) => Task.CompletedTask;
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }
}
