using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Conocimiento;
using TicketsHex.Application.Puertos.Entrada.Conocimiento;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Conocimiento;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.ConocimientoCasosUso
{
    public sealed class ConocimientoTicketService : IConocimientoTicketService
    {
        private static readonly TicketEstado[] EstadosDesarrollador =
        [
            TicketEstado.EnAnalisis,
            TicketEstado.EnProceso,
            TicketEstado.Bloqueado,
            TicketEstado.BUG,
            TicketEstado.Rollback
        ];

        private static readonly TicketEstado[] EstadosValidacionQa =
        [
            TicketEstado.DespliegueApitesting,
            TicketEstado.EnReplicaQA,
            TicketEstado.EnRevisionApitesting,
            TicketEstado.DespligueQA,
            TicketEstado.EnRevisionQA,
            TicketEstado.BUG
        ];

        private readonly IConocimientoTicketRepository _repository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IAplicativoRepository _aplicativoRepository;
        private readonly IUsuarioActual _usuarioActual;

        public ConocimientoTicketService(
            IConocimientoTicketRepository repository,
            ITicketRepository ticketRepository,
            IAplicativoRepository aplicativoRepository,
            IUsuarioActual usuarioActual)
        {
            _repository = repository;
            _ticketRepository = ticketRepository;
            _aplicativoRepository = aplicativoRepository;
            _usuarioActual = usuarioActual;
        }

        public async Task<BaseConocimientoTicketDTO> ObtenerBaseAsync(Guid idTicket)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            ValidarPuedeConsultar(ticket);
            var entradas = await _repository.ObtenerEntradasTicketAsync(idTicket);
            var tags = await _repository.ObtenerTagsTicketAsync(idTicket);
            var aplicativos = await _aplicativoRepository.ObtenerAsignacionesTicketAsync(idTicket);

            return new BaseConocimientoTicketDTO(
                idTicket,
                tags.Select(item => item.Nombre).OrderBy(item => item).ToArray(),
                aplicativos.Select(item => item.IdAplicativo).ToArray(),
                entradas.OrderByDescending(item => item.FechaCreacion).Select(Mapear).ToArray());
        }

        public async Task<PaginaResultado<EntradaConocimientoDTO>> BuscarAsync(
            ConocimientoFiltroRequest filtro)
        {
            var pagina = await _repository.BuscarAsync(filtro.Normalizar());
            return new PaginaResultado<EntradaConocimientoDTO>(
                pagina.Elementos.Select(Mapear).ToArray(),
                pagina.Pagina,
                pagina.TamanoPagina,
                pagina.TotalElementos);
        }

        public async Task<IReadOnlyCollection<RevisionEntradaConocimientoDTO>> ObtenerRevisionesAsync(
            Guid idTicket,
            Guid idEntrada)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            ValidarPuedeConsultar(ticket);
            var entrada = await ObtenerEntradaAsync(idTicket, idEntrada);
            var revisiones = await _repository.ObtenerRevisionesAsync(entrada.IdEntrada);
            return revisiones
                .OrderByDescending(item => item.FechaRevision)
                .Select(item => new RevisionEntradaConocimientoDTO(
                    item.IdRevision,
                    item.IdEntrada,
                    item.ContenidoAnterior,
                    item.IdUsuarioAccion,
                    item.IdRolUsuarioAccion,
                    item.IdEstadoTicket,
                    item.FechaRevision))
                .ToArray();
        }

        public Task<Guid> CrearDiagnosticoAsync(Guid idTicket, CrearDiagnosticoRequest request) =>
            CrearEntradaAsync(
                idTicket,
                TipoEntradaConocimiento.Diagnostico,
                request.IdResultado,
                request.Resumen,
                request.Sintomas,
                request.Comprobaciones,
                request.PasosReproduccion,
                request.IdAmbiente,
                null,
                null,
                request.Referencias,
                request.Tags,
                request.IdsAplicativos);

        public Task<Guid> CrearSolucionAsync(Guid idTicket, CrearSolucionRequest request) =>
            CrearEntradaAsync(
                idTicket,
                TipoEntradaConocimiento.Solucion,
                request.IdResultado,
                request.Resumen,
                null,
                null,
                null,
                null,
                request.RequiereDespliegue,
                request.Observaciones,
                request.Referencias,
                null,
                null);

        public Task<Guid> CrearValidacionQaAsync(Guid idTicket, CrearValidacionQaRequest request) =>
            CrearEntradaAsync(
                idTicket,
                TipoEntradaConocimiento.ValidacionQa,
                request.IdResultado,
                request.Resumen,
                request.Sintomas,
                request.Comprobaciones,
                null,
                request.IdAmbiente,
                null,
                request.Observaciones,
                request.Referencias,
                null,
                null);

        public async Task ActualizarEntradaAsync(
            Guid idTicket,
            Guid idEntrada,
            ActualizarEntradaConocimientoRequest request)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            var entrada = await ObtenerEntradaAsync(idTicket, idEntrada);
            ValidarPuedeEscribir(ticket, entrada.IdTipoEntrada, entrada.IdUsuarioAutor);
            await ValidarParametrosAsync(entrada.IdTipoEntrada, request.IdResultado, request.IdAmbiente);

            IReadOnlyCollection<string>? tags = null;
            IReadOnlyCollection<Guid>? aplicativos = null;
            if (entrada.IdTipoEntrada == TipoEntradaConocimiento.Diagnostico)
            {
                tags = request.Tags;
                aplicativos = request.IdsAplicativos;
                await ValidarAplicativosAsync(aplicativos);
            }

            entrada.Actualizar(
                request.IdResultado,
                request.Resumen,
                entrada.IdTipoEntrada == TipoEntradaConocimiento.Solucion ? null : request.Sintomas,
                entrada.IdTipoEntrada == TipoEntradaConocimiento.Solucion ? null : request.Comprobaciones,
                entrada.IdTipoEntrada == TipoEntradaConocimiento.Diagnostico ? request.PasosReproduccion : null,
                entrada.IdTipoEntrada == TipoEntradaConocimiento.Solucion ? null : request.IdAmbiente,
                entrada.IdTipoEntrada == TipoEntradaConocimiento.Solucion ? request.RequiereDespliegue : null,
                entrada.IdTipoEntrada == TipoEntradaConocimiento.Diagnostico ? null : request.Observaciones,
                MapearReferencias(request.Referencias),
                _usuarioActual.IdUsuario,
                _usuarioActual.Rol,
                ticket.IdEstado);

            await _repository.ActualizarEntradaAsync(entrada, tags, aplicativos);
        }

        private async Task<Guid> CrearEntradaAsync(
            Guid idTicket,
            TipoEntradaConocimiento tipo,
            int idResultado,
            string resumen,
            string? sintomas,
            string? comprobaciones,
            string? pasosReproduccion,
            int? idAmbiente,
            bool? requiereDespliegue,
            string? observaciones,
            IReadOnlyCollection<ReferenciaConocimientoRequest>? referencias,
            IReadOnlyCollection<string>? tags,
            IReadOnlyCollection<Guid>? idsAplicativos)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            ValidarPuedeEscribir(ticket, tipo, null);
            await ValidarParametrosAsync(tipo, idResultado, idAmbiente);
            await ValidarAplicativosAsync(idsAplicativos);

            var entrada = new EntradaConocimientoTicket(
                idTicket,
                tipo,
                idResultado,
                resumen,
                sintomas,
                comprobaciones,
                pasosReproduccion,
                idAmbiente,
                requiereDespliegue,
                observaciones,
                _usuarioActual.IdUsuario,
                _usuarioActual.Rol,
                MapearReferencias(referencias));

            await _repository.GuardarEntradaAsync(entrada, tags, idsAplicativos);
            return entrada.IdEntrada;
        }

        private void ValidarPuedeEscribir(
            Ticket ticket,
            TipoEntradaConocimiento tipo,
            long? idAutorEntrada)
        {
            if (tipo is TipoEntradaConocimiento.Diagnostico or TipoEntradaConocimiento.Solucion)
            {
                if (!ticket.PuedeEditarDatosDeDesarrollo(_usuarioActual.IdUsuario, _usuarioActual.Rol))
                {
                    throw new UnauthorizedAccessException(
                        "Solo el desarrollador asignado, Planner o Líder Técnico pueden registrar diagnóstico o solución.");
                }
                if (!EstadosDesarrollador.Contains(ticket.IdEstado))
                    throw new InvalidOperationException(
                        $"No se puede registrar conocimiento técnico en el estado {ticket.IdEstado}.");
                return;
            }

            if (_usuarioActual.Rol != Rol.QA)
                throw new UnauthorizedAccessException("Solo QA puede registrar validaciones QA.");
            if (idAutorEntrada.HasValue && idAutorEntrada.Value != _usuarioActual.IdUsuario)
                throw new UnauthorizedAccessException("QA solo puede editar sus propias validaciones.");
            if (!EstadosValidacionQa.Contains(ticket.IdEstado))
                throw new InvalidOperationException(
                    $"No se puede registrar una validación QA en el estado {ticket.IdEstado}.");
        }

        private async Task ValidarParametrosAsync(
            TipoEntradaConocimiento tipo,
            int idResultado,
            int? idAmbiente)
        {
            if (!await _repository.ExisteResultadoActivoAsync(tipo, idResultado))
                throw new RecursoNoEncontradoException(
                    "El resultado no existe, está inactivo o no corresponde al tipo de entrada.");
            if (idAmbiente.HasValue &&
                !await _repository.ExisteAmbienteActivoAsync(idAmbiente.Value))
            {
                throw new RecursoNoEncontradoException("El ambiente no existe o está inactivo.");
            }
        }

        private async Task ValidarAplicativosAsync(IReadOnlyCollection<Guid>? idsAplicativos)
        {
            if (idsAplicativos is null)
                return;

            foreach (var idAplicativo in idsAplicativos.Distinct())
            {
                _ = await _aplicativoRepository.ObtenerAplicativoAsync(idAplicativo)
                    ?? throw new RecursoNoEncontradoException(
                        $"El aplicativo {idAplicativo} no existe o está inactivo.");
            }
        }

        private async Task<Ticket> ObtenerTicketAsync(Guid idTicket) =>
            await _ticketRepository.ObtenerPorIdAsync(idTicket)
            ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");

        private void ValidarPuedeConsultar(Ticket ticket)
        {
            if (!ticket.PuedeConsultar(_usuarioActual.IdUsuario, _usuarioActual.Rol))
                throw new UnauthorizedAccessException("No tiene acceso al conocimiento de este ticket.");
        }

        private async Task<EntradaConocimientoTicket> ObtenerEntradaAsync(
            Guid idTicket,
            Guid idEntrada)
        {
            var entrada = await _repository.ObtenerEntradaAsync(idEntrada)
                ?? throw new RecursoNoEncontradoException("Entrada de conocimiento no encontrada.");
            if (entrada.IdTicket != idTicket)
                throw new RecursoNoEncontradoException("Entrada de conocimiento no encontrada.");
            return entrada;
        }

        private static IEnumerable<(TipoReferenciaConocimiento Tipo, string Url, string? Descripcion)>?
            MapearReferencias(IReadOnlyCollection<ReferenciaConocimientoRequest>? referencias) =>
            referencias?.Select(item => (item.Tipo, item.Url, item.Descripcion));

        private static EntradaConocimientoDTO Mapear(EntradaConocimientoTicket entrada) => new(
            entrada.IdEntrada,
            entrada.IdTicket,
            entrada.IdTipoEntrada,
            entrada.IdResultado,
            entrada.Resumen,
            entrada.Sintomas,
            entrada.Comprobaciones,
            entrada.PasosReproduccion,
            entrada.IdAmbiente,
            entrada.RequiereDespliegue,
            entrada.Observaciones,
            entrada.IdUsuarioAutor,
            entrada.IdRolAutor,
            entrada.FechaCreacion,
            entrada.FechaUltimaActualizacion,
            entrada.Referencias.Select(item => new ReferenciaConocimientoDTO(
                item.IdReferencia,
                item.TipoReferencia,
                item.Url,
                item.Descripcion)).ToArray());
    }
}
