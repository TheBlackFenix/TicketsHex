using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public static class TicketWorkflow
    {
        private record ReglaTransicion(TicketEstado[] EstadosPrevios, Roles[] RolesPermitidos, bool RequiereComentario = false);

        private static readonly Dictionary<TicketEstado, ReglaTransicion> ReglasDeTransicion = new()
        {
            [TicketEstado.EnProceso] = new([TicketEstado.EnAnalisis], [Roles.Desarrollador, Roles.LiderTecnico]),
            [TicketEstado.Bloqueado] = new([TicketEstado.EnProceso], [Roles.Desarrollador, Roles.LiderTecnico], true),
            [TicketEstado.Entregado] = new([TicketEstado.EnProceso], [Roles.Desarrollador, Roles.LiderTecnico]),
            [TicketEstado.DespliegueApitesting] = new([TicketEstado.Entregado], [Roles.LiderTecnico]),
            [TicketEstado.EnRevisionApitesting] = new([TicketEstado.DespliegueApitesting], [Roles.QA, Roles.Desarrollador]),
            [TicketEstado.AprobadoApitesting] = new([TicketEstado.EnRevisionApitesting], [Roles.QA, Roles.Desarrollador]),
            [TicketEstado.DespligueQA] = new([TicketEstado.AprobadoApitesting], [Roles.LiderTecnico]),
            [TicketEstado.EnRevisionQA] = new([TicketEstado.DespligueQA], [Roles.QA]),
            [TicketEstado.AprobadoQA] = new([TicketEstado.EnRevisionQA], [Roles.QA]),
            [TicketEstado.PendienteCertificacion] = new([TicketEstado.AprobadoQA], [Roles.Planner]),
            [TicketEstado.Certificado] = new([TicketEstado.PendienteCertificacion], [Roles.Planner, Roles.QA]),
            [TicketEstado.DespliegueProduccion] = new([TicketEstado.AprobadoQA], [Roles.LiderTecnico]),
            [TicketEstado.BUG] = new([TicketEstado.EnProceso, TicketEstado.EnRevisionApitesting, TicketEstado.EnRevisionQA], [Roles.Desarrollador, Roles.QA, Roles.LiderTecnico], true),
            [TicketEstado.Rollback] = new([TicketEstado.DespliegueProduccion], [Roles.LiderTecnico])
        };

        public static void ValidarTransicion(TicketEstado estadoActual, TicketEstado nuevoEstado, Roles rolActualiza, string? comentario)
        {
            if (!ReglasDeTransicion.TryGetValue(nuevoEstado, out var regla))
                throw new InvalidOperationException($"No hay reglas de transición definidas para el estado objetivo: {nuevoEstado}.");

            // 1. Validar el estado previo
            if (!regla.EstadosPrevios.Contains(estadoActual))
                throw new InvalidOperationException($"Transición inválida. No se puede pasar a {nuevoEstado} desde el estado actual {estadoActual}. Estados válidos de origen: {string.Join(", ", regla.EstadosPrevios)}.");

            // 2. Validar el rol operante
            if (!regla.RolesPermitidos.Contains(rolActualiza))
                throw new InvalidOperationException($"Rol [{rolActualiza}] no autorizado para esta transición. Requeridos: {string.Join(", ", regla.RolesPermitidos)}.");

            // 3. Validar obligatoriedad de justificación
            if (regla.RequiereComentario && string.IsNullOrWhiteSpace(comentario))
                throw new ArgumentException($"Se requiere obligatoriamente un comentario justificativo para cambiar al estado {nuevoEstado}.", nameof(comentario));
        }
    }
}
