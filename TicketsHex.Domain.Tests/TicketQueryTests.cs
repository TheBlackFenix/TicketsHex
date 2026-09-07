using TicketsHex.Application.CasosUso.TicketCasosUso;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class TicketQueryTests
{
    [Theory]
    [InlineData(Rol.Planner)]
    [InlineData(Rol.LiderTecnico)]
    public async Task Planner_y_lider_tecnico_pueden_consultar_todos_los_tickets(Rol rol)
    {
        var repository = new TicketRepositoryFake(CrearTicket());
        var query = new TicketQuery(
            repository,
            new HistorialAsignacionRepositoryFake(repository.Ticket),
            new UsuarioActualFake(99, rol));

        var resultado = await query.ObtenerListaTicketsAsync(new TicketFiltroRequest());

        Assert.Single(resultado.Elementos);
    }

    [Fact]
    public async Task Lider_tecnico_puede_consultar_un_ticket_no_asignado()
    {
        var ticket = CrearTicket();
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            new HistorialAsignacionRepositoryFake(ticket),
            new UsuarioActualFake(99, Rol.LiderTecnico));

        var resultado = await query.ObtenerTicketPorIdAsync(ticket.IdTicket);

        Assert.Equal(ticket.IdTicket, resultado.IdTicket);
    }

    [Fact]
    public async Task Desarrollador_no_puede_consultar_el_listado_general()
    {
        var ticket = CrearTicket();
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            new HistorialAsignacionRepositoryFake(ticket),
            new UsuarioActualFake(2, Rol.Desarrollador));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.ObtenerListaTicketsAsync(new TicketFiltroRequest()));
    }

    [Fact]
    public async Task QA_consulta_el_listado_filtrado_por_estados_de_validacion()
    {
        var repository = new TicketRepositoryFake(CrearTicket());
        var query = new TicketQuery(
            repository,
            new HistorialAsignacionRepositoryFake(repository.Ticket),
            new UsuarioActualFake(3, Rol.QA));

        var resultado = await query.ObtenerListaTicketsAsync(new TicketFiltroRequest());

        Assert.True(repository.FueConsultadaPaginaQa);
        Assert.Equal(3, repository.IdUsuarioPaginaQa);
        Assert.Single(resultado.Elementos);
    }

    [Fact]
    public async Task Historico_mis_tickets_consulta_asignaciones_del_usuario_actual()
    {
        var ticket = CrearTicket();
        var repository = new TicketRepositoryFake(ticket);
        var query = new TicketQuery(
            repository,
            new HistorialAsignacionRepositoryFake(ticket),
            new UsuarioActualFake(7, Rol.Desarrollador));

        var resultado = await query.ObtenerHistoricoMisTicketsAsync(
            new TicketFiltroRequest(IdUsuarioAsignado: 99, IncluirEliminados: true));

        Assert.Equal(7, repository.IdUsuarioHistoricoConsultado);
        Assert.False(repository.FiltroHistorico!.IncluirEliminados);
        Assert.Null(repository.FiltroHistorico.IdUsuarioAsignado);
        Assert.Single(resultado.Elementos);
        Assert.Equal(ticket.IdTicket, resultado.Elementos.Single().IdTicket);
    }

    [Fact]
    public async Task DTO_incluye_clasificacion_y_capacidades_del_usuario_actual()
    {
        var ticket = CrearTicket();
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            new HistorialAsignacionRepositoryFake(ticket),
            new UsuarioActualFake(2, Rol.Desarrollador));

        var resultado = await query.ObtenerTicketPorIdAsync(ticket.IdTicket);

        Assert.Equal(TicketTipo.Incidente, resultado.Tipo);
        Assert.Equal(TicketPrioridad.Media, resultado.Prioridad);
        Assert.Equal(TicketImpacto.Medio, resultado.Impacto);
        Assert.Contains(AccionTicketPermitida.EditarDescripcion, resultado.Capacidades.AccionesPermitidas);
        Assert.Contains(resultado.Capacidades.TransicionesDisponibles, item =>
            item.EstadoDestino == TicketEstado.EnProceso);
    }

    [Fact]
    public async Task QA_colaborativo_puede_consultar_ticket_en_estado_QA_sin_ser_responsable()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.EnRevisionQA;
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            new HistorialAsignacionRepositoryFake(ticket),
            new UsuarioActualFake(50, Rol.QA));

        var resultado = await query.ObtenerTicketPorIdAsync(ticket.IdTicket);

        Assert.Equal(ticket.IdTicket, resultado.IdTicket);
        Assert.Contains(AccionTicketPermitida.Comentar, resultado.Capacidades.AccionesPermitidas);
    }

    [Fact]
    public async Task QA_no_designado_no_consulta_ticket_en_estado_de_desarrollo()
    {
        var ticket = CrearTicket();
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            new HistorialAsignacionRepositoryFake(ticket),
            new UsuarioActualFake(50, Rol.QA));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.ObtenerTicketPorIdAsync(ticket.IdTicket));
    }

    [Fact]
    public async Task Historial_asignaciones_se_consulta_separado_y_paginado()
    {
        var ticket = CrearTicket();
        var historialRepository = new HistorialAsignacionRepositoryFake(ticket);
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            historialRepository,
            new UsuarioActualFake(2, Rol.Desarrollador));

        var resultado = await query.ObtenerHistorialAsignacionesAsync(
            ticket.IdTicket,
            new HistorialAsignacionTicketFiltroRequest(2, 10));

        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(10, resultado.TamanoPagina);
        Assert.True(historialRepository.FueConsultado);
        Assert.Equal(2, historialRepository.IdUsuarioValidacion);
    }

    [Fact]
    public async Task Historial_asignaciones_rechaza_usuario_sin_acceso()
    {
        var ticket = CrearTicket();
        var historialRepository = new HistorialAsignacionRepositoryFake(ticket);
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            historialRepository,
            new UsuarioActualFake(50, Rol.Desarrollador));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.ObtenerHistorialAsignacionesAsync(
                ticket.IdTicket,
                new HistorialAsignacionTicketFiltroRequest()));

        Assert.False(historialRepository.FueConsultado);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Historial_asignaciones_valida_limites_de_paginacion(
        int pagina,
        int tamanoPagina)
    {
        var ticket = CrearTicket();
        var historialRepository = new HistorialAsignacionRepositoryFake(ticket);
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            historialRepository,
            new UsuarioActualFake(2, Rol.Desarrollador));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            query.ObtenerHistorialAsignacionesAsync(
                ticket.IdTicket,
                new HistorialAsignacionTicketFiltroRequest(pagina, tamanoPagina)));

        Assert.False(historialRepository.FueConsultado);
    }

    [Fact]
    public void Historial_identifica_registros_migrados_sin_inventar_datos()
    {
        var usuario = new UsuarioReferenciaTicketDTO(2, "Usuario Desarrollo");
        var registroMigrado = new HistorialAsignacionTicketDTO(
            Guid.NewGuid(),
            null,
            usuario,
            usuario,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);
        var registroCompleto = registroMigrado with
        {
            Estado = TicketEstado.EnAnalisis,
            TipoMovimiento = TipoMovimientoAsignacionTicket.AsignacionInicial
        };

        Assert.True(registroMigrado.EsRegistroMigrado);
        Assert.False(registroCompleto.EsRegistroMigrado);
    }

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Ticket de prueba",
        "DescripciÃ³n suficientemente larga",
        2,
        1,
        TicketTipo.Incidente,
        TicketPrioridad.Media,
        TicketImpacto.Medio,
        TicketOrigen.SAIA);

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class TicketRepositoryFake(Ticket ticket) : ITicketRepository
    {
        public Ticket Ticket { get; } = ticket;
        public long? IdUsuarioHistoricoConsultado { get; private set; }
        public TicketFiltroRequest? FiltroHistorico { get; private set; }
        public bool FueConsultadaPaginaQa { get; private set; }
        public long? IdUsuarioPaginaQa { get; private set; }

        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult(Ticket.IdTicket == id ? Ticket : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));

        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(
            long idUsuario,
            TicketFiltroRequest filtro)
        {
            FueConsultadaPaginaQa = true;
            IdUsuarioPaginaQa = idUsuario;
            return Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));
        }

        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(
            long idUsuario,
            TicketFiltroRequest filtro)
        {
            IdUsuarioHistoricoConsultado = idUsuario;
            FiltroHistorico = filtro;
            return Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));
        }

        public Task GuardarAsync(Ticket ticketGuardado) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticketActualizado) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>([]);
        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;
    }

    private sealed class HistorialAsignacionRepositoryFake(Ticket ticket)
        : IHistorialAsignacionTicketRepository
    {
        public bool FueConsultado { get; private set; }
        public long? IdUsuarioValidacion { get; private set; }

        public Task<Ticket?> ObtenerTicketParaValidarAccesoAsync(
            Guid idTicket,
            long idUsuario,
            bool incluirEliminados)
        {
            IdUsuarioValidacion = idUsuario;
            return Task.FromResult<Ticket?>(
                ticket.IdTicket == idTicket && (ticket.Activo || incluirEliminados)
                    ? ticket
                    : null);
        }

        public Task<PaginaResultado<HistorialAsignacionTicketDTO>> ObtenerPaginaAsync(
            Guid idTicket,
            HistorialAsignacionTicketFiltroRequest filtro)
        {
            FueConsultado = true;
            return Task.FromResult(new PaginaResultado<HistorialAsignacionTicketDTO>(
                [],
                filtro.Pagina,
                filtro.TamanoPagina,
                0));
        }
    }
}
