using TicketsHex.Application.CasosUso.TicketCasosUso;
using TicketsHex.Application.CasosUso.NotificacionCasosUso;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Notificacion;
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

    [Theory]
    [InlineData("Causa manual", null)]
    [InlineData(null, "Solución manual")]
    public async Task Patch_rechaza_modificar_resumenes_fuera_de_conocimiento(
        string? causaRaiz,
        string? solucionPropuesta)
    {
        var tickets = new TicketRepositoryFake(CrearTicket());
        var command = CrearCommand(tickets, Rol.Planner);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            command.ActualizarTicketAsync(
                tickets.TicketGuardado!.IdTicket,
                new ActualizarTicketRequest(
                    Titulo: null,
                    Descripcion: null,
                    NuevoEstado: null,
                    CausaRaiz: causaRaiz,
                    SolucionPropuesta: solucionPropuesta,
                    Comentario: null)));

        Assert.Equal(
            TicketsHex.Domain.Comun.Errores.CodigosError.FuenteConocimientoRequerida,
            TicketsHex.Domain.Comun.Errores.CodigosError.ObtenerCodigo(error));
        Assert.False(tickets.FueActualizado);
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

    [Fact]
    public async Task Creacion_notifica_a_responsables_iniciales_excepto_al_autor()
    {
        var tickets = new TicketRepositoryFake();
        var notificaciones = new NotificacionRepositoryFake();
        var publisher = new NotificacionPublisherFake();
        var command = CrearCommand(tickets, Rol.Planner, notificaciones, publisher);

        await command.CrearTicketAsync(new CrearTicketRequest(
            "CASO-NOT-001",
            TicketOrigen.SAIA,
            "Ticket con responsables",
            "Descripción suficientemente larga",
            2,
            TicketTipo.Incidente,
            TicketPrioridad.Media,
            TicketImpacto.Medio,
            IdQaResponsable: 3));

        Assert.Equal([2L, 3L], notificaciones.Notificaciones
            .Select(item => item.IdUsuarioDestinatario)
            .OrderBy(item => item));
        Assert.Equal(2, publisher.Eventos.Count);
    }

    [Fact]
    public async Task Desarrollador_que_crea_ticket_no_se_notifica_a_si_mismo()
    {
        var tickets = new TicketRepositoryFake();
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(tickets, Rol.Desarrollador, notificaciones);

        await command.CrearTicketAsync(new CrearTicketRequest(
            "CASO-NOT-002",
            TicketOrigen.SAIA,
            "Ticket creado por desarrollo",
            "Descripción suficientemente larga",
            2,
            TicketTipo.Incidente,
            TicketPrioridad.Media,
            TicketImpacto.Medio));

        Assert.Empty(notificaciones.Notificaciones);
    }

    [Fact]
    public async Task Bloqueo_notifica_a_planners_y_lideres_activos()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.EnProceso;
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Desarrollador,
            notificaciones);

        await command.ActualizarTicketAsync(
            ticket.IdTicket,
            new ActualizarTicketRequest(
                null,
                null,
                TicketEstado.Bloqueado,
                null,
                null,
                "Dependencia externa"));

        Assert.Equal([1L, 10L], notificaciones.Notificaciones
            .Select(item => item.IdUsuarioDestinatario)
            .OrderBy(item => item));
        Assert.All(notificaciones.Notificaciones, item =>
            Assert.Equal(TipoNotificacion.Bloqueo, item.IdTipoNotificacion));
    }

    [Theory]
    [InlineData(TicketEstado.BUG, TipoNotificacion.Bug)]
    [InlineData(TicketEstado.Rollback, TipoNotificacion.Rollback)]
    public async Task Bug_y_rollback_notifican_a_dev_y_qa(
        TicketEstado estado,
        TipoNotificacion tipo)
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.EnRevisionQA;
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Planner,
            notificaciones);

        await command.ActualizarTicketAsync(
            ticket.IdTicket,
            new ActualizarTicketRequest(
                null,
                null,
                estado,
                null,
                null,
                "Incidencia detectada"));

        Assert.Equal([2L, 3L], notificaciones.Notificaciones
            .Select(item => item.IdUsuarioDestinatario)
            .OrderBy(item => item));
        Assert.All(notificaciones.Notificaciones, item =>
            Assert.Equal(tipo, item.IdTipoNotificacion));
    }

    [Fact]
    public async Task Entrada_a_revision_QA_notifica_al_responsable_QA()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.DespliegueApitesting;
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Planner,
            notificaciones);

        await command.ActualizarTicketAsync(
            ticket.IdTicket,
            new ActualizarTicketRequest(
                null,
                null,
                TicketEstado.EnRevisionApitesting,
                null,
                null,
                null));

        var notificacion = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(3, notificacion.IdUsuarioDestinatario);
        Assert.Equal(TipoNotificacion.SolicitudQa, notificacion.IdTipoNotificacion);
    }

    [Fact]
    public async Task Devolucion_desde_replica_notifica_a_dev_y_qa()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.EnReplicaQA;
        ticket.IdUsuarioAsignado = 3;
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Planner,
            notificaciones);

        await command.ActualizarTicketAsync(
            ticket.IdTicket,
            new ActualizarTicketRequest(
                null,
                null,
                TicketEstado.EnAnalisis,
                null,
                null,
                "Escenario aclarado"));

        Assert.Equal([2L, 3L], notificaciones.Notificaciones
            .Select(item => item.IdUsuarioDestinatario)
            .OrderBy(item => item));
        Assert.All(notificaciones.Notificaciones, item =>
            Assert.Equal(TipoNotificacion.DevolucionQa, item.IdTipoNotificacion));
    }

    [Fact]
    public async Task Reasignacion_notifica_solo_al_nuevo_responsable()
    {
        var ticket = CrearTicket();
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Planner,
            notificaciones);

        await command.AsignarResponsableDesarrolloAsync(
            ticket.IdTicket,
            new AsignarResponsableTicketRequest(5, "Cambio de equipo"));

        var notificacion = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(5, notificacion.IdUsuarioDestinatario);
        Assert.DoesNotContain(notificaciones.Notificaciones, item =>
            item.IdUsuarioDestinatario == 2);
    }

    [Fact]
    public async Task Hu_y_carpeta_generan_una_sola_notificacion_al_ser_agregadas()
    {
        var ticket = CrearTicket();
        var notificaciones = new NotificacionRepositoryFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Planner,
            notificaciones);
        var request = new ActualizarTicketRequest(
            null,
            null,
            null,
            null,
            null,
            null,
            EsDesarrollo: true,
            NombreHu: "HU-500",
            UrlHu: "https://dev.azure.com/equipo/proyecto/_workitems/edit/500",
            CarpetaMedios: "medios/caso-500");

        await command.ActualizarTicketAsync(ticket.IdTicket, request);
        await command.ActualizarTicketAsync(ticket.IdTicket, request);

        var notificacion = Assert.Single(notificaciones.Notificaciones);
        Assert.Equal(2, notificacion.IdUsuarioDestinatario);
        Assert.Equal(TipoNotificacion.DatosDesarrolloDisponibles, notificacion.IdTipoNotificacion);
        Assert.Contains("HU y carpeta", notificacion.Mensaje);
    }

    [Fact]
    public async Task Finalizar_elimina_notificaciones_existentes_sin_crear_otra()
    {
        var ticket = CrearTicket();
        var notificaciones = new NotificacionRepositoryFake();
        notificaciones.Notificaciones.Add(new NotificacionUsuario(
            2,
            ticket.IdTicket,
            TipoNotificacion.Asignacion,
            "Notificación existente"));
        var publisher = new NotificacionPublisherFake();
        var command = CrearCommand(
            new TicketRepositoryFake(ticket),
            Rol.Planner,
            notificaciones,
            publisher);

        await command.ActualizarTicketAsync(
            ticket.IdTicket,
            new ActualizarTicketRequest(
                null,
                null,
                TicketEstado.Finalizado,
                null,
                null,
                "Caso resuelto"));

        Assert.Empty(notificaciones.Notificaciones);
        Assert.Empty(publisher.Eventos);
        Assert.Contains(2, publisher.UsuariosConConteoActualizado);
    }

    private static TicketCommand CrearCommand(
        TicketRepositoryFake tickets,
        Rol rol,
        NotificacionRepositoryFake? notificaciones = null,
        NotificacionPublisherFake? publisher = null)
    {
        var idUsuario = rol == Rol.Desarrollador ? 2 : 1;
        var usuarioActual = new UsuarioActualFake(idUsuario, rol);
        var repository = notificaciones ?? new NotificacionRepositoryFake();
        var notificacionPublisher = publisher ?? new NotificacionPublisherFake();
        return new(
            tickets,
            new UsuarioRepositoryFake(rol),
            usuarioActual,
            new NotificacionTicketService(
                repository,
                notificacionPublisher,
                usuarioActual));
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
                5 => Rol.Desarrollador,
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

        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(
            long idUsuario,
            TicketFiltroRequest filtro) =>
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
        public List<NotificacionEventoDTO> Eventos { get; } = [];
        public List<long> UsuariosConConteoActualizado { get; } = [];

        public Task PublicarResumenAsync() => Task.CompletedTask;
        public Task PublicarNotificacionesAsync(IReadOnlyCollection<NotificacionEventoDTO> notificaciones)
        {
            Eventos.AddRange(notificaciones);
            return Task.CompletedTask;
        }
        public Task PublicarConteosNoLeidasAsync(IReadOnlyCollection<long> idsUsuarios)
        {
            UsuariosConConteoActualizado.AddRange(idsUsuarios);
            return Task.CompletedTask;
        }
    }

    private sealed class NotificacionRepositoryFake
        : INotificacionRepository, INotificacionUsuarioRepository
    {
        public List<NotificacionUsuario> Notificaciones { get; } = [];

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
        public Task<int> ObtenerCantidadNoLeidasAsync(long idUsuario) => Task.FromResult(0);
        public Task<NotificacionUsuario?> ObtenerPorIdUsuarioAsync(Guid idNotificacion, long idUsuario) =>
            Task.FromResult(Notificaciones.SingleOrDefault(item =>
                item.IdNotificacion == idNotificacion && item.IdUsuarioDestinatario == idUsuario));
        public Task<IReadOnlyCollection<NotificacionUsuario>> ObtenerNoLeidasUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<NotificacionUsuario>>(
                Notificaciones.Where(item => item.IdUsuarioDestinatario == idUsuario && !item.Leida).ToArray());
        public Task<IReadOnlyCollection<long>> ObtenerIdsUsuariosActivosPorRolesAsync(IReadOnlyCollection<Rol> roles) =>
            Task.FromResult<IReadOnlyCollection<long>>([1, 10]);
        public Task<IReadOnlyCollection<long>> PrepararEliminacionPorTicketAsync(Guid idTicket)
        {
            var ids = Notificaciones
                .Where(item => item.IdTicket == idTicket && !item.Leida)
                .Select(item => item.IdUsuarioDestinatario)
                .Distinct()
                .ToArray();
            Notificaciones.RemoveAll(item => item.IdTicket == idTicket);
            return Task.FromResult<IReadOnlyCollection<long>>(ids);
        }
        public Task<IReadOnlyCollection<long>> EliminarExpiradasOFinalizadasAsync(DateTimeOffset fechaActual) =>
            Task.FromResult<IReadOnlyCollection<long>>([]);
        public Task GuardarCambiosAsync() => Task.CompletedTask;
    }
}
