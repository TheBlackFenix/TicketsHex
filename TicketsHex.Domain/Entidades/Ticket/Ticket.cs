using TicketsHex.Domain.Enums;
using TicketsHex.Domain.ValueObjects.Ticket;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public class Ticket
    {
        // Propiedades directas que hacen match con las columnas de Postgres
        public Guid IdTicket { get; set; }
        public CodigoCasoVO CodigoCaso { get; set; } = null!; // Mantiene el VO
        public TituloVO Titulo { get; set; } = null!;     // Mantiene el VO
        public DescripcionVO Descripcion { get; set; } = null!; // Mantiene el VO
        public DateTimeOffset FechaAsignacion { get; set; }
        public DateTimeOffset? FechaUltimaActualizacion { get; set; }
        public long? IdUsuarioAsignado { get; set; }
        public TicketOrigen IdOrigen { get; set; }
        public TicketEstado IdEstado { get; set; }
        public string? CarpetaMedios { get; set; }
        public string? CausaRaiz { get; set; }
        public string? SolucionPropuesta { get; set; }
        public bool EsDesarrollo { get; set; }
        public string? NombreHu { get; set; }
        public string? UrlHu { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset? FechaEliminacion { get; set; }
        public long? IdUsuarioEliminacion { get; set; }

        // Propiedades de Navegación directas de EF Core (Baja complejidad)
        public virtual ICollection<HistoricoEstadosTicket> HistoricoEstados { get; set; } = new List<HistoricoEstadosTicket>();

        // Constructor vacío requerido por EF Core
        public Ticket() { }


        // Factory Method (Sustituye al método CrearTicket difuso)
        // Constructor de inicialización de negocio (Ajustado)
        public Ticket(
            string codigoCaso,
            string titulo,
            string descripcion,
            long? usuarioAsignado,
            long idUsuarioCreador,
            TicketOrigen origenTicket = TicketOrigen.SAIA,
            bool esDesarrollo = false)
        {
            if (usuarioAsignado is not null && usuarioAsignado <= 0)
                throw new ArgumentException("El ID del usuario asignado debe ser un número positivo.", nameof(usuarioAsignado));
            if (idUsuarioCreador <= 0)
                throw new ArgumentException("El ID del usuario creador debe ser positivo.", nameof(idUsuarioCreador));

            IdTicket = Guid.NewGuid();
            CodigoCaso = new CodigoCasoVO(codigoCaso); // Mapeado a VARCHAR(20) en tu script
            Titulo = new TituloVO(titulo);
            Descripcion = new DescripcionVO(descripcion);
            FechaAsignacion = DateTimeOffset.UtcNow;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
            IdUsuarioAsignado = usuarioAsignado;
            IdOrigen = origenTicket;
            IdEstado = TicketEstado.EnAnalisis;
            EsDesarrollo = esDesarrollo;
            Activo = true;

            // Registrar la creación en la colección relacional de forma simple
            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = this.IdTicket,
                IdEstadoOrigen = null, // Al ser creación, no viene de ningún estado previo
                IdEstadoDestino = TicketEstado.EnAnalisis,
                IdUsuarioAccion = idUsuarioCreador,
                Comentario = "Creación inicial del ticket.",
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        public void ActualizarEstado(TicketEstado nuevoEstado, long idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            ValidarActivo();

            if (nuevoEstado == IdEstado)
                throw new InvalidOperationException("El nuevo estado debe ser diferente al estado actual.");

            // La máquina de estados sigue validando con las reglas del negocio
            TicketWorkflow.ValidarTransicion(IdEstado, nuevoEstado, rolActualiza, comentario);

            var estadoAnterior = IdEstado;
            IdEstado = nuevoEstado;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            // Trazabilidad directa en la tabla relacional
            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = this.IdTicket,
                IdEstadoOrigen = estadoAnterior,
                IdEstadoDestino = nuevoEstado,
                IdUsuarioAccion = idUsuarioActualizacion,
                Comentario = comentario,
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        public void ReasignarTicket(long nuevoIdUsuarioAsignado, long idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            ValidarActivo();

            if (nuevoIdUsuarioAsignado <= 0)
                throw new ArgumentException("El ID del nuevo usuario asignado debe ser un número positivo.", nameof(nuevoIdUsuarioAsignado));
            if (nuevoIdUsuarioAsignado == IdUsuarioAsignado)
                throw new InvalidOperationException("El nuevo usuario asignado debe ser diferente al actual.");
            if (rolActualiza != Rol.LiderTecnico && rolActualiza != Rol.Planner)
                throw new InvalidOperationException("Solo los roles de Líder Técnico o Planner pueden reasignar tickets.");

            IdUsuarioAsignado = nuevoIdUsuarioAsignado;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = this.IdTicket,
                IdEstadoOrigen = IdEstado,
                IdEstadoDestino = IdEstado, // El estado no cambia, sólo se audita la reasignación
                IdUsuarioAccion = idUsuarioActualizacion,
                Comentario = $"Reasignado. Nuevo usuario: {nuevoIdUsuarioAsignado}. Obs: {comentario}",
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        public void ActualizarDescripcion(DescripcionVO nuevaDescripcion, long idUsuarioActualizacion, Rol rolActualiza)
        {
            ValidarActivo();
            ArgumentNullException.ThrowIfNull(nuevaDescripcion);

            if (rolActualiza != Rol.Planner && rolActualiza != Rol.LiderTecnico)
                throw new UnauthorizedAccessException("Solo Planner o Líder Técnico pueden actualizar la descripción.");

            Descripcion = nuevaDescripcion;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = this.IdTicket,
                IdEstadoOrigen = IdEstado,
                IdEstadoDestino = IdEstado,
                IdUsuarioAccion = idUsuarioActualizacion,
                Comentario = "Descripción actualizada.",
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        public void AgregarComentarioLibre(string nuevoComentario, long idUsuarioActualizacion, Rol rolActualiza)
        {
            ValidarActivo();

            if (string.IsNullOrWhiteSpace(nuevoComentario))
                throw new ArgumentException("El comentario no puede estar vacío.", nameof(nuevoComentario));

            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = this.IdTicket,
                IdEstadoOrigen = IdEstado,
                IdEstadoDestino = IdEstado,
                IdUsuarioAccion = idUsuarioActualizacion,
                Comentario = nuevoComentario,
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        public void ActualizarTitulo(string nuevoTitulo, long idUsuarioActualizacion, Rol rolActualiza)
        {
            ValidarActivo();

            if (rolActualiza != Rol.Planner && rolActualiza != Rol.LiderTecnico)
                throw new UnauthorizedAccessException("Solo Planner o Líder Técnico pueden actualizar el título.");

            Titulo = new TituloVO(nuevoTitulo);
            RegistrarAuditoria(idUsuarioActualizacion, "Título actualizado.");
        }

        public void ActualizarDiagnostico(
            string? causaRaiz,
            string? solucionPropuesta,
            long idUsuarioActualizacion,
            Rol rolActualiza)
        {
            ValidarActivo();

            if (rolActualiza != Rol.Desarrollador)
                throw new UnauthorizedAccessException("Solo el Desarrollador puede actualizar la causa raíz y la solución propuesta.");

            if (causaRaiz is null && solucionPropuesta is null)
                throw new ArgumentException("Debe indicar la causa raíz o la solución propuesta.");

            if (causaRaiz is not null)
            {
                if (causaRaiz.Length > 1000)
                    throw new ArgumentException("La causa raíz no puede superar 1000 caracteres.", nameof(causaRaiz));
                CausaRaiz = causaRaiz;
            }

            if (solucionPropuesta is not null)
            {
                if (solucionPropuesta.Length > 1000)
                    throw new ArgumentException("La solución propuesta no puede superar 1000 caracteres.", nameof(solucionPropuesta));
                SolucionPropuesta = solucionPropuesta;
            }

            RegistrarAuditoria(idUsuarioActualizacion, "Diagnóstico técnico actualizado.");
        }

        public void ActualizarDatosDesarrollo(
            bool? esDesarrollo,
            string? nombreHu,
            string? urlHu,
            long idUsuarioActualizacion,
            Rol rolActualiza)
        {
            ValidarActivo();

            if (rolActualiza != Rol.Planner)
                throw new UnauthorizedAccessException("Solo el Planner puede actualizar los datos de desarrollo y la HU.");

            var nuevoEsDesarrollo = esDesarrollo ?? EsDesarrollo;
            var nombreHuSolicitado = nombreHu is null ? null : NormalizarTextoOpcional(nombreHu);
            var urlHuSolicitada = urlHu is null ? null : NormalizarTextoOpcional(urlHu);
            var nuevoNombreHu = nombreHu is null ? NombreHu : nombreHuSolicitado;
            var nuevaUrlHu = urlHu is null ? UrlHu : urlHuSolicitada;

            if (!nuevoEsDesarrollo)
            {
                if (nombreHuSolicitado is not null || urlHuSolicitada is not null)
                    throw new InvalidOperationException("No se puede registrar una HU en un ticket que no es de desarrollo.");

                nuevoNombreHu = null;
                nuevaUrlHu = null;
            }
            else
            {
                if ((nuevoNombreHu is null) != (nuevaUrlHu is null))
                    throw new ArgumentException("El nombre y la URL de la HU deben registrarse juntos.");

                if (nuevoNombreHu?.Length > 100)
                    throw new ArgumentException("El nombre de la HU no puede superar 100 caracteres.", nameof(nombreHu));

                if (nuevaUrlHu?.Length > 2048)
                    throw new ArgumentException("La URL de la HU no puede superar 2048 caracteres.", nameof(urlHu));

                if (nuevaUrlHu is not null &&
                    (!Uri.TryCreate(nuevaUrlHu, UriKind.Absolute, out var uriHu) ||
                     (uriHu.Scheme != Uri.UriSchemeHttp && uriHu.Scheme != Uri.UriSchemeHttps)))
                {
                    throw new ArgumentException("La URL de la HU debe ser una URL absoluta HTTP o HTTPS.", nameof(urlHu));
                }
            }

            EsDesarrollo = nuevoEsDesarrollo;
            NombreHu = nuevoNombreHu;
            UrlHu = nuevaUrlHu;
            RegistrarAuditoria(
                idUsuarioActualizacion,
                EsDesarrollo ? "Datos de desarrollo y HU actualizados." : "Ticket marcado como no desarrollo.");
        }

        public void EliminarLogicamente(long idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            ValidarActivo();

            if (rolActualiza != Rol.Planner)
                throw new UnauthorizedAccessException("Solo el Planner puede eliminar tickets.");

            Activo = false;
            FechaEliminacion = DateTimeOffset.UtcNow;
            IdUsuarioEliminacion = idUsuarioActualizacion;
            RegistrarAuditoria(idUsuarioActualizacion, $"Ticket eliminado lógicamente. {comentario}".Trim());
        }

        private void RegistrarAuditoria(long idUsuarioActualizacion, string comentario)
        {
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = IdTicket,
                IdEstadoOrigen = IdEstado,
                IdEstadoDestino = IdEstado,
                IdUsuarioAccion = idUsuarioActualizacion,
                Comentario = comentario,
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        private void ValidarActivo()
        {
            if (!Activo)
                throw new InvalidOperationException("No se puede modificar un ticket eliminado.");
        }

        private static string? NormalizarTextoOpcional(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
