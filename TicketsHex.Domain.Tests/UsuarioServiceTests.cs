using Microsoft.Extensions.Configuration;
using TicketsHex.Application.CasosUso.UsuarioCasosUso;
using TicketsHex.Application.DTO_s.Usuario;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;
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

    [Fact]
    public async Task Usuario_actualiza_su_imagen_de_perfil_sin_indicar_un_id()
    {
        var usuario = CrearUsuarioActual();
        var usuarios = new UsuarioRepositoryFake(usuario);
        var service = CrearServicio(
            usuarios,
            new AutenticacionRepositoryFake(usuario),
            new ContrasenaHasherFake());

        var resultado = await service.ActualizarPerfilPropioAsync(
            new ActualizarPerfilPropioRequest(ImagenPerfilBase64: "aW1hZ2Vu"));

        Assert.Equal("aW1hZ2Vu", usuario.ImagenPerfilBase64);
        Assert.Equal(usuario.IdUsuario, resultado.IdUsuario);
        Assert.True(usuarios.Actualizado);
    }

    [Fact]
    public async Task Actualizar_perfil_propio_requiere_indicar_la_imagen()
    {
        var usuario = CrearUsuarioActual();
        var usuarios = new UsuarioRepositoryFake(usuario);
        var service = CrearServicio(
            usuarios,
            new AutenticacionRepositoryFake(usuario),
            new ContrasenaHasherFake());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ActualizarPerfilPropioAsync(new ActualizarPerfilPropioRequest()));

        Assert.False(usuarios.Actualizado);
    }

    [Fact]
    public async Task Desbloquear_usuario_restablece_contrasena_temporal_y_fuerza_su_cambio()
    {
        var usuario = new Usuario(
            10,
            "usuario.bloqueado",
            "Usuario",
            "Bloqueado",
            Rol.Desarrollador,
            Area.Mantenimiento,
            "hash-anterior");
        for (var intento = 0; intento < Usuario.MaximoIntentosFallidos; intento++)
            usuario.RegistrarIntentoFallido(DateTimeOffset.UtcNow);

        var usuarios = new UsuarioRepositoryFake(usuario);
        var autenticacion = new AutenticacionRepositoryFake(usuario);
        var hasher = new ContrasenaHasherFake();
        var service = CrearServicio(usuarios, autenticacion, hasher);

        await service.DesbloquearAsync(usuario.IdUsuario);

        Assert.False(usuario.Bloqueado);
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Equal("Cambiar#2026", hasher.UltimaContrasena);
        Assert.Equal("hash-Cambiar#2026", usuario.ContrasenaHash);
        Assert.True(usuario.DebeCambiarContrasena);
        Assert.Equal(usuario.IdUsuario, autenticacion.IdUsuarioSesionesRevocadas);
        Assert.True(usuarios.Actualizado);
    }

    [Fact]
    public async Task No_permite_cambiar_rol_si_el_usuario_tiene_carga_activa()
    {
        var usuario = new Usuario(
            10,
            "usuario.dev",
            "Usuario",
            "Dev",
            Rol.Desarrollador,
            Area.Mantenimiento,
            "hash");
        var ticket = new Ticket(
            "CASO-010",
            "Ticket con carga",
            "Descripción suficientemente larga",
            usuario.IdUsuario,
            1,
            TicketTipo.Incidente,
            TicketPrioridad.Media,
            TicketImpacto.Medio);
        var service = CrearServicio(
            new UsuarioRepositoryFake(usuario),
            new AutenticacionRepositoryFake(usuario),
            new ContrasenaHasherFake(),
            new TicketRepositoryFake([ticket]));

        var error = await Assert.ThrowsAsync<ConflictoException>(() =>
            service.ActualizarAsync(usuario.IdUsuario, new ActualizarUsuarioRequest(
                usuario.NombreUsuario,
                usuario.Nombres,
                usuario.Apellidos,
                Rol.QA,
                usuario.IdArea,
                true)));

        Assert.Contains("USUARIO_CON_CARGA_ACTIVA", error.Message);
    }

    private static Usuario CrearUsuarioActual() => new(
        1,
        "usuario.actual",
        "Usuario",
        "Actual",
        Rol.Desarrollador,
        Area.Mantenimiento,
        "hash-actual",
        debeCambiarContrasena: true);

    private static UsuarioService CrearServicio(
        UsuarioRepositoryFake usuarios,
        AutenticacionRepositoryFake autenticacion,
        ContrasenaHasherFake hasher,
        TicketRepositoryFake? tickets = null)
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
            configuration,
            tickets ?? new TicketRepositoryFake());
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

    private sealed class AutenticacionRepositoryFake(Usuario? usuario = null) : IAutenticacionRepository
    {
        public long? IdUsuarioSesionesRevocadas { get; private set; }

        public Task<Usuario?> ObtenerUsuarioPorIdAsync(long idUsuario) =>
            Task.FromResult(usuario?.IdUsuario == idUsuario ? usuario : null);
        public Task<Usuario?> ObtenerUsuarioPorNombreAsync(string nombreUsuario) => Task.FromResult<Usuario?>(null);
        public Task<bool> ExisteUsuarioConContrasenaAsync() => Task.FromResult(false);
        public Task<SesionUsuario?> ObtenerSesionPorJtiAsync(string jti) => Task.FromResult<SesionUsuario?>(null);
        public Task RegistrarIntentoFallidoAsync(long idUsuario, DateTimeOffset fecha) => Task.CompletedTask;
        public Task CrearUsuarioAsync(Usuario usuario) => Task.CompletedTask;
        public Task ReemplazarSesionAsync(SesionUsuario nuevaSesion, DateTimeOffset fechaRevocacion) => Task.CompletedTask;
        public Task RevocarSesionesAsync(long idUsuario, DateTimeOffset fecha)
        {
            IdUsuarioSesionesRevocadas = idUsuario;
            return Task.CompletedTask;
        }
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }

    private sealed class TicketRepositoryFake(IReadOnlyCollection<Ticket>? tickets = null) : ITicketRepository
    {
        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult<Ticket?>(null);
        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));
        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));
        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(
            long idUsuario,
            TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));
        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>((tickets ?? [])
                .Where(ticket =>
                    ticket.IdUsuarioAsignado == idUsuario ||
                    ticket.Responsables.Any(responsable => responsable.IdUsuario == idUsuario))
                .ToArray());
        public Task GuardarAsync(Ticket ticket) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticket) => Task.CompletedTask;
        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;
    }
}
