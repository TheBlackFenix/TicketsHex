using TicketsHex.Domain.Enums;
using TicketsHex.Domain.ValueObjects.Ticket;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public class Ticket
    {
        // Lista interna para persistencia encubierta (Backing Field)
        private readonly List<TicketModificacion> _historial = new();

        public Guid Id { get; private set; }
        public CodigoCasoVO CodigoCaso { get; private set; }
        public TituloVO Titulo { get; private set; }
        public DescripcionVO Descripcion { get; private set; }
        public DateTimeOffset FechaAsignacion { get; private set; }
        public DateTimeOffset? FechaUltimaActualizacion { get; private set; }
        public int? IdUsuarioAsignado { get; private set; }
        public TicketEstado TicketEstado { get; private set; }

        // Exponer la colección como de solo lectura para proteger el encapsulamiento
        public IReadOnlyCollection<TicketModificacion> Historial => _historial.AsReadOnly();

        // Constructor privado para el flujo controlado (Factory y ORMs)
        private Ticket() { }

        // Constructor completo para cuando se reconstruye desde base de datos
        public Ticket(Guid id, CodigoCasoVO codigoCaso, TituloVO titulo, DescripcionVO descripcion,
                      DateTimeOffset fechaAsignacion, DateTimeOffset? fechaUltimaActualizacion,
                      int idUsuarioAsignado, TicketEstado ticketEstado)
        {
            Id = id;
            CodigoCaso = codigoCaso;
            Titulo = titulo;
            Descripcion = descripcion;
            FechaAsignacion = fechaAsignacion;
            FechaUltimaActualizacion = fechaUltimaActualizacion;
            IdUsuarioAsignado = idUsuarioAsignado;
            TicketEstado = ticketEstado;
        }

        // Factory Method (Sustituye al método CrearTicket difuso)
        public Ticket (int codigoCaso, string titulo, string descripcion, int? idUsuarioAsignado)
        {
            if (idUsuarioAsignado is not null && idUsuarioAsignado <= 0)
                throw new ArgumentException("El ID del usuario asignado debe ser un número positivo.", nameof(idUsuarioAsignado));


            Id = Guid.NewGuid();
            CodigoCaso =  new CodigoCasoVO(codigoCaso);
            Titulo = new TituloVO(titulo);
            Descripcion = new DescripcionVO(descripcion);
            FechaAsignacion = DateTimeOffset.UtcNow;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
            IdUsuarioAsignado = idUsuarioAsignado;
            TicketEstado = TicketEstado.EnAnalisis;

            // Registrar la creación en el historial
            _historial.Add(new TicketModificacion(
                TicketEstado.EnAnalisis, TicketEstado.EnAnalisis, idUsuarioAsignado, Rol.Planner, "Creación inicial del ticket."));
        }

        public void ActualizarEstado(TicketEstado nuevoEstado, int idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            if (nuevoEstado == TicketEstado)
                throw new InvalidOperationException("El nuevo estado debe ser diferente al estado actual.");

            // SRP: Delegamos la evaluación de transiciones de negocio a su propia estructura
            TicketWorkflow.ValidarTransicion(TicketEstado, nuevoEstado, rolActualiza, comentario);

            // Aplicar cambios internos
            var estadoAnterior = TicketEstado;
            TicketEstado = nuevoEstado;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            // Registrar trazabilidad inmutable
            _historial.Add(new TicketModificacion(estadoAnterior, nuevoEstado, idUsuarioActualizacion, rolActualiza, comentario));
        }

        public void ReasignarTicket(int nuevoIdUsuarioAsignado, int idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            if (nuevoIdUsuarioAsignado <= 0)
                throw new ArgumentException("El ID del nuevo usuario asignado debe ser un número positivo.", nameof(nuevoIdUsuarioAsignado));
            if (nuevoIdUsuarioAsignado == IdUsuarioAsignado)
                throw new InvalidOperationException("El nuevo usuario asignado debe ser diferente al actual.");
            if (rolActualiza != Rol.LiderTecnico && rolActualiza != Rol.Planner)
                throw new InvalidOperationException("Solo los roles de Líder Técnico o Planner pueden reasignar tickets.");

            IdUsuarioAsignado = nuevoIdUsuarioAsignado;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            _historial.Add(new TicketModificacion(TicketEstado, TicketEstado, idUsuarioActualizacion, rolActualiza, $"Reasignado. Nuevo usuario: {nuevoIdUsuarioAsignado}. Obs: {comentario}"));
        }

        public void ActualizarDescripcion(DescripcionVO nuevaDescripcion, int idUsuarioActualizacion, Rol rolActualiza)
        {
            ArgumentNullException.ThrowIfNull(nuevaDescripcion);

            if (rolActualiza != Rol.Desarrollador && rolActualiza != Rol.LiderTecnico)
                throw new InvalidOperationException("Solo los roles de Desarrollador o Líder Técnico pueden actualizar la descripción.");

            Descripcion = nuevaDescripcion;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
            _historial.Add(new TicketModificacion(TicketEstado, TicketEstado, idUsuarioActualizacion, rolActualiza, "Descripción actualizada."));
        }

        public void AgregarComentarioLibre(string nuevoComentario, int idUsuarioActualizacion, Rol rolActualiza)
        {
            if (string.IsNullOrWhiteSpace(nuevoComentario))
                throw new ArgumentException("El comentario no puede estar vacío.", nameof(nuevoComentario));

            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
            _historial.Add(new TicketModificacion(TicketEstado, TicketEstado, idUsuarioActualizacion, rolActualiza, nuevoComentario));
        }
    }
}
