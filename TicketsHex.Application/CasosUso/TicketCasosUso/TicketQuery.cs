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
            if (_usuarioActual.Rol != Rol.Planner)
                throw new UnauthorizedAccessException("Solo el Planner puede consultar el listado general.");

            return await ObtenerPaginaAsync(filtro.Normalizar());
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

        public async Task<TicketDTO> ObtenerTicketPorIdAsync(Guid id)
        {
            var esPlanner = _usuarioActual.Rol == Rol.Planner;
            var ticket = await _ticketRepository.ObtenerPorIdAsync(id, esPlanner)
                ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");

            if (!esPlanner && ticket.IdUsuarioAsignado != _usuarioActual.IdUsuario)
                throw new UnauthorizedAccessException("No tiene acceso a este ticket.");

            return ticket.ToDto();
        }

        private async Task<PaginaResultado<TicketDTO>> ObtenerPaginaAsync(TicketFiltroRequest filtro)
        {
            var pagina = await _ticketRepository.ObtenerPaginaAsync(filtro);
            return new PaginaResultado<TicketDTO>(
                pagina.Elementos.Select(ticket => ticket.ToDto()).ToArray(),
                pagina.Pagina,
                pagina.TamanoPagina,
                pagina.TotalElementos);
        }
    }
}
