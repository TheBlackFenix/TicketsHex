using TicketsHex.Application.CasosUso.ConocimientoCasosUso;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Conocimiento;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Entidades.Conocimiento;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;
using System.Text.Json;

namespace TicketsHex.Domain.Tests;

public sealed class ConocimientoTicketTests
{
    [Fact]
    public void Entrada_rechaza_resultado_de_otro_tipo()
    {
        Assert.Throws<ArgumentException>(() => new EntradaConocimientoTicket(
            Guid.NewGuid(),
            TipoEntradaConocimiento.Diagnostico,
            ResultadoEntradaConocimiento.SolucionExitosa,
            "Hipótesis técnica",
            null,
            null,
            null,
            null,
            null,
            null,
            2,
            Rol.Desarrollador));
    }

    [Fact]
    public void Editar_entrada_conserva_revision_del_contenido_anterior()
    {
        var entrada = CrearDiagnostico("Hipótesis inicial");

        entrada.Actualizar(
            ResultadoEntradaConocimiento.DiagnosticoDescartado,
            "Hipótesis descartada",
            "Síntoma",
            "Prueba ejecutada",
            "Paso uno",
            1,
            null,
            null,
            null,
            2,
            Rol.Desarrollador,
            TicketEstado.EnProceso);

        var revision = Assert.Single(entrada.Revisiones);
        using var contenido = JsonDocument.Parse(revision.ContenidoAnterior);
        Assert.Equal(
            "Hipótesis inicial",
            contenido.RootElement.GetProperty("Resumen").GetString());
        Assert.Equal(TicketEstado.EnProceso, revision.IdEstadoTicket);
        Assert.Equal("Hipótesis descartada", entrada.Resumen);
    }

    [Fact]
    public async Task Desarrollador_asignado_puede_crear_varios_diagnosticos()
    {
        var ticket = CrearTicket();
        var repository = new ConocimientoRepositoryFake();
        var service = CrearService(ticket, repository, 2, Rol.Desarrollador);

        await service.CrearDiagnosticoAsync(ticket.IdTicket, new CrearDiagnosticoRequest(
            ResultadoEntradaConocimiento.DiagnosticoDescartado,
            "Primera hipótesis"));
        await service.CrearDiagnosticoAsync(ticket.IdTicket, new CrearDiagnosticoRequest(
            ResultadoEntradaConocimiento.DiagnosticoConfirmado,
            "Segunda hipótesis"));

        Assert.Equal(2, repository.Entradas.Count);
        Assert.Equal("Segunda hipótesis", ticket.CausaRaiz);
        Assert.Same(ticket, repository.TicketUltimaPersistencia);
    }

    [Fact]
    public async Task Diagnostico_no_confirmado_no_reemplaza_resumen_heredado()
    {
        var ticket = CrearTicket();
        ticket.SincronizarResumenConocimiento(
            TipoEntradaConocimiento.Diagnostico,
            "Causa heredada");
        var service = CrearService(ticket, new ConocimientoRepositoryFake(), 2, Rol.Desarrollador);

        await service.CrearDiagnosticoAsync(ticket.IdTicket, new CrearDiagnosticoRequest(
            ResultadoEntradaConocimiento.DiagnosticoInconcluso,
            "Hipótesis todavía inconclusa"));

        Assert.Equal("Causa heredada", ticket.CausaRaiz);
    }

    [Fact]
    public async Task Al_descartar_diagnostico_vigente_recupera_el_confirmado_anterior()
    {
        var ticket = CrearTicket();
        var repository = new ConocimientoRepositoryFake();
        var service = CrearService(ticket, repository, 2, Rol.Desarrollador);
        await service.CrearDiagnosticoAsync(ticket.IdTicket, new CrearDiagnosticoRequest(
            ResultadoEntradaConocimiento.DiagnosticoConfirmado,
            "Causa anterior"));
        var idVigente = await service.CrearDiagnosticoAsync(
            ticket.IdTicket,
            new CrearDiagnosticoRequest(
                ResultadoEntradaConocimiento.DiagnosticoConfirmado,
                "Causa más reciente"));

        await service.ActualizarEntradaAsync(
            ticket.IdTicket,
            idVigente,
            new ActualizarEntradaConocimientoRequest(
                ResultadoEntradaConocimiento.DiagnosticoDescartado,
                "Hipótesis descartada"));

        Assert.Equal("Causa anterior", ticket.CausaRaiz);
        Assert.Same(ticket, repository.TicketUltimaPersistencia);
    }

    [Fact]
    public async Task Al_descartar_el_unico_diagnostico_confirmado_limpia_el_resumen()
    {
        var ticket = CrearTicket();
        var repository = new ConocimientoRepositoryFake();
        var service = CrearService(ticket, repository, 2, Rol.Desarrollador);
        var idDiagnostico = await service.CrearDiagnosticoAsync(
            ticket.IdTicket,
            new CrearDiagnosticoRequest(
                ResultadoEntradaConocimiento.DiagnosticoConfirmado,
                "Única causa confirmada"));

        await service.ActualizarEntradaAsync(
            ticket.IdTicket,
            idDiagnostico,
            new ActualizarEntradaConocimientoRequest(
                ResultadoEntradaConocimiento.DiagnosticoDescartado,
                "Hipótesis descartada"));

        Assert.Null(ticket.CausaRaiz);
    }

