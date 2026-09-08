using TicketsHex.Application.CasosUso.NotificacionCasosUso;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Notificacion;
using TicketsHex.Domain.Enums;
using Xunit;
using Microsoft.EntityFrameworkCore;
using PostgreSqlContext = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context.MantenimientoContext;
using SqlServerContext = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context.MantenimientoContext;

namespace TicketsHex.Domain.Tests;

public sealed class NotificacionTests
{
    [Fact]
    public void Notificacion_expira_exactamente_a_los_catorce_dias()
    {
        var fecha = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

        var notificacion = new NotificacionUsuario(
            2,
            Guid.NewGuid(),
            TipoNotificacion.Asignacion,
            "Ticket asignado",
            fecha);

        Assert.Equal(fecha.AddDays(14), notificacion.FechaExpiracion);
        Assert.False(notificacion.Leida);
    }

    [Fact]
    public void Ambos_adaptadores_configuran_la_bandeja_de_notificaciones()
    {
        using var sqlServer = new SqlServerContext(
            new DbContextOptionsBuilder<SqlServerContext>()
                .UseSqlServer("Server=(local);Database=TicketsHex;Trusted_Connection=True")
                .Options);
        using var postgreSql = new PostgreSqlContext(
            new DbContextOptionsBuilder<PostgreSqlContext>()
                .UseNpgsql("Host=localhost;Database=ticketshex;Username=test;Password=test")
                .Options);

        Assert.Equal(
            "notificacionesusuario",
            sqlServer.Model.FindEntityType(typeof(NotificacionUsuario))?.GetTableName());
        Assert.Equal(
            "notificacionesusuario",
            postgreSql.Model.FindEntityType(typeof(NotificacionUsuario))?.GetTableName());
    }

    [Fact]
    public async Task Usuario_puede_marcar_su_notificacion_como_leida()
    {
        var repository = new NotificacionRepositoryFake();
        var notificacion = CrearNotificacion(2);
        repository.Notificaciones.Add(notificacion);
        var publisher = new NotificacionPublisherFake();
        var command = new NotificacionCommand(
            repository,
            publisher,
            new UsuarioActualFake(2, Rol.Desarrollador));

        await command.MarcarLeidaAsync(notificacion.IdNotificacion);

        Assert.True(notificacion.Leida);
        Assert.Equal(1, repository.Guardados);
        Assert.Contains(2, publisher.Usuarios);
    }

    [Fact]
    public async Task Usuario_no_puede_marcar_notificacion_de_otro_usuario()
    {
        var repository = new NotificacionRepositoryFake();
        var notificacion = CrearNotificacion(3);
        repository.Notificaciones.Add(notificacion);
        var command = new NotificacionCommand(
            repository,
            new NotificacionPublisherFake(),
            new UsuarioActualFake(2, Rol.Desarrollador));

        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
            command.MarcarLeidaAsync(notificacion.IdNotificacion));

        Assert.False(notificacion.Leida);
    }

    [Fact]
    public async Task Marcar_todas_solo_afecta_notificaciones_del_usuario_actual()
    {
        var repository = new NotificacionRepositoryFake();
        repository.Notificaciones.AddRange([
            CrearNotificacion(2),
            CrearNotificacion(2),
            CrearNotificacion(3)
        ]);
        var command = new NotificacionCommand(
            repository,
            new NotificacionPublisherFake(),
            new UsuarioActualFake(2, Rol.Desarrollador));

        var cantidad = await command.MarcarTodasLeidasAsync();

        Assert.Equal(2, cantidad);
        Assert.All(
            repository.Notificaciones.Where(item => item.IdUsuarioDestinatario == 2),
            item => Assert.True(item.Leida));
        Assert.False(repository.Notificaciones.Single(item =>
            item.IdUsuarioDestinatario == 3).Leida);
    }

    private static NotificacionUsuario CrearNotificacion(long idUsuario) => new(
        idUsuario,
        Guid.NewGuid(),
        TipoNotificacion.Asignacion,
        "Ticket asignado");

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class NotificacionPublisherFake : INotificacionPublisher
    {
        public List<long> Usuarios { get; } = [];
        public Task PublicarResumenAsync() => Task.CompletedTask;
        public Task PublicarConteosNoLeidasAsync(IReadOnlyCollection<long> idsUsuarios)
        {
            Usuarios.AddRange(idsUsuarios);
            return Task.CompletedTask;
        }
    }

    private sealed class NotificacionRepositoryFake
        : INotificacionRepository, INotificacionUsuarioRepository
    {
        public List<NotificacionUsuario> Notificaciones { get; } = [];
        public int Guardados { get; private set; }

        public Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinHuAsync() =>
            Task.FromResult<IReadOnlyCollection<TicketNotificacionDTO>>([]);
        public Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosAsync() =>
            Task.FromResult<IReadOnlyCollection<TicketNotificacionDTO>>([]);
        public Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinRamasAsync() =>
            Task.FromResult<IReadOnlyCollection<TicketNotificacionDTO>>([]);
        public Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosORamasAsync() =>
            Task.FromResult<IReadOnlyCollection<TicketNotificacionDTO>>([]);
        public Task PrepararNotificacionesAsync(IReadOnlyCollection<NotificacionUsuario> notificaciones)
        {
            Notificaciones.AddRange(notificaciones);
            return Task.CompletedTask;
        }
        public Task<PaginaResultado<NotificacionUsuarioDTO>> ObtenerPaginaUsuarioAsync(long idUsuario, NotificacionFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<NotificacionUsuarioDTO>([], 1, 20, 0));
        public Task<int> ObtenerCantidadNoLeidasAsync(long idUsuario) =>
            Task.FromResult(Notificaciones.Count(item =>
                item.IdUsuarioDestinatario == idUsuario && !item.Leida));
        public Task<NotificacionUsuario?> ObtenerPorIdUsuarioAsync(Guid idNotificacion, long idUsuario) =>
            Task.FromResult(Notificaciones.SingleOrDefault(item =>
                item.IdNotificacion == idNotificacion &&
                item.IdUsuarioDestinatario == idUsuario));
        public Task<IReadOnlyCollection<NotificacionUsuario>> ObtenerNoLeidasUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<NotificacionUsuario>>(
                Notificaciones.Where(item =>
                    item.IdUsuarioDestinatario == idUsuario && !item.Leida).ToArray());
        public Task<IReadOnlyCollection<long>> ObtenerIdsUsuariosActivosPorRolesAsync(IReadOnlyCollection<Rol> roles) =>
            Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task<IReadOnlyCollection<long>> PrepararEliminacionPorTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task<IReadOnlyCollection<long>> EliminarExpiradasOFinalizadasAsync(DateTimeOffset fechaActual) =>
            Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task GuardarCambiosAsync()
        {
            Guardados++;
            return Task.CompletedTask;
        }
    }
}
