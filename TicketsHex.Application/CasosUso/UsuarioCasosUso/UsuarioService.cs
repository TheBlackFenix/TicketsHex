using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.DTO_s.Usuario;
using TicketsHex.Application.Puertos.Entrada.Usuario;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.Servicios;
using Microsoft.Extensions.Configuration;

namespace TicketsHex.Application.CasosUso.UsuarioCasosUso
{
    public sealed class UsuarioService : IUsuarioService
    {
        private const string ContrasenaPorDefectoKey = "Usuarios:ContrasenaPorDefecto";

        private readonly IUsuarioRepository _repository;
        private readonly IUsuarioActual _usuarioActual;
        private readonly IAutenticacionRepository _autenticacionRepository;
        private readonly IContrasenaHasher _contrasenaHasher;
        private readonly IConfiguration _configuration;
        private readonly ITicketRepository _ticketRepository;

        public UsuarioService(
            IUsuarioRepository repository,
            IUsuarioActual usuarioActual,
            IAutenticacionRepository autenticacionRepository,
            IContrasenaHasher contrasenaHasher,
            IConfiguration configuration,
            ITicketRepository ticketRepository)
        {
            _repository = repository;
            _usuarioActual = usuarioActual;
            _autenticacionRepository = autenticacionRepository;
            _contrasenaHasher = contrasenaHasher;
            _configuration = configuration;
            _ticketRepository = ticketRepository;
        }

        public async Task<IReadOnlyCollection<UsuarioDTO>> ObtenerTodosAsync(bool incluirInactivos)
        {
            var usuarios = await _repository.ObtenerTodosAsync(incluirInactivos);
            return usuarios.Select(Mapear).ToArray();
        }

        public async Task<UsuarioDTO> ObtenerPorIdAsync(long idUsuario)
        {
            var usuario = await ObtenerEntidadAsync(idUsuario);
            return Mapear(usuario);
        }

        public async Task CrearAsync(CrearUsuarioRequest request)
        {
            ValidarPlannerOLiderTecnico();
            if (await _repository.ObtenerPorIdAsync(request.IdUsuario) is not null)
                throw new ConflictoException($"El usuario {request.IdUsuario} ya existe.");
            if (await _autenticacionRepository.ObtenerUsuarioPorNombreAsync(request.NombreUsuario) is not null)
                throw new ConflictoException($"El nombre de usuario {request.NombreUsuario} ya existe.");

            var contrasenaPorDefecto = ObtenerContrasenaPorDefecto();
            ValidadorContrasena.Validar(contrasenaPorDefecto);
            var usuario = new Usuario(
                request.IdUsuario,
                request.NombreUsuario,
                request.Nombres,
                request.Apellidos,
                request.Rol,
                request.IdArea,
                _contrasenaHasher.CrearHash(contrasenaPorDefecto),
                request.ImagenPerfilBase64,
                debeCambiarContrasena: true);

            await _repository.GuardarAsync(usuario);
        }

        public async Task ActualizarAsync(long idUsuario, ActualizarUsuarioRequest request)
        {
            ValidarPlannerOLiderTecnico();
            var usuario = await ObtenerEntidadAsync(idUsuario);
            var usuarioMismoNombre = await _autenticacionRepository
                .ObtenerUsuarioPorNombreAsync(request.NombreUsuario);
            if (usuarioMismoNombre is not null && usuarioMismoNombre.IdUsuario != idUsuario)
                throw new ConflictoException($"El nombre de usuario {request.NombreUsuario} ya existe.");

            if (usuario.IdRol != request.Rol ||
                usuario.IdArea != request.IdArea ||
                (usuario.Activo && !request.Activo))
            {
                await ValidarUsuarioSinCargaActivaAsync(idUsuario);
            }

            usuario.Actualizar(
                request.NombreUsuario,
                request.Nombres,
                request.Apellidos,
                request.Rol,
                request.IdArea);
            if (request.ImagenPerfilBase64 is not null)
                usuario.ActualizarImagenPerfilBase64(request.ImagenPerfilBase64);

            if (request.Activo)
                usuario.Activar();
            else
                usuario.Desactivar();

            await _autenticacionRepository.RevocarSesionesAsync(
                idUsuario,
                DateTimeOffset.UtcNow);
            await _repository.ActualizarAsync(usuario);
        }

        public async Task<UsuarioDTO> ActualizarPerfilPropioAsync(ActualizarPerfilPropioRequest request)
        {
            if (request.ImagenPerfilBase64 is null)
                throw new ArgumentException("Debe indicar la imagen de perfil.");

            var usuario = await ObtenerEntidadAsync(_usuarioActual.IdUsuario);
            usuario.ActualizarImagenPerfilBase64(request.ImagenPerfilBase64);

            await _repository.ActualizarAsync(usuario);
            return Mapear(usuario);
        }

        public async Task DesactivarAsync(long idUsuario)
        {
            ValidarPlannerOLiderTecnico();
            if (idUsuario == _usuarioActual.IdUsuario)
                throw new InvalidOperationException("El Planner no puede desactivar su propio usuario.");

            var usuario = await ObtenerEntidadAsync(idUsuario);
            await ValidarUsuarioSinCargaActivaAsync(idUsuario);
            usuario.Desactivar();
            await _autenticacionRepository.RevocarSesionesAsync(
                idUsuario,
                DateTimeOffset.UtcNow);
            await _repository.ActualizarAsync(usuario);
        }

