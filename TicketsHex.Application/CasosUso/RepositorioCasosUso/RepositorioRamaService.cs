using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Repositorio;
using TicketsHex.Application.Puertos.Entrada.Repositorio;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.CasosUso.RepositorioCasosUso
{
    public sealed class RepositorioRamaService : IRepositorioRamaService
    {
        private readonly IRepositorioRamaRepository _repository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IUsuarioActual _usuarioActual;

        public RepositorioRamaService(
            IRepositorioRamaRepository repository,
            ITicketRepository ticketRepository,
            IUsuarioActual usuarioActual)
        {
            _repository = repository;
            _ticketRepository = ticketRepository;
            _usuarioActual = usuarioActual;
        }

        public async Task<IReadOnlyCollection<RepositorioDTO>> ObtenerRepositoriosAsync()
        {
            var repositorios = await _repository.ObtenerRepositoriosAsync();
            return repositorios
                .Select(item => new RepositorioDTO(
                    item.IdRepositorio,
                    item.Nombre,
                    item.Link,
                    item.Descripcion))
                .ToArray();
        }

        public async Task<IReadOnlyCollection<RamaDTO>> ObtenerRamasAsync(Guid idRepositorio)
        {
            var repositorio = await ObtenerRepositorioAsync(idRepositorio);
            return repositorio.Ramas
                .OrderBy(item => item.NombreRama)
                .Select(MapearRama)
                .ToArray();
        }

        public async Task<IReadOnlyCollection<RamaTicketDTO>> ObtenerRamasTicketAsync(Guid idTicket)
        {
            var ticket = await _ticketRepository.ObtenerPorIdAsync(idTicket)
                ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");

            if (_usuarioActual.Rol is not Rol.Planner and not Rol.LiderTecnico &&
                ticket.IdUsuarioAsignado != _usuarioActual.IdUsuario)
            {
                throw new UnauthorizedAccessException("No tiene acceso a las ramas de este ticket.");
            }

            var asignaciones = await _repository.ObtenerAsignacionesTicketAsync(idTicket);
            var resultado = new List<RamaTicketDTO>(asignaciones.Count);

            foreach (var asignacion in asignaciones)
            {
                var rama = await _repository.ObtenerRamaAsync(asignacion.IdRama)
                    ?? throw new RecursoNoEncontradoException("La rama asignada no existe.");
                var repositorio = await ObtenerRepositorioAsync(rama.IdRepositorio);
                resultado.Add(new RamaTicketDTO(
                    asignacion.IdRamaTicket,
                    asignacion.IdTicket,
                    repositorio.IdRepositorio,
                    repositorio.Nombre,
                    rama.IdRama,
                    rama.NombreRama,
                    asignacion.FechaAsignacion));
            }

            return resultado;
        }

        public async Task<Guid> CrearRepositorioAsync(CrearRepositorioRequest request)
        {
            ValidarPlannerOLiderTecnico();
            if (await _repository.ObtenerRepositorioPorNombreAsync(request.Nombre) is not null)
                throw new ConflictoException($"Ya existe el repositorio '{request.Nombre}'.");

            var repositorio = new Repositorio(request.Nombre, request.Link, request.Descripcion);
            await _repository.GuardarRepositorioAsync(repositorio);
            return repositorio.IdRepositorio;
        }

        public async Task<Guid> CrearRamaAsync(Guid idRepositorio, CrearRamaRequest request)
        {
            ValidarPlannerOLiderTecnico();
            var repositorio = await ObtenerRepositorioAsync(idRepositorio);
            if (await _repository.ObtenerRamaPorNombreAsync(idRepositorio, request.Nombre) is not null)
                throw new ConflictoException(
                    $"La rama '{request.Nombre}' ya existe en el repositorio.");

            var rama = repositorio.CrearRama(request.Nombre);
            await _repository.GuardarRamaAsync(rama);
            return rama.IdRama;
        }

        public async Task<Guid> AsignarRamaAsync(
            Guid idTicket,
            AsignarRamaTicketRequest request)
        {
            ValidarPlannerOLiderTecnico();
            var ticket = await _ticketRepository.ObtenerPorIdAsync(idTicket)
                ?? throw new RecursoNoEncontradoException("Ticket no encontrado.");
            if (!ticket.EsDesarrollo)
                throw new InvalidOperationException("Solo se pueden asociar ramas a tickets de desarrollo.");
            _ = await ObtenerRepositorioAsync(request.IdRepositorio);
            var rama = await _repository.ObtenerRamaAsync(request.IdRama)
                ?? throw new RecursoNoEncontradoException("Rama no encontrada.");

            if (rama.IdRepositorio != request.IdRepositorio)
                throw new InvalidOperationException(
                    "La rama no pertenece al repositorio indicado.");
            if (await _repository.ExisteAsignacionAsync(idTicket, request.IdRama))
                throw new ConflictoException("La rama ya está asignada al ticket.");

            var asignacion = new RamaTicket(idTicket, request.IdRama);
            await _repository.GuardarAsignacionAsync(asignacion);
            return asignacion.IdRamaTicket;
        }

        public async Task DesasignarRamaAsync(Guid idTicket, Guid idRama)
        {
            ValidarPlannerOLiderTecnico();
            if (!await _repository.ExisteAsignacionAsync(idTicket, idRama))
                throw new RecursoNoEncontradoException(
                    "La rama no está asignada al ticket.");

            await _repository.EliminarAsignacionAsync(idTicket, idRama);
        }

        private async Task<Repositorio> ObtenerRepositorioAsync(Guid idRepositorio)
        {
            return await _repository.ObtenerRepositorioAsync(idRepositorio)
                ?? throw new RecursoNoEncontradoException("Repositorio no encontrado.");
        }

        private void ValidarPlannerOLiderTecnico()
        {
            if (_usuarioActual.Rol is not Rol.LiderTecnico and not Rol.Planner)
                throw new UnauthorizedAccessException(
                    "Solo Planner o Lider Tecnico pueden administrar repositorios y ramas.");
        }

        private static RamaDTO MapearRama(Rama rama) => new(
            rama.IdRama,
            rama.IdRepositorio,
            rama.NombreRama,
            rama.FechaCreacion);
    }
}
