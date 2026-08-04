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

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Ticket de prueba",
        "DescripciÃ³n suficientemente larga",
        2,
        1,
        TicketOrigen.SAIA);

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class TicketRepositoryFake(Ticket ticket) : ITicketRepository
    {
        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult(ticket.IdTicket == id ? ticket : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([ticket], 1, 20, 1));

        public Task GuardarAsync(Ticket ticketGuardado) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticketActualizado) => Task.CompletedTask;
    }
}
