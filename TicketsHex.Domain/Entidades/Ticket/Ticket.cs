using TicketsHex.Domain.Enums;
using TicketsHex.Domain.ValueObjects.Ticket;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public class Ticket
    {
        // Propiedades directas que hacen match con las columnas de Postgres
        public Guid IdTicket { get; set; }
        public CodigoCasoVO CodigoCaso { get; set; } // Mantiene el VO
        public TituloVO Titulo { get; set; }     // Mantiene el VO
        public DescripcionVO Descripcion { get; set; } // Mantiene el VO
        public DateTimeOffset FechaAsignacion { get; set; }
        public DateTimeOffset? FechaUltimaActualizacion { get; set; }
        public long? IdUsuarioAsignado { get; set; }
        public TicketOrigen IdOrigen { get; set; }
        public TicketEstado IdEstado { get; set; }
        public string? CarpetaMedios { get; set; }
        public string? CausaRaiz { get; set; }
        public string? SolucionPropuesta { get; set; }

        // Propiedades de Navegación directas de EF Core (Baja complejidad)
        public virtual ICollection<HistoricoEstadosTicket> HistoricoEstados { get; set; } = new List<HistoricoEstadosTicket>();

        // Constructor vacío requerido por EF Core
        public Ticket() { }


        // Factory Method (Sustituye al método CrearTicket difuso)
        // Constructor de inicialización de negocio (Ajustado)
        public Ticket(string codigoCaso, string titulo, string descripcion, long? usuarioAsignado, TicketOrigen origenTicket = TicketOrigen.SAIA)
        {
            if (usuarioAsignado is not null && usuarioAsignado <= 0)
                throw new ArgumentException("El ID del usuario asignado debe ser un número positivo.", nameof(usuarioAsignado));

            IdTicket = Guid.NewGuid();
            CodigoCaso = new CodigoCasoVO(codigoCaso); // Mapeado a VARCHAR(20) en tu script
            Titulo = new TituloVO(titulo);
            Descripcion = new DescripcionVO(descripcion);
            FechaAsignacion = DateTimeOffset.UtcNow;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
            IdUsuarioAsignado = usuarioAsignado;
            IdOrigen = origenTicket;
            IdEstado = TicketEstado.EnAnalisis;

            // Registrar la creación en la colección relacional de forma simple
            HistoricoEstados.Add(new HistoricoEstadosTicket
            {
                IdHistorico = Guid.NewGuid(),
                IdTicket = this.IdTicket,
                IdEstadoOrigen = null, // Al ser creación, no viene de ningún estado previo
                IdEstadoDestino = TicketEstado.EnAnalisis,
                IdUsuarioAccion = usuarioAsignado ?? 0, // ID del planner o creador
                Comentario = "Creación inicial del ticket.",
                FechaCambio = DateTimeOffset.UtcNow
            });
        }

        public void ActualizarEstado(TicketEstado nuevoEstado, long idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
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
            ArgumentNullException.ThrowIfNull(nuevaDescripcion);

            if (rolActualiza != Rol.Desarrollador && rolActualiza != Rol.LiderTecnico)
                throw new InvalidOperationException("Solo los roles de Desarrollador o Líder Técnico pueden actualizar la descripción.");

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
    }
}