    [Fact]
    public async Task Solucion_fallida_no_reemplaza_ultima_solucion_viable()
    {
        var ticket = CrearTicket();
        var repository = new ConocimientoRepositoryFake();
        var service = CrearService(ticket, repository, 2, Rol.Desarrollador);
        await service.CrearSolucionAsync(ticket.IdTicket, new CrearSolucionRequest(
            ResultadoEntradaConocimiento.SolucionParcial,
            "Mitigación viable"));

        await service.CrearSolucionAsync(ticket.IdTicket, new CrearSolucionRequest(
            ResultadoEntradaConocimiento.SolucionFallida,
            "Intento fallido"));

        Assert.Equal("Mitigación viable", ticket.SolucionPropuesta);
    }

    [Fact]
    public async Task Desarrollador_no_asignado_no_puede_crear_diagnostico()
    {
        var ticket = CrearTicket();
        var service = CrearService(
            ticket,
            new ConocimientoRepositoryFake(),
            99,
            Rol.Desarrollador);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CrearDiagnosticoAsync(ticket.IdTicket, new CrearDiagnosticoRequest(
                ResultadoEntradaConocimiento.DiagnosticoConfirmado,
                "Hipótesis")));
    }

    [Fact]
    public async Task Desarrollador_no_puede_crear_solucion_despues_de_entregar()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.Entregado;
        var service = CrearService(
            ticket,
            new ConocimientoRepositoryFake(),
            2,
            Rol.Desarrollador);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CrearSolucionAsync(ticket.IdTicket, new CrearSolucionRequest(
                ResultadoEntradaConocimiento.SolucionExitosa,
                "Solución fuera del estado permitido")));
    }

    [Fact]
    public void Tags_se_normalizan_sin_distinguir_mayusculas()
    {
        var primero = new TagConocimiento(" Autenticación ");
        var segundo = new TagConocimiento("autenticación");

        Assert.Equal("Autenticación", primero.Nombre);
        Assert.Equal(primero.NombreNormalizado, segundo.NombreNormalizado);
    }

    [Fact]
    public async Task Qa_puede_validar_sin_ser_usuario_asignado()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.EnRevisionQA;
        var repository = new ConocimientoRepositoryFake();
        var service = CrearService(ticket, repository, 50, Rol.QA);

        await service.CrearValidacionQaAsync(ticket.IdTicket, new CrearValidacionQaRequest(
            ResultadoEntradaConocimiento.ValidacionRechazada,
            "Persiste el síntoma reportado",
            IdAmbiente: 4));

        var validacion = Assert.Single(repository.Entradas);
        Assert.Equal(TipoEntradaConocimiento.ValidacionQa, validacion.IdTipoEntrada);
        Assert.Equal(50, validacion.IdUsuarioAutor);
    }

    [Fact]
    public async Task Qa_no_puede_editar_validacion_de_otro_qa()
    {
        var ticket = CrearTicket();
        ticket.IdEstado = TicketEstado.EnRevisionQA;
        var repository = new ConocimientoRepositoryFake();
        repository.Entradas.Add(new EntradaConocimientoTicket(
            ticket.IdTicket,
            TipoEntradaConocimiento.ValidacionQa,
            ResultadoEntradaConocimiento.ValidacionConObservaciones,
            "Validación original",
            null,
            null,
            null,
            4,
            null,
            null,
            51,
            Rol.QA));
        var service = CrearService(ticket, repository, 50, Rol.QA);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ActualizarEntradaAsync(
                ticket.IdTicket,
                repository.Entradas.Single().IdEntrada,
                new ActualizarEntradaConocimientoRequest(
                    ResultadoEntradaConocimiento.ValidacionAprobada,
                    "Aprobada")));
    }

    private static EntradaConocimientoTicket CrearDiagnostico(string resumen) => new(
        Guid.NewGuid(),
        TipoEntradaConocimiento.Diagnostico,
        ResultadoEntradaConocimiento.DiagnosticoConfirmado,
        resumen,
        null,
        null,
        null,
        null,
        null,
        null,
        2,
        Rol.Desarrollador);

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Ticket de prueba",
        "Descripción suficientemente larga",
        2,
        1,
        TicketTipo.Incidente,
        TicketPrioridad.Media,
        TicketImpacto.Medio,
        TicketOrigen.SAIA);

    private static ConocimientoTicketService CrearService(
        Ticket ticket,
        ConocimientoRepositoryFake conocimiento,
        long idUsuario,
        Rol rol) => new(
            conocimiento,
            new TicketRepositoryFake(ticket),
            new AplicativoRepositoryFake(),
            new UsuarioActualFake(idUsuario, rol));

    private sealed class UsuarioActualFake(long idUsuario, Rol rol) : IUsuarioActual
    {
        public long IdUsuario { get; } = idUsuario;
        public Rol Rol { get; } = rol;
    }

    private sealed class TicketRepositoryFake(Ticket ticket) : ITicketRepository
    {
        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult<Ticket?>(id == ticket.IdTicket ? ticket : null);
        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));
        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(
            long idUsuario,
            TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));
        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(long idUsuario, TicketFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<Ticket>([], 1, 20, 0));
        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>([]);
        public Task GuardarAsync(Ticket ticketGuardado) => Task.CompletedTask;
        public Task ActualizarAsync(Ticket ticketActualizado) => Task.CompletedTask;
        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;
    }

    private sealed class ConocimientoRepositoryFake : IConocimientoTicketRepository
    {
        public List<EntradaConocimientoTicket> Entradas { get; } = [];
        public Ticket? TicketUltimaPersistencia { get; private set; }

        public Task<IReadOnlyCollection<EntradaConocimientoTicket>> ObtenerEntradasTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<EntradaConocimientoTicket>>(
                Entradas.Where(item => item.IdTicket == idTicket).ToArray());
        public Task<EntradaConocimientoTicket?> ObtenerEntradaAsync(Guid idEntrada) =>
            Task.FromResult(Entradas.SingleOrDefault(item => item.IdEntrada == idEntrada));
        public Task<IReadOnlyCollection<RevisionEntradaConocimiento>> ObtenerRevisionesAsync(Guid idEntrada) =>
            Task.FromResult<IReadOnlyCollection<RevisionEntradaConocimiento>>(
                Entradas.Single(item => item.IdEntrada == idEntrada).Revisiones.ToArray());
        public Task<PaginaResultado<EntradaConocimientoTicket>> BuscarAsync(ConocimientoFiltroRequest filtro) =>
            Task.FromResult(new PaginaResultado<EntradaConocimientoTicket>(Entradas, 1, 20, Entradas.Count));
        public Task<IReadOnlyCollection<TagConocimiento>> ObtenerTagsTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<TagConocimiento>>([]);
        public Task<bool> ExisteResultadoActivoAsync(TipoEntradaConocimiento tipo, int idResultado) =>
            Task.FromResult(ResultadoEntradaConocimiento.PerteneceA(tipo, idResultado));
        public Task<bool> ExisteAmbienteActivoAsync(int idAmbiente) =>
            Task.FromResult(idAmbiente is >= 1 and <= 5);
        public Task GuardarEntradaAsync(Ticket ticket, EntradaConocimientoTicket entrada, IReadOnlyCollection<string>? tags, IReadOnlyCollection<Guid>? idsAplicativos)
        {
            TicketUltimaPersistencia = ticket;
            Entradas.Add(entrada);
            return Task.CompletedTask;
        }
        public Task ActualizarEntradaAsync(Ticket ticket, EntradaConocimientoTicket entrada, IReadOnlyCollection<string>? tags, IReadOnlyCollection<Guid>? idsAplicativos)
        {
            TicketUltimaPersistencia = ticket;
            return Task.CompletedTask;
        }
    }

    private sealed class AplicativoRepositoryFake : IAplicativoRepository
    {
        public Task<IReadOnlyCollection<Aplicativo>> ObtenerAplicativosAsync(bool incluirInactivos) =>
            Task.FromResult<IReadOnlyCollection<Aplicativo>>([]);
        public Task<Aplicativo?> ObtenerAplicativoAsync(Guid idAplicativo) => Task.FromResult<Aplicativo?>(null);
        public Task<Aplicativo?> ObtenerAplicativoPorNombreAsync(string nombre) => Task.FromResult<Aplicativo?>(null);
        public Task<IReadOnlyCollection<AplicativoTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket) =>
            Task.FromResult<IReadOnlyCollection<AplicativoTicket>>([]);
        public Task<IReadOnlyCollection<RepositorioAplicativo>> ObtenerRelacionesRepositorioAsync(Guid idAplicativo) =>
            Task.FromResult<IReadOnlyCollection<RepositorioAplicativo>>([]);
        public Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAplicativoAsync(Guid idAplicativo) =>
            Task.FromResult<IReadOnlyCollection<Repositorio>>([]);
        public Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idAplicativo) => Task.FromResult(false);
        public Task<bool> ExisteRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio) => Task.FromResult(false);
        public Task<bool> TieneRamasTicketSinRespaldoAsync(Guid idTicket, Guid idAplicativo) => Task.FromResult(false);
        public Task<bool> TieneTicketsActivosDependientesAsync(Guid idAplicativo, Guid idRepositorio) => Task.FromResult(false);
        public Task GuardarAplicativoAsync(
            Aplicativo aplicativo,
            IReadOnlyCollection<RepositorioAplicativo> relaciones) => Task.CompletedTask;
        public Task GuardarRelacionRepositorioAsync(RepositorioAplicativo relacion) => Task.CompletedTask;
        public Task EliminarRelacionRepositorioAsync(Guid idAplicativo, Guid idRepositorio) => Task.CompletedTask;
        public Task GuardarAsignacionAsync(AplicativoTicket asignacion) => Task.CompletedTask;
        public Task EliminarAsignacionAsync(Guid idTicket, Guid idAplicativo) => Task.CompletedTask;
    }
}
