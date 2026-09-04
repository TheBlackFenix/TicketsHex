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
        var query = new TicketQuery(repository, new UsuarioActualFake(99, rol));

        var resultado = await query.ObtenerListaTicketsAsync(new TicketFiltroRequest());

        Assert.Single(resultado.Elementos);
    }

    [Fact]
    public async Task Lider_tecnico_puede_consultar_un_ticket_no_asignado()
    {
        var ticket = CrearTicket();
        var query = new TicketQuery(
            new TicketRepositoryFake(ticket),
            new UsuarioActualFake(99, Rol.LiderTecnico));

        var resultado = await query.ObtenerTicketPorIdAsync(ticket.IdTicket);

        Assert.Equal(ticket.IdTicket, resultado.IdTicket);
    }

    [Fact]
    public async Task Desarrollador_no_puede_consultar_el_listado_general()
    {
        var query = new TicketQuery(
            new TicketRepositoryFake(CrearTicket()),
            new UsuarioActualFake(2, Rol.Desarrollador));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            query.ObtenerListaTicketsAsync(new TicketFiltroRequest()));
    }

    [Fact]
    public async Task QA_consulta_el_listado_filtrado_por_estados_de_validacion()
    {
        var repository = new TicketRepositoryFake(CrearTicket());
        var query = new TicketQuery(repository, new UsuarioActualFake(3, Rol.QA));

        var resultado = await query.ObtenerListaTicketsAsync(new TicketFiltroRequest());

        Assert.True(repository.FueConsultadaPaginaQa);
        Assert.Single(resultado.Elementos);
    }

    [Fact]
    public async Task Historico_mis_tickets_consulta_asignaciones_del_usuario_actual()
    {
        var ticket = CrearTicket();
        var repository = new TicketRepositoryFake(ticket);
        var query = new TicketQuery(
            repository,
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
            new UsuarioActualFake(2, Rol.Desarrollador));

        var resultado = await query.ObtenerTicketPorIdAsync(ticket.IdTicket);

        Assert.Equal(TicketTipo.Incidente, resultado.Tipo);
        Assert.Equal(TicketPrioridad.Media, resultado.Prioridad);
        Assert.Equal(TicketImpacto.Medio, resultado.Impacto);
        Assert.Contains(AccionTicketPermitida.EditarDescripcion, resultado.Capacidades.AccionesPermitidas);
        Assert.Contains(resultado.Capacidades.TransicionesDisponibles, item =>
            item.EstadoDestino == TicketEstado.EnProceso);
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
        public long? IdUsuarioHistoricoConsultado { get; private set; }
        public TicketFiltroRequest? FiltroHistorico { get; private set; }
        public bool FueConsultadaPaginaQa { get; private set; }

        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult(ticket.IdTicket == id ? ticket : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([ticket], 1, 20, 1));

        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(TicketFiltroRequest filtro)
        {
            FueConsultadaPaginaQa = true;
            return Task.FromResult(new PaginaResultado<Ticket>([ticket], 1, 20, 1));
        }

        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(
            long idUsuario,
            TicketFiltroRequest filtro)
        {
            IdUsuarioHistoricoConsultado = idUsuario;
            FiltroHistorico = filtro;
            return Task.FromResult(new PaginaResultado<Ticket>([ticket], 1, 20, 1));
        }

        public Task GuardarAsync(Ticket ticketGuardado) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticketActualizado) => Task.CompletedTask;
        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>([]);
        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;
    }
}
