using TicketsHex.Application.CasosUso.TicketCasosUso;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class TicketCommandTests
{
    [Theory]
    [InlineData(Rol.Desarrollador)]
    [InlineData(Rol.LiderTecnico)]
    [InlineData(Rol.Planner)]
    public async Task Desarrollador_lider_y_planner_pueden_crear_un_ticket(Rol rol)
    {
        var tickets = new TicketRepositoryFake();
        var command = CrearCommand(tickets, rol);

        var idTicket = await command.CrearTicketAsync(new CrearTicketRequest(
            "CASO-001",
            TicketOrigen.SAIA,
            "Ticket de prueba",
            "Descripción suficientemente larga",
            2,
            TicketTipo.Incidente,
            TicketPrioridad.Media,
            TicketImpacto.Medio));

        Assert.NotEqual(Guid.Empty, idTicket);
        Assert.NotNull(tickets.TicketGuardado);
        Assert.False(tickets.TicketGuardado.EsDesarrollo);
        Assert.Equal(2, tickets.TicketGuardado.IdUsuarioAsignado);
    }

    [Fact]
    public async Task QA_no_puede_crear_tickets()
    {
        var command = CrearCommand(new TicketRepositoryFake(), Rol.QA);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            command.CrearTicketAsync(new CrearTicketRequest(
                "CASO-001",
                TicketOrigen.SAIA,
                "Ticket de prueba",
                "Descripción suficientemente larga",
                2,
                TicketTipo.Incidente,
                TicketPrioridad.Media,
                TicketImpacto.Medio)));
    }

    [Fact]
    public async Task Puede_cambiar_estado_manteniendo_el_usuario_asignado_actual()
    {
        var tickets = new TicketRepositoryFake(CrearTicket());
        var command = CrearCommand(tickets, Rol.Desarrollador);

        await command.ActualizarTicketAsync(
            tickets.TicketGuardado!.IdTicket,
            new ActualizarTicketRequest(
                Titulo: null,
                Descripcion: null,
                NuevoEstado: TicketEstado.EnProceso,
                CausaRaiz: null,
                SolucionPropuesta: null,
                Comentario: null));

        Assert.Equal(TicketEstado.EnProceso, tickets.TicketGuardado.IdEstado);
        Assert.Equal(2, tickets.TicketGuardado.IdUsuarioAsignado);
        Assert.True(tickets.FueActualizado);
    }

    [Fact]
    public async Task Planner_actualiza_datos_de_desarrollo_y_HU()
    {
        var tickets = new TicketRepositoryFake(CrearTicket());
        var command = CrearCommand(tickets, Rol.Planner);

        await command.ActualizarTicketAsync(
            tickets.TicketGuardado!.IdTicket,
            new ActualizarTicketRequest(
                Titulo: null,
                Descripcion: null,
                NuevoEstado: null,
                CausaRaiz: null,
                SolucionPropuesta: null,
                Comentario: null,
                EsDesarrollo: true,
                NombreHu: "HU-1234",
                UrlHu: "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
                CarpetaMedios: "medios/caso-001"));

        Assert.True(tickets.TicketGuardado.EsDesarrollo);
        Assert.Equal("HU-1234", tickets.TicketGuardado.NombreHu);
        Assert.Equal(
            "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
            tickets.TicketGuardado.UrlHu);
        Assert.True(tickets.FueActualizado);
    }

    [Fact]
    public async Task Planner_crea_ticket_con_HU_y_carpeta_en_una_sola_persistencia()
    {
        var tickets = new TicketRepositoryFake();
        var command = CrearCommand(tickets, Rol.Planner);

        await command.CrearTicketAsync(new CrearTicketRequest(
            "CASO-002",
            TicketOrigen.SAIA,
            "Ticket de desarrollo",
            "Descripción suficientemente larga",
            2,
            TicketTipo.Requerimiento,
            TicketPrioridad.Alta,
            TicketImpacto.Alto,
            EsDesarrollo: true,
            IdQaResponsable: 3,
            NombreHu: "HU-200",
            UrlHu: "https://dev.azure.com/equipo/proyecto/_workitems/edit/200",
            CarpetaMedios: "medios/caso-002"));

        Assert.Equal(1, tickets.CantidadGuardados);
        Assert.False(tickets.FueActualizado);
        Assert.Equal(TicketTipo.Requerimiento, tickets.TicketGuardado!.IdTipo);
        Assert.Equal("HU-200", tickets.TicketGuardado.NombreHu);
        Assert.Equal("medios/caso-002", tickets.TicketGuardado.CarpetaMedios);
    }

    [Fact]
    public async Task Desarrollador_puede_incluir_carpeta_pero_no_HU_en_la_creacion()
    {
        var tickets = new TicketRepositoryFake();
        var command = CrearCommand(tickets, Rol.Desarrollador);

        await command.CrearTicketAsync(new CrearTicketRequest(
            "CASO-003",
            TicketOrigen.SAIA,
            "Ticket de desarrollo",
            "Descripción suficientemente larga",
            2,
            TicketTipo.Incidente,
            TicketPrioridad.Media,
            TicketImpacto.Medio,
            EsDesarrollo: true,
            CarpetaMedios: "medios/caso-003"));

        Assert.Equal("medios/caso-003", tickets.TicketGuardado!.CarpetaMedios);

        var otroRepositorio = new TicketRepositoryFake();
        var otroCommand = CrearCommand(otroRepositorio, Rol.Desarrollador);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            otroCommand.CrearTicketAsync(new CrearTicketRequest(
                "CASO-004",
                TicketOrigen.SAIA,
                "Ticket de desarrollo",
                "Descripción suficientemente larga",
                2,
                TicketTipo.Incidente,
                TicketPrioridad.Media,
                TicketImpacto.Medio,
                EsDesarrollo: true,
                NombreHu: "HU-201",
                UrlHu: "https://dev.azure.com/equipo/proyecto/_workitems/edit/201")));
        Assert.Equal(0, otroRepositorio.CantidadGuardados);
    }

    [Fact]
    public async Task No_persiste_si_se_envian_datos_tecnicos_sin_ser_desarrollo()
    {
        var tickets = new TicketRepositoryFake();
        var command = CrearCommand(tickets, Rol.Planner);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            command.CrearTicketAsync(new CrearTicketRequest(
                "CASO-005",
                TicketOrigen.SAIA,
                "Ticket no desarrollo",
                "Descripción suficientemente larga",
                2,
                TicketTipo.Incidente,
                TicketPrioridad.Baja,
                TicketImpacto.Bajo,
                CarpetaMedios: "medios/no-valido")));

        Assert.Equal(0, tickets.CantidadGuardados);
    }

    private static TicketCommand CrearCommand(TicketRepositoryFake tickets, Rol rol)
    {
        var idUsuario = rol == Rol.Desarrollador ? 2 : 1;
        return new(
            tickets,
            new UsuarioRepositoryFake(rol),
            new UsuarioActualFake(idUsuario, rol),
            new NotificacionPublisherFake());
    }

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Ticket de prueba",
        "Descripción suficientemente larga",
        2,
        1,
        TicketTipo.Incidente,
        TicketPrioridad.Media,
        TicketImpacto.Medio,
        TicketOrigen.SAIA,
        usuarioQa: 3);

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class UsuarioRepositoryFake(Rol rolActual) : IUsuarioRepository
    {
        public Task<bool> ExisteAsync(long idUsuario) => Task.FromResult(idUsuario > 0);
        public Task<Usuario?> ObtenerPorIdAsync(long idUsuario)
        {
            var rol = idUsuario switch
            {
                2 => Rol.Desarrollador,
                3 => Rol.QA,
                _ => rolActual
            };
            return Task.FromResult<Usuario?>(new Usuario(
                idUsuario,
                $"usuario{idUsuario}",
                "Usuario",
                null,
                rol,
                Area.Mantenimiento,
                "hash"));
        }
        public Task<IReadOnlyCollection<Usuario>> ObtenerTodosAsync(bool incluirInactivos) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>([]);
        public Task GuardarAsync(Usuario usuario) => Task.CompletedTask;
        public Task ActualizarAsync(Usuario usuario) => Task.CompletedTask;
    }

    private sealed class TicketRepositoryFake(Ticket? ticket = null) : ITicketRepository
    {
        public Ticket? TicketGuardado { get; private set; } = ticket;
        public bool FueActualizado { get; private set; }
        public int CantidadGuardados { get; private set; }

        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult(TicketGuardado?.IdTicket == id ? TicketGuardado : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));

        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));

        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(
            long idUsuario,
            TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));

        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>([]);

        public Task GuardarAsync(Ticket ticketGuardado)
        {
            TicketGuardado = ticketGuardado;
            CantidadGuardados++;
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Ticket ticketActualizado)
        {
            TicketGuardado = ticketActualizado;
            FueActualizado = true;
            return Task.CompletedTask;
        }

        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;
    }

    private sealed class NotificacionPublisherFake : INotificacionPublisher
    {
        public Task PublicarResumenAsync() => Task.CompletedTask;
    }
}
