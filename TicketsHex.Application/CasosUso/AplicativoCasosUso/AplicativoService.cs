using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Aplicativo;
using TicketsHex.Application.Puertos.Entrada.Aplicativo;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Aplicativos;

namespace TicketsHex.Application.CasosUso.AplicativoCasosUso
{
    public sealed class AplicativoService : IAplicativoService
    {
        private readonly IAplicativoRepository _repository;
        private readonly ITicketRepository _ticketRepository;

        public AplicativoService(
            IAplicativoRepository repository,
            ITicketRepository ticketRepository)
        {
            _repository = repository;
            _ticketRepository = ticketRepository;
        }

        public async Task<IReadOnlyCollection<AplicativoDTO>> ObtenerAplicativosAsync(bool incluirInactivos)
        {
            var aplicativos = await _repository.ObtenerAplicativosAsync(incluirInactivos);
            return aplicativos.Select(Mapear).ToArray();
        }

        public async Task<IReadOnlyCollection<AplicativoTicketDTO>> ObtenerAplicativosTicketAsync(Guid idTicket)
        {
            _ = await ObtenerTicketAsync(idTicket);
            var asignaciones = await _repository.ObtenerAsignacionesTicketAsync(idTicket);
            var resultado = new List<AplicativoTicketDTO>(asignaciones.Count);

            foreach (var asignacion in asignaciones)
            {
                var aplicativo = await _repository.ObtenerAplicativoAsync(asignacion.IdAplicativo)
                    ?? throw new RecursoNoEncontradoException("El aplicativo asignado no existe.");
                resultado.Add(new AplicativoTicketDTO(
                    asignacion.IdAplicativoTicket,
                    asignacion.IdTicket,
                    aplicativo.IdAplicativo,
                    aplicativo.Nombre,
                    asignacion.FechaAsignacion));
            }

            return resultado;
        }

        public async Task<Guid> CrearAplicativoAsync(CrearAplicativoRequest request)
        {
            if (await _repository.ObtenerAplicativoPorNombreAsync(request.Nombre) is not null)
                throw new ConflictoException($"Ya existe el aplicativo '{request.Nombre}'.");

            var aplicativo = new Aplicativo(request.Nombre, request.Descripcion);
            await _repository.GuardarAplicativoAsync(aplicativo);
            return aplicativo.IdAplicativo;
        }

        public async Task<Guid> AsignarAplicativoAsync(Guid idTicket, AsignarAplicativoTicketRequest request)
        {
            _ = await ObtenerTicketAsync(idTicket);
            _ = await _repository.ObtenerAplicativoAsync(request.IdAplicativo)
                ?? throw new RecursoNoEncontradoException("Aplicativo no encontrado.");

            if (await _repository.ExisteAsignacionAsync(idTicket, request.IdAplicativo))
                throw new ConflictoException("El aplicativo ya está asociado al ticket.");

            var asignacion = new AplicativoTicket(idTicket, request.IdAplicativo);
            await _repository.GuardarAsignacionAsync(asignacion);
            return asignacion.IdAplicativoTicket;
        }

        public async Task DesasignarAplicativoAsync(Guid idTicket, Guid idAplicativo)
        {
            if (!await _repository.ExisteAsignacionAsync(idTicket, idAplicativo))
                throw new RecursoNoEncontradoException("El aplicativo no está asociado al ticket.");

            await _repository.EliminarAsignacionAsync(idTicket, idAplicativo);
        }

        private async Task<Domain.Entidades.Ticket.Ticket> ObtenerTicketAsync(Guid idTicket) =>
            await _ticketRepository.ObtenerPorIdAsync(idTicket)
            ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");

        private static AplicativoDTO Mapear(Aplicativo aplicativo) => new(
            aplicativo.IdAplicativo,
            aplicativo.Nombre,
            aplicativo.Descripcion,
            aplicativo.Activo);
    }
}
