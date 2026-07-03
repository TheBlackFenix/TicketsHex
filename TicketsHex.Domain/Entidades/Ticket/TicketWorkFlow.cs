using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public static class TicketWorkflow
    {
        private record ReglaTransicion(TicketEstado[] EstadosPrevios, Rol[] RolesPermitidos, bool RequiereComentario = false);

        private static readonly Dictionary<TicketEstado, ReglaTransicion> ReglasDeTransicion = new()
        {
            [TicketEstado.EnProceso] = new(
                [TicketEstado.EnAnalisis, TicketEstado.Bloqueado, TicketEstado.BUG],
                [Rol.Desarrollador, Rol.LiderTecnico]),
            [TicketEstado.Bloqueado] = new([TicketEstado.EnProceso], [Rol.Desarrollador, Rol.LiderTecnico], true),
            [TicketEstado.Entregado] = new([TicketEstado.EnProceso], [Rol.Desarrollador, Rol.LiderTecnico]),
            [TicketEstado.DespliegueApitesting] = new([TicketEstado.Entregado], [Rol.LiderTecnico]),
            [TicketEstado.EnRevisionApitesting] = new([TicketEstado.DespliegueApitesting], [Rol.QA, Rol.Desarrollador]),
            [TicketEstado.AprobadoApitesting] = new([TicketEstado.EnRevisionApitesting], [Rol.QA, Rol.Desarrollador]),
            [TicketEstado.DespligueQA] = new([TicketEstado.AprobadoApitesting], [Rol.LiderTecnico]),
            [TicketEstado.EnRevisionQA] = new([TicketEstado.DespligueQA], [Rol.QA]),
            [TicketEstado.AprobadoQA] = new([TicketEstado.EnRevisionQA], [Rol.QA]),
            [TicketEstado.PendienteCertificacion] = new([TicketEstado.AprobadoQA], [Rol.Planner]),
            [TicketEstado.Certificado] = new([TicketEstado.PendienteCertificacion], [Rol.Planner, Rol.QA]),
            [TicketEstado.DespliegueProduccion] = new([TicketEstado.AprobadoQA], [Rol.LiderTecnico]),
            [TicketEstado.BUG] = new([TicketEstado.EnProceso, TicketEstado.EnRevisionApitesting, TicketEstado.EnRevisionQA], [Rol.Desarrollador, Rol.QA, Rol.LiderTecnico], true),
            [TicketEstado.Rollback] = new([TicketEstado.DespliegueProduccion], [Rol.LiderTecnico])
        };

        public static void ValidarTransicion(TicketEstado estadoActual, TicketEstado nuevoEstado, Rol rolActualiza, string? comentario)
        {
            if (!Enum.IsDefined(nuevoEstado))
                throw new ArgumentOutOfRangeException(nameof(nuevoEstado), nuevoEstado, "El estado objetivo no es válido.");

            var esRolAdministradorDelFlujo = rolActualiza is Rol.Planner or Rol.LiderTecnico;

            if (esRolAdministradorDelFlujo)
            {
                if (ReglasDeTransicion.TryGetValue(nuevoEstado, out var reglaAdministrativa) &&
                    reglaAdministrativa.RequiereComentario &&
                    string.IsNullOrWhiteSpace(comentario))
                {
                    throw new ArgumentException($"Se requiere obligatoriamente un comentario justificativo para cambiar al estado {nuevoEstado}.", nameof(comentario));
                }

                return;
            }

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
