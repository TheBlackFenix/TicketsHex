using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Mappers;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.TicketCasosUso
{
    public class TicketQuery : ITicketQuery
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUsuarioActual _usuarioActual;

        public TicketQuery(ITicketRepository ticketRepository, IUsuarioActual usuarioActual)
        {
            _ticketRepository = ticketRepository;
            _usuarioActual = usuarioActual;
        }

        public async Task<PaginaResultado<TicketDTO>> ObtenerListaTicketsAsync(TicketFiltroRequest filtro)
        {
            var filtroNormalizado = filtro.Normalizar();
            if (_usuarioActual.Rol == Rol.QA)
            {
                filtroNormalizado = filtroNormalizado with { IncluirEliminados = false };
                var paginaQa = await _ticketRepository.ObtenerPaginaParaQaAsync(filtroNormalizado);
                return MapearPagina(paginaQa);
            }

            if (!PuedeConsultarTodosLosTickets())
                throw new UnauthorizedAccessException("Solo QA, Planner o Lider Tecnico pueden consultar el listado general.");

            if (_usuarioActual.Rol == Rol.LiderTecnico)
                filtroNormalizado = filtroNormalizado with { IncluirEliminados = false };

            return await ObtenerPaginaAsync(filtroNormalizado);
        }

        public async Task<PaginaResultado<TicketDTO>> ObtenerMisTicketsAsync(TicketFiltroRequest filtro)
        {
            var filtroUsuario = filtro.Normalizar() with
            {
                IdUsuarioAsignado = _usuarioActual.IdUsuario,
                IncluirEliminados = false
            };

            return await ObtenerPaginaAsync(filtroUsuario);
        }

        public async Task<PaginaResultado<TicketDTO>> ObtenerHistoricoMisTicketsAsync(
            TicketFiltroRequest filtro)
        {
            var filtroNormalizado = filtro.Normalizar() with
            {
                IdUsuarioAsignado = null,
                IncluirEliminados = false
            };
            var pagina = await _ticketRepository.ObtenerPaginaPorAsignacionHistoricaAsync(
                _usuarioActual.IdUsuario,
                filtroNormalizado);

            return MapearPagina(pagina);
        }

        public async Task<TicketDTO> ObtenerTicketPorIdAsync(Guid id)
        {
            var puedeConsultarTodos = PuedeConsultarTodosLosTickets();
            var puedeConsultarEliminados = _usuarioActual.Rol == Rol.Planner;
            var ticket = await _ticketRepository.ObtenerPorIdAsync(id, puedeConsultarEliminados)
                ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");

            if (!puedeConsultarTodos && !ticket.PuedeConsultar(_usuarioActual.IdUsuario, _usuarioActual.Rol))
                throw new UnauthorizedAccessException("No tiene acceso a este ticket.");

            return ticket.ToDto(_usuarioActual.IdUsuario, _usuarioActual.Rol);
        }

        private async Task<PaginaResultado<TicketDTO>> ObtenerPaginaAsync(TicketFiltroRequest filtro)
        {
            var pagina = await _ticketRepository.ObtenerPaginaAsync(filtro);
            return MapearPagina(pagina);
        }

        private PaginaResultado<TicketDTO> MapearPagina(
            PaginaResultado<TicketsHex.Domain.Entidades.Ticket.Ticket> pagina)
        {
            return new PaginaResultado<TicketDTO>(
                pagina.Elementos
                    .Select(ticket => ticket.ToDto(_usuarioActual.IdUsuario, _usuarioActual.Rol))
                    .ToArray(),
                pagina.Pagina,
                pagina.TamanoPagina,
                pagina.TotalElementos);
        }

        private bool PuedeConsultarTodosLosTickets() =>
            _usuarioActual.Rol is Rol.Planner or Rol.LiderTecnico;
    }
}
