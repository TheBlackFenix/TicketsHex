using TicketsHex.Application.CasosUso.AplicativoCasosUso;
using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Aplicativo;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Comun.Errores;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class AplicativoRepositorioServiceTests
{
    [Fact]
    public async Task Crea_aplicativo_con_varios_repositorios_en_una_sola_persistencia()
    {
        var aplicativos = new AplicativoRepositoryFake();
        var repositorios = new RepositorioRamaRepositoryFake(
            new Repositorio("wpf-pos", null, null, TipoRepositorio.Frontend),
            new Repositorio("api-central", null, null, TipoRepositorio.Backend));
        var service = CrearServicio(aplicativos, repositorios);
        var ids = repositorios.Repositorios.Select(item => item.IdRepositorio).ToArray();

        var idAplicativo = await service.CrearAplicativoAsync(new CrearAplicativoRequest(
            "POS",
            "Punto de venta",
            [ids[0], ids[1], ids[1]]));

        Assert.Equal(idAplicativo, aplicativos.AplicativoGuardado!.IdAplicativo);
        Assert.Equal(2, aplicativos.Relaciones.Count);
        Assert.All(aplicativos.Relaciones, relacion =>
            Assert.Equal(idAplicativo, relacion.IdAplicativo));
        Assert.Equal(1, aplicativos.CantidadPersistenciasAplicativo);

        var relacionados = await service.ObtenerRepositoriosAplicativoAsync(idAplicativo);
        Assert.Equal(2, relacionados.Count);
        Assert.Contains(relacionados, item =>
            item.Nombre == "wpf-pos" && item.IdTipoRepositorio == TipoRepositorio.Frontend);
        Assert.Contains(relacionados, item =>
            item.Nombre == "api-central" && item.IdTipoRepositorio == TipoRepositorio.Backend);
    }

    [Fact]
    public async Task Bloquea_retirar_aplicativo_si_deja_ramas_sin_respaldo()
    {
        var aplicativos = new AplicativoRepositoryFake();
        var tickets = new TicketRepositoryFake();
        var service = CrearServicio(
            aplicativos,
            new RepositorioRamaRepositoryFake(),
            tickets);
        var idAplicativo = await service.CrearAplicativoAsync(
            new CrearAplicativoRequest("POS", null));
        await service.AsignarAplicativoAsync(
            tickets.Ticket.IdTicket,
            new AsignarAplicativoTicketRequest(idAplicativo));
        aplicativos.TieneRamasSinRespaldo = true;

        var exception = await Assert.ThrowsAsync<ConflictoException>(() =>
            service.DesasignarAplicativoAsync(tickets.Ticket.IdTicket, idAplicativo));

        Assert.Equal(CodigosError.AplicativoConRamasAsociadas, exception.ObtenerCodigo());
        Assert.True(await aplicativos.ExisteAsignacionAsync(
            tickets.Ticket.IdTicket,
            idAplicativo));
    }

    [Fact]
    public async Task Bloquea_retirar_relacion_usada_por_tickets_activos()
    {
        var aplicativos = new AplicativoRepositoryFake();
        var repositorio = new Repositorio(
            "api-central",
            null,
            null,
            TipoRepositorio.Backend);
        var service = CrearServicio(
            aplicativos,
            new RepositorioRamaRepositoryFake(repositorio));
        var idAplicativo = await service.CrearAplicativoAsync(new CrearAplicativoRequest(
            "POS",
            null,
            [repositorio.IdRepositorio]));
        aplicativos.TieneTicketsActivosDependientes = true;

        var exception = await Assert.ThrowsAsync<ConflictoException>(() =>
            service.DesasignarRepositorioAsync(idAplicativo, repositorio.IdRepositorio));

        Assert.Equal(CodigosError.RelacionRepositorioEnUso, exception.ObtenerCodigo());
        Assert.True(await aplicativos.ExisteRelacionRepositorioAsync(
            idAplicativo,
            repositorio.IdRepositorio));
    }

    private static AplicativoService CrearServicio(
        AplicativoRepositoryFake aplicativos,
        RepositorioRamaRepositoryFake repositorios,
        TicketRepositoryFake? tickets = null)
    {
        aplicativos.RepositorioRepository = repositorios;
        return new AplicativoService(
            aplicativos,
            tickets ?? new TicketRepositoryFake(),
            repositorios,
            new UsuarioActualFake(3, Rol.LiderTecnico));
    }

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class AplicativoRepositoryFake : IAplicativoRepository
    {
        private readonly List<Aplicativo> _aplicativos = [];
        private readonly List<AplicativoTicket> _asignaciones = [];
        public List<RepositorioAplicativo> Relaciones { get; } = [];
        public Aplicativo? AplicativoGuardado { get; private set; }
        public int CantidadPersistenciasAplicativo { get; private set; }
        public bool TieneRamasSinRespaldo { get; set; }
        public bool TieneTicketsActivosDependientes { get; set; }
        public RepositorioRamaRepositoryFake? RepositorioRepository { get; set; }

        public Task<IReadOnlyCollection<Aplicativo>> ObtenerAplicativosAsync(bool incluirInactivos) =>
            Task.FromResult<IReadOnlyCollection<Aplicativo>>(_aplicativos);
        public Task<Aplicativo?> ObtenerAplicativoAsync(Guid idAplicativo) =>
            Task.FromResult(_aplicativos.SingleOrDefault(item => item.IdAplicativo == idAplicativo));
        public Task<Aplicativo?> ObtenerAplicativoPorNombreAsync(string nombre) =>
            Task.FromResult(_aplicativos.SingleOrDefault(item =>
                item.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyCollection<AplicativoTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<AplicativoTicket>>(
                _asignaciones.Where(item => item.IdTicket == idTicket).ToArray());
        public Task<IReadOnlyCollection<RepositorioAplicativo>> ObtenerRelacionesRepositorioAsync(Guid idAplicativo) =>
            Task.FromResult<IReadOnlyCollection<RepositorioAplicativo>>(
                Relaciones.Where(item => item.IdAplicativo == idAplicativo).ToArray());
        public Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAplicativoAsync(Guid idAplicativo)
        {
            var ids = Relaciones
                .Where(item => item.IdAplicativo == idAplicativo)
                .Select(item => item.IdRepositorio)
                .ToHashSet();
            return Task.FromResult<IReadOnlyCollection<Repositorio>>(
                (RepositorioRepository?.Repositorios ?? [])
                    .Where(item => ids.Contains(item.IdRepositorio))
                    .ToArray());
        }
        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idAplicativo) =>
            Task.FromResult(_asignaciones.Any(item =>
                item.IdTicket == idTicket && item.IdAplicativo == idAplicativo));
        public Task<bool> ExisteRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio) =>
            Task.FromResult(Relaciones.Any(item =>
                item.IdAplicativo == idAplicativo && item.IdRepositorio == idRepositorio));
        public Task<bool> TieneRamasTicketSinRespaldoAsync(Guid idTicket, Guid idAplicativo) =>
            Task.FromResult(TieneRamasSinRespaldo);
        public Task<bool> TieneTicketsActivosDependientesAsync(Guid idAplicativo, Guid idRepositorio) =>
            Task.FromResult(TieneTicketsActivosDependientes);
        public Task GuardarAplicativoAsync(
            Aplicativo aplicativo,
            IReadOnlyCollection<RepositorioAplicativo> relaciones)
        {
            AplicativoGuardado = aplicativo;
            CantidadPersistenciasAplicativo++;
            _aplicativos.Add(aplicativo);
            Relaciones.AddRange(relaciones);
            return Task.CompletedTask;
        }
        public Task GuardarRelacionRepositorioAsync(RepositorioAplicativo relacion)
        {
            Relaciones.Add(relacion);
            return Task.CompletedTask;
        }
        public Task EliminarRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio)
        {
            Relaciones.RemoveAll(item =>
                item.IdAplicativo == idAplicativo && item.IdRepositorio == idRepositorio);
            return Task.CompletedTask;
        }
        public Task GuardarAsignacionAsync(AplicativoTicket asignacion)
        {
            _asignaciones.Add(asignacion);
            return Task.CompletedTask;
        }
        public Task EliminarAsignacionAsync(Guid idTicket, Guid idAplicativo)
        {
            _asignaciones.RemoveAll(item =>
                item.IdTicket == idTicket && item.IdAplicativo == idAplicativo);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioRamaRepositoryFake : IRepositorioRamaRepository
    {
        public RepositorioRamaRepositoryFake(params Repositorio[] repositorios)
        {
            Repositorios.AddRange(repositorios);
        }

        public List<Repositorio> Repositorios { get; } = [];
        public Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAsync() =>
            Task.FromResult<IReadOnlyCollection<Repositorio>>(Repositorios);
        public Task<Repositorio?> ObtenerRepositorioAsync(Guid idRepositorio) =>
            Task.FromResult(Repositorios.SingleOrDefault(item => item.IdRepositorio == idRepositorio));
        public Task<Repositorio?> ObtenerRepositorioPorNombreAsync(string nombre) =>
            Task.FromResult(Repositorios.SingleOrDefault(item =>
                item.Nombre.Equals(nombre.Trim(), StringComparison.OrdinalIgnoreCase)));
        public Task<Rama?> ObtenerRamaAsync(Guid idRama) => Task.FromResult<Rama?>(null);
        public Task<Rama?> ObtenerRamaPorNombreAsync(Guid idRepositorio, string nombre) => Task.FromResult<Rama?>(null);
        public Task<IReadOnlyCollection<RamaTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<RamaTicket>>([]);
        public Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosDisponiblesTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<Repositorio>>([]);
        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idRama) => Task.FromResult(false);
        public Task<bool> RepositorioPerteneceAAplicativoTicketAsync(Guid idTicket, Guid idRepositorio) =>
            Task.FromResult(false);
        public Task GuardarRepositorioAsync(Repositorio repositorio) => Task.CompletedTask;
        public Task GuardarRamaAsync(Rama rama) => Task.CompletedTask;
        public Task GuardarAsignacionAsync(RamaTicket asignacion) => Task.CompletedTask;
        public Task EliminarAsignacionAsync(Guid idTicket, Guid idRama) => Task.CompletedTask;
    }

    private sealed class TicketRepositoryFake : ITicketRepository
    {
        public TicketRepositoryFake()
        {
            Ticket = new Ticket(
                "CASO-APP-001",
                "Ticket de aplicativo",
                "Descripción suficiente para el ticket",
                3,
                1,
                TicketTipo.Incidente,
                TicketPrioridad.Media,
                TicketImpacto.Medio,
                TicketOrigen.SAIA,
                esDesarrollo: true);
        }

        public Ticket Ticket { get; }
        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult<Ticket?>(id == Ticket.IdTicket ? Ticket : null);
        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));
        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(long idUsuario, TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));
        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(long idUsuario, TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([Ticket], 1, 20, 1));
        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>([]);
        public Task GuardarAsync(Ticket ticket) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticket) => Task.CompletedTask;
        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;
    }
}
