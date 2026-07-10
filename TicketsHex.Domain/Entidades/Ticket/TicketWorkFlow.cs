using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public static class TicketWorkflow
    {
        private record ReglaTransicion(TicketEstado[] EstadosPrevios, Rol[] RolesPermitidos, bool RequiereComentario = false);

        private static readonly Dictionary<TicketEstado, ReglaTransicion> ReglasDeTransicion = new()
        {
            [TicketEstado.EnProceso] = new([TicketEstado.EnAnalisis], []),
            [TicketEstado.Bloqueado] = new([TicketEstado.EnProceso], [], true),
            [TicketEstado.Entregado] = new([TicketEstado.Bloqueado], []),
            [TicketEstado.DespliegueApitesting] = new([TicketEstado.Entregado], []),
            [TicketEstado.EnRevisionApitesting] = new([TicketEstado.DespliegueApitesting], []),
            [TicketEstado.AprobadoApitesting] = new([TicketEstado.EnRevisionApitesting], []),
            [TicketEstado.DespligueQA] = new([TicketEstado.AprobadoApitesting], []),
            [TicketEstado.EnRevisionQA] = new([TicketEstado.DespligueQA], []),
            [TicketEstado.AprobadoQA] = new([TicketEstado.EnRevisionQA], []),
            [TicketEstado.PendienteCertificacion] = new([TicketEstado.AprobadoQA], []),
            [TicketEstado.Certificado] = new([TicketEstado.PendienteCertificacion], []),
            [TicketEstado.DespliegueProduccion] = new([TicketEstado.Certificado], []),
            [TicketEstado.BUG] = new(Enum.GetValues<TicketEstado>(), [], true),
            [TicketEstado.Rollback] = new(Enum.GetValues<TicketEstado>(), [], true)
        };

        private static readonly TicketEstado[] EstadosConSalidaLibre =
        [
            TicketEstado.Bloqueado,
            TicketEstado.BUG,
            TicketEstado.Rollback
        ];

        public static void ValidarTransicion(TicketEstado estadoActual, TicketEstado nuevoEstado, Rol rolActualiza, string? comentario)
        {
            if (!Enum.IsDefined(nuevoEstado))
                throw new ArgumentOutOfRangeException(nameof(nuevoEstado), nuevoEstado, "El estado objetivo no es válido.");

            if (EstadosConSalidaLibre.Contains(estadoActual))
                return;

            if (!ReglasDeTransicion.TryGetValue(nuevoEstado, out var regla))
                throw new InvalidOperationException($"No hay reglas de transición definidas para el estado objetivo: {nuevoEstado}.");

            // 1. Validar el estado previo
            if (!regla.EstadosPrevios.Contains(estadoActual))
                throw new InvalidOperationException($"Transición inválida. No se puede pasar a {nuevoEstado} desde el estado actual {estadoActual}. Estados válidos de origen: {string.Join(", ", regla.EstadosPrevios)}.");

            // 2. Preparado para restricciones futuras por rol.
            // Mientras RolesPermitidos esté vacío, cualquier rol autenticado puede ejecutar la transición.
            if (regla.RolesPermitidos.Length > 0 && !regla.RolesPermitidos.Contains(rolActualiza))
                throw new InvalidOperationException($"Rol [{rolActualiza}] no autorizado para esta transición. Requeridos: {string.Join(", ", regla.RolesPermitidos)}.");

            // 3. Validar obligatoriedad de justificación
            if (regla.RequiereComentario && string.IsNullOrWhiteSpace(comentario))
                throw new ArgumentException($"Se requiere obligatoriamente un comentario justificativo para cambiar al estado {nuevoEstado}.", nameof(comentario));
        }
    }
}
