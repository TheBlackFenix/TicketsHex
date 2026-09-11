using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Aplicativo;
using TicketsHex.Application.DTO_s.Repositorio;
using TicketsHex.Application.Puertos.Entrada.Aplicativo;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Enums;

using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.Application.CasosUso.AplicativoCasosUso
{
    public sealed class AplicativoService : IAplicativoService
    {
        private readonly IAplicativoRepository _repository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IRepositorioRamaRepository _repositorioRepository;
        private readonly IUsuarioActual _usuarioActual;

        public AplicativoService(
            IAplicativoRepository repository,
            ITicketRepository ticketRepository,
            IRepositorioRamaRepository repositorioRepository,
            IUsuarioActual usuarioActual)
        {
            _repository = repository;
            _ticketRepository = ticketRepository;
            _repositorioRepository = repositorioRepository;
            _usuarioActual = usuarioActual;
        }

        public async Task<IReadOnlyCollection<AplicativoDTO>> ObtenerAplicativosAsync(bool incluirInactivos)
        {
            var aplicativos = await _repository.ObtenerAplicativosAsync(incluirInactivos);
            return aplicativos.Select(Mapear).ToArray();
        }

        public async Task<IReadOnlyCollection<AplicativoTicketDTO>> ObtenerAplicativosTicketAsync(Guid idTicket)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            if (!ticket.PuedeConsultar(_usuarioActual.IdUsuario, _usuarioActual.Rol))
                throw new UnauthorizedAccessException("No tiene acceso a los aplicativos de este ticket.");
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
            ValidarPlannerOLiderTecnico();
            if (await _repository.ObtenerAplicativoPorNombreAsync(request.Nombre) is not null)
                throw new ConflictoException(
                    $"Ya existe el aplicativo '{request.Nombre}'.",
                    CodigosError.RecursoDuplicado);

            var aplicativo = new Aplicativo(request.Nombre, request.Descripcion);
            if (request.IdsRepositorios?.Any(item => item == Guid.Empty) == true)
                throw new ArgumentException("Los identificadores de repositorio deben ser válidos.");
            var idsRepositorios = (request.IdsRepositorios ?? [])
                .Distinct()
                .ToArray();
            foreach (var idRepositorio in idsRepositorios)
                _ = await ObtenerRepositorioAsync(idRepositorio);

            var relaciones = idsRepositorios
                .Select(idRepositorio => new RepositorioAplicativo(
                    idRepositorio,
                    aplicativo.IdAplicativo))
                .ToArray();
            await _repository.GuardarAplicativoAsync(aplicativo, relaciones);
            return aplicativo.IdAplicativo;
        }

        public async Task<IReadOnlyCollection<RepositorioAplicativoDTO>> ObtenerRepositoriosAplicativoAsync(
            Guid idAplicativo)
        {
            _ = await ObtenerAplicativoAsync(idAplicativo);
            var relaciones = await _repository.ObtenerRelacionesRepositorioAsync(idAplicativo);
            var relacionesPorRepositorio = relaciones.ToDictionary(item => item.IdRepositorio);
            var repositorios = await _repository.ObtenerRepositoriosAplicativoAsync(idAplicativo);

            return repositorios.Select(repositorio => new RepositorioAplicativoDTO(
                relacionesPorRepositorio[repositorio.IdRepositorio].IdRepositorioAplicativo,
                repositorio.IdRepositorio,
                repositorio.Nombre,
                repositorio.Link,
                repositorio.Descripcion,
                repositorio.IdTipoRepositorio,
                repositorio.IdTipoRepositorio?.ToString()))
                .ToArray();
        }

        public async Task<Guid> AsignarRepositorioAsync(
            Guid idAplicativo,
            AsignarRepositorioAplicativoRequest request)
        {
            ValidarPlannerOLiderTecnico();
            _ = await ObtenerAplicativoAsync(idAplicativo);
            _ = await ObtenerRepositorioAsync(request.IdRepositorio);
            if (await _repository.ExisteRelacionRepositorioAsync(
                    idAplicativo,
                    request.IdRepositorio))
            {
                throw new ConflictoException(
                    "El repositorio ya está asociado al aplicativo.",
                    CodigosError.RecursoDuplicado);
            }

            var relacion = new RepositorioAplicativo(request.IdRepositorio, idAplicativo);
            await _repository.GuardarRelacionRepositorioAsync(relacion);
            return relacion.IdRepositorioAplicativo;
        }

        public async Task DesasignarRepositorioAsync(Guid idAplicativo, Guid idRepositorio)
        {
            ValidarPlannerOLiderTecnico();
            _ = await ObtenerAplicativoAsync(idAplicativo);
            if (!await _repository.ExisteRelacionRepositorioAsync(idAplicativo, idRepositorio))
                throw new RecursoNoEncontradoException(
                    "El repositorio no está asociado al aplicativo.");
            if (await _repository.TieneTicketsActivosDependientesAsync(
                    idAplicativo,
                    idRepositorio))
            {
                throw new ConflictoException(
                    "La relación no puede retirarse porque existen tickets activos con ramas que dependen de ella.",
                    CodigosError.RelacionRepositorioEnUso);
            }

            await _repository.EliminarRelacionRepositorioAsync(idAplicativo, idRepositorio);
        }

        public async Task<Guid> AsignarAplicativoAsync(Guid idTicket, AsignarAplicativoTicketRequest request)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            ValidarPuedeEditarTicket(ticket);
            _ = await _repository.ObtenerAplicativoAsync(request.IdAplicativo)
                ?? throw new RecursoNoEncontradoException("Aplicativo no encontrado.");

            if (await _repository.ExisteAsignacionAsync(idTicket, request.IdAplicativo))
                throw new ConflictoException(
                    "El aplicativo ya está asociado al ticket.",
                    CodigosError.RecursoDuplicado);

            var asignacion = new AplicativoTicket(idTicket, request.IdAplicativo);
            await _repository.GuardarAsignacionAsync(asignacion);
            return asignacion.IdAplicativoTicket;
        }

        public async Task DesasignarAplicativoAsync(Guid idTicket, Guid idAplicativo)
        {
            var ticket = await ObtenerTicketAsync(idTicket);
            ValidarPuedeEditarTicket(ticket);
            if (!await _repository.ExisteAsignacionAsync(idTicket, idAplicativo))
                throw new RecursoNoEncontradoException("El aplicativo no está asociado al ticket.");
            if (await _repository.TieneRamasTicketSinRespaldoAsync(idTicket, idAplicativo))
            {
                throw new ConflictoException(
                    "No se puede retirar el aplicativo porque dejaría ramas del ticket sin un aplicativo relacionado.",
                    CodigosError.AplicativoConRamasAsociadas);
            }

            await _repository.EliminarAsignacionAsync(idTicket, idAplicativo);
        }

        private async Task<Aplicativo> ObtenerAplicativoAsync(Guid idAplicativo) =>
            await _repository.ObtenerAplicativoAsync(idAplicativo)
            ?? throw new RecursoNoEncontradoException("Aplicativo no encontrado.");

        private async Task<Domain.Entidades.ConfiguracionGit.Repositorio> ObtenerRepositorioAsync(
            Guid idRepositorio) =>
            await _repositorioRepository.ObtenerRepositorioAsync(idRepositorio)
            ?? throw new RecursoNoEncontradoException("Repositorio no encontrado.");

        private async Task<Domain.Entidades.Ticket.Ticket> ObtenerTicketAsync(Guid idTicket) =>
            await _ticketRepository.ObtenerPorIdAsync(idTicket)
            ?? throw new RecursoNoEncontradoException(
                "Ticket no encontrado.",
                CodigosError.TicketNoEncontrado);

        private void ValidarPlannerOLiderTecnico()
        {
            if (_usuarioActual.Rol is not Rol.Planner and not Rol.LiderTecnico)
                throw new UnauthorizedAccessException("Solo Planner o Lider Tecnico pueden administrar aplicativos.");
        }

        private void ValidarPuedeEditarTicket(Domain.Entidades.Ticket.Ticket ticket)
        {
            if (!ticket.PuedeEditarDatosDeDesarrollo(_usuarioActual.IdUsuario, _usuarioActual.Rol))
                throw new UnauthorizedAccessException("No puede modificar los aplicativos de este ticket.");
        }

        private static AplicativoDTO Mapear(Aplicativo aplicativo) => new(
            aplicativo.IdAplicativo,
            aplicativo.Nombre,
            aplicativo.Descripcion,
            aplicativo.Activo);
    }
}
