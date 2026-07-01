using TicketsHex.Application.CasosUso.RepositorioCasosUso;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Repositorio;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class RepositorioRamaServiceTests
{
    [Fact]
    public async Task Lider_tecnico_crea_rama_y_la_asigna_a_un_ticket()
    {
        var configuracion = new RepositorioRamaRepositoryFake();
        var tickets = new TicketRepositoryFake();
        var usuario = new UsuarioActualFake(3, Rol.LiderTecnico);
        var service = new RepositorioRamaService(configuracion, tickets, usuario);
        var ticket = tickets.Ticket;

        var idRepositorio = await service.CrearRepositorioAsync(
            new CrearRepositorioRequest(
                "tickets-api",
                "https://git.example.com/tickets-api",
                null));
        var idRama = await service.CrearRamaAsync(
            idRepositorio,
            new CrearRamaRequest("feature/ticket-123"));
        var idAsignacion = await service.AsignarRamaAsync(
            ticket.IdTicket,
            new AsignarRamaTicketRequest(idRepositorio, idRama));

        var ramas = await service.ObtenerRamasTicketAsync(ticket.IdTicket);
        var asignacion = Assert.Single(ramas);
        Assert.Equal(idAsignacion, asignacion.IdRamaTicket);
        Assert.Equal("tickets-api", asignacion.Repositorio);
        Assert.Equal("feature/ticket-123", asignacion.Rama);
    }

    [Fact]
    public async Task Usuario_que_no_es_lider_no_puede_crear_repositorio()
    {
        var service = new RepositorioRamaService(
            new RepositorioRamaRepositoryFake(),
            new TicketRepositoryFake(),
            new UsuarioActualFake(1, Rol.Planner));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CrearRepositorioAsync(
                new CrearRepositorioRequest("tickets-api", null, null)));
    }

    [Fact]
    public async Task No_permite_asignar_una_rama_desde_otro_repositorio()
    {
        var configuracion = new RepositorioRamaRepositoryFake();
        var tickets = new TicketRepositoryFake();
        var service = new RepositorioRamaService(
            configuracion,
            tickets,
            new UsuarioActualFake(3, Rol.LiderTecnico));

        var repositorioUno = await service.CrearRepositorioAsync(
            new CrearRepositorioRequest("repo-uno", null, null));
        var repositorioDos = await service.CrearRepositorioAsync(
            new CrearRepositorioRequest("repo-dos", null, null));
        var rama = await service.CrearRamaAsync(
            repositorioUno,
            new CrearRamaRequest("feature/uno"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AsignarRamaAsync(
                tickets.Ticket.IdTicket,
                new AsignarRamaTicketRequest(repositorioDos, rama)));
    }

    private sealed class UsuarioActualFake : IUsuarioActual
    {
        public UsuarioActualFake(long idUsuario, Rol rol)
        {
            IdUsuario = idUsuario;
            Rol = rol;
        }

        public long IdUsuario { get; }
        public Rol Rol { get; }
    }

    private sealed class TicketRepositoryFake : ITicketRepository
    {
        public TicketRepositoryFake()
        {
            Ticket = new Ticket(
                "CASO-001",
                "Ticket de prueba",
                "Descripción del ticket de prueba",
                3,
                1,
                TicketOrigen.SAIA);
        }

        public Ticket Ticket { get; }

        public Task<Ticket?> ObtenerPorIdAsync(
            Guid id,
            bool incluirEliminados = false) =>
            Task.FromResult<Ticket?>(id == Ticket.IdTicket ? Ticket : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));

        public Task GuardarAsync(Ticket ticket) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticket) => Task.CompletedTask;
    }

    private sealed class RepositorioRamaRepositoryFake : IRepositorioRamaRepository
    {
        private readonly List<Repositorio> _repositorios = [];
        private readonly List<Rama> _ramas = [];
        private readonly List<RamaTicket> _asignaciones = [];

        public Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAsync() =>
            Task.FromResult<IReadOnlyCollection<Repositorio>>(_repositorios);

        public Task<Repositorio?> ObtenerRepositorioAsync(Guid idRepositorio) =>
            Task.FromResult(_repositorios.SingleOrDefault(
                item => item.IdRepositorio == idRepositorio));

        public Task<Repositorio?> ObtenerRepositorioPorNombreAsync(string nombre) =>
            Task.FromResult(_repositorios.SingleOrDefault(
                item => item.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<Rama?> ObtenerRamaAsync(Guid idRama) =>
            Task.FromResult(_ramas.SingleOrDefault(item => item.IdRama == idRama));

        public Task<Rama?> ObtenerRamaPorNombreAsync(Guid idRepositorio, string nombre) =>
            Task.FromResult(_ramas.SingleOrDefault(item =>
                item.IdRepositorio == idRepositorio &&
                item.NombreRama.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyCollection<RamaTicket>> ObtenerAsignacionesTicketAsync(
            Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<RamaTicket>>(
                _asignaciones.Where(item => item.IdTicket == idTicket).ToArray());

        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idRama) =>
            Task.FromResult(_asignaciones.Any(item =>
                item.IdTicket == idTicket && item.IdRama == idRama));

        public Task GuardarRepositorioAsync(Repositorio repositorio)
        {
            _repositorios.Add(repositorio);
            return Task.CompletedTask;
        }

        public Task GuardarRamaAsync(Rama rama)
        {
            _ramas.Add(rama);
            return Task.CompletedTask;
        }

        public Task GuardarAsignacionAsync(RamaTicket asignacion)
        {
            _asignaciones.Add(asignacion);
            return Task.CompletedTask;
        }

        public Task EliminarAsignacionAsync(Guid idTicket, Guid idRama)
        {
            _asignaciones.RemoveAll(item =>
                item.IdTicket == idTicket && item.IdRama == idRama);
            return Task.CompletedTask;
        }
    }
}