        public async Task<int> TransferirCargaAsync(
            long idUsuario,
            TransferirCargaUsuarioRequest request)
        {
            ValidarPlannerOLiderTecnico();
            if (idUsuario == request.IdUsuarioReemplazo)
                throw new ArgumentException("El usuario de reemplazo debe ser diferente al usuario origen.");
            if (string.IsNullOrWhiteSpace(request.Comentario))
                throw new ArgumentException("Debe indicar el motivo de la transferencia.", nameof(request));

            var usuarioOrigen = await ObtenerEntidadAsync(idUsuario);
            var reemplazo = await ObtenerEntidadAsync(request.IdUsuarioReemplazo);
            if (!reemplazo.Activo)
                throw new InvalidOperationException("El usuario de reemplazo está inactivo.");
            if (reemplazo.IdRol != usuarioOrigen.IdRol || reemplazo.IdArea != usuarioOrigen.IdArea)
                throw new InvalidOperationException("El reemplazo debe tener el mismo rol y área del usuario origen.");

            var tickets = await _ticketRepository.ObtenerCargaActivaUsuarioAsync(idUsuario);
            foreach (var ticket in tickets)
            {
                if (ticket.EsResponsableFuncional(idUsuario, TipoResponsabilidadTicket.Desarrollo))
                {
                    ticket.AsignarResponsable(
                        TipoResponsabilidadTicket.Desarrollo,
                        reemplazo.IdUsuario,
                        _usuarioActual.IdUsuario,
                        _usuarioActual.Rol,
                        request.Comentario,
                        esTransferenciaMasiva: true);
                }

                if (ticket.EsResponsableFuncional(idUsuario, TipoResponsabilidadTicket.QA))
                {
                    ticket.AsignarResponsable(
                        TipoResponsabilidadTicket.QA,
                        reemplazo.IdUsuario,
                        _usuarioActual.IdUsuario,
                        _usuarioActual.Rol,
                        request.Comentario,
                        esTransferenciaMasiva: true);
                }

                if (ticket.IdUsuarioAsignado == idUsuario)
                {
                    ticket.ReasignarTicket(
                        reemplazo.IdUsuario,
                        _usuarioActual.IdUsuario,
                        _usuarioActual.Rol,
                        request.Comentario,
                        esTransferenciaMasiva: true);
                }
            }

            await _ticketRepository.ActualizarRangoAsync(tickets);
            return tickets.Count;
        }

        public async Task DesbloquearAsync(long idUsuario)
        {
            ValidarPlannerOLiderTecnico();
            var usuario = await ObtenerEntidadAsync(idUsuario);

            var contrasenaPorDefecto = ObtenerContrasenaPorDefecto();
            ValidadorContrasena.Validar(contrasenaPorDefecto);
            var ahora = DateTimeOffset.UtcNow;
            usuario.RestablecerContrasena(
                _contrasenaHasher.CrearHash(contrasenaPorDefecto),
                ahora);

            await _autenticacionRepository.RevocarSesionesAsync(idUsuario, ahora);
            await _repository.ActualizarAsync(usuario);
        }

        private void ValidarPlannerOLiderTecnico()
        {
            if (_usuarioActual.Rol is not Rol.Planner and not Rol.LiderTecnico)
                throw new UnauthorizedAccessException("Solo Planner o Lider Tecnico pueden administrar usuarios.");
        }

        private async Task<Usuario> ObtenerEntidadAsync(long idUsuario)
        {
            return await _repository.ObtenerPorIdAsync(idUsuario)
                ?? throw new RecursoNoEncontradoException("Usuario no encontrado.");
        }

        private async Task ValidarUsuarioSinCargaActivaAsync(long idUsuario)
        {
            var carga = await _ticketRepository.ObtenerCargaActivaUsuarioAsync(idUsuario);
            if (carga.Count > 0)
                throw new ConflictoException(
                    "USUARIO_CON_CARGA_ACTIVA: Transfiera los tickets antes de cambiar rol, área o desactivar el usuario.");
        }

        private static UsuarioDTO Mapear(Usuario usuario) => new(
            usuario.IdUsuario,
            usuario.NombreUsuario,
            usuario.Nombres,
            usuario.Apellidos,
            usuario.IdRol,
            usuario.IdArea,
            usuario.ImagenPerfilBase64,
            usuario.Activo,
            usuario.Bloqueado,
            usuario.IntentosFallidos,
            usuario.FechaBloqueo,
            usuario.ContrasenaExpiraEn);

        private string ObtenerContrasenaPorDefecto()
        {
            var contrasenaPorDefecto = _configuration[ContrasenaPorDefectoKey];
            if (string.IsNullOrWhiteSpace(contrasenaPorDefecto))
                throw new InvalidOperationException(
                    $"No existe la configuración obligatoria {ContrasenaPorDefectoKey}.");

            return contrasenaPorDefecto;
        }
    }
}
