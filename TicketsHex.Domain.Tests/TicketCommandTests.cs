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
    [InlineData(Rol.QA)]
    [InlineData(Rol.LiderTecnico)]
    [InlineData(Rol.Planner)]
    public async Task Cualquier_rol_puede_crear_un_ticket(Rol rol)
    {
        var tickets = new TicketRepositoryFake();
        var command = CrearCommand(tickets, rol);

        var idTicket = await command.CrearTicketAsync(new CrearTicketRequest(
            "CASO-001",
            TicketOrigen.SAIA,
            "Ticket de prueba",
            "Descripción suficientemente larga",
            2));

        Assert.NotEqual(Guid.Empty, idTicket);
        Assert.NotNull(tickets.TicketGuardado);
    }

    [Fact]
    public async Task Puede_cambiar_estado_manteniendo_el_usuario_asignado_actual()
    {
        var tickets = new TicketRepositoryFake(CrearTicket());
        var command = CrearCommand(tickets, Rol.Desarrollador);

        await command.ActualizarTicketAsync(
            tickets.TicketGuardado!.IdTicket,
            new ActualizarTicketRequest(
                null,
                null,
                TicketEstado.EnProceso,
                tickets.TicketGuardado.IdUsuarioAsignado,
                null,
                null,
                null));

        Assert.Equal(TicketEstado.EnProceso, tickets.TicketGuardado.IdEstado);
        Assert.Equal(2, tickets.TicketGuardado.IdUsuarioAsignado);
        Assert.True(tickets.FueActualizado);
    }

    private static TicketCommand CrearCommand(TicketRepositoryFake tickets, Rol rol) =>
        new(tickets, new UsuarioRepositoryFake(), new UsuarioActualFake(1, rol));

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Ticket de prueba",
        "Descripción suficientemente larga",
        2,
        1,
        TicketOrigen.SAIA);

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Task<bool> ExisteAsync(long idUsuario) => Task.FromResult(idUsuario > 0);
        public Task<Usuario?> ObtenerPorIdAsync(long idUsuario) => Task.FromResult<Usuario?>(null);
        public Task<IReadOnlyCollection<Usuario>> ObtenerTodosAsync(bool incluirInactivos) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>([]);
        public Task GuardarAsync(Usuario usuario) => Task.CompletedTask;
        public Task ActualizarAsync(Usuario usuario) => Task.CompletedTask;
    }

    private sealed class TicketRepositoryFake(Ticket? ticket = null) : ITicketRepository
    {
        public Ticket? TicketGuardado { get; private set; } = ticket;
        public bool FueActualizado { get; private set; }

        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult(TicketGuardado?.IdTicket == id ? TicketGuardado : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));

        public Task GuardarAsync(Ticket ticketGuardado)
        {
            TicketGuardado = ticketGuardado;
            return Task.CompletedTask;
        }

        public Task ActualizarAsync(Ticket ticketActualizado)
        {
            TicketGuardado = ticketActualizado;
            FueActualizado = true;
            return Task.CompletedTask;
        }
    }
}
