using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Ticket
{
    public static class TicketWorkflow
    {
        private record ReglaTransicion(
            TicketEstado EstadoOrigen,
            TicketEstado EstadoDestino,
            Rol[] RolesPermitidos,
            bool RequiereComentario = false);

        private static readonly ReglaTransicion[] ReglasDeTransicion =
        [
            new(TicketEstado.EnAnalisis, TicketEstado.EnProceso, [Rol.Desarrollador, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.EnAnalisis, TicketEstado.EnReplicaQA, [Rol.Desarrollador, Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.EnReplicaQA, TicketEstado.EnAnalisis, [Rol.QA, Rol.LiderTecnico, Rol.Planner], true),
            new(TicketEstado.EnProceso, TicketEstado.Bloqueado, Enum.GetValues<Rol>(), true),
            new(TicketEstado.EnProceso, TicketEstado.Entregado, [Rol.Desarrollador, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.Entregado, TicketEstado.DespliegueApitesting, [Rol.Desarrollador, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.DespliegueApitesting, TicketEstado.EnRevisionApitesting, [Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.EnRevisionApitesting, TicketEstado.AprobadoApitesting, [Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.AprobadoApitesting, TicketEstado.DespligueQA, [Rol.Desarrollador, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.DespligueQA, TicketEstado.EnRevisionQA, [Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.EnRevisionQA, TicketEstado.AprobadoQA, [Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.AprobadoQA, TicketEstado.PendienteCertificacion, [Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.PendienteCertificacion, TicketEstado.Certificado, [Rol.QA, Rol.LiderTecnico, Rol.Planner]),
            new(TicketEstado.Certificado, TicketEstado.DespliegueProduccion, [Rol.Desarrollador, Rol.LiderTecnico, Rol.Planner])
        ];

        private static readonly TicketEstado[] EstadosConSalidaLibre =
        [
            TicketEstado.Bloqueado,
            TicketEstado.BUG,
            TicketEstado.Rollback
        ];

        private static readonly TicketEstado[] EstadosQa =
        [
            TicketEstado.EnReplicaQA,
            TicketEstado.DespliegueApitesting,
            TicketEstado.EnRevisionApitesting,
            TicketEstado.AprobadoApitesting,
            TicketEstado.DespligueQA,
            TicketEstado.EnRevisionQA,
            TicketEstado.AprobadoQA,
            TicketEstado.PendienteCertificacion,
            TicketEstado.Certificado,
            TicketEstado.BUG
        ];

        public static IReadOnlyCollection<TicketEstado> EstadosAccesiblesParaQa => EstadosQa;

        public static void ValidarTransicion(TicketEstado estadoActual, TicketEstado nuevoEstado, Rol rolActualiza, string? comentario)
        {
            if (!Enum.IsDefined(nuevoEstado))
                throw new ArgumentOutOfRangeException(nameof(nuevoEstado), nuevoEstado, "El estado objetivo no es válido.");

            if (estadoActual == TicketEstado.Finalizado)
                throw new InvalidOperationException("Un ticket finalizado es terminal y no admite nuevas transiciones.");

            if (nuevoEstado == TicketEstado.Finalizado)
            {
                if (rolActualiza is not Rol.Planner and not Rol.LiderTecnico)
                    throw new UnauthorizedAccessException("Solo Planner o Lider Tecnico pueden finalizar un ticket.");
                ValidarComentarioObligatorio(nuevoEstado, comentario);
                return;
            }

            if (nuevoEstado is TicketEstado.BUG or TicketEstado.Rollback)
            {
                var roles = nuevoEstado == TicketEstado.BUG
                    ? new[] { Rol.QA, Rol.LiderTecnico, Rol.Planner }
                    : new[] { Rol.LiderTecnico, Rol.Planner };
                ValidarRol(rolActualiza, roles, nuevoEstado);
                ValidarComentarioObligatorio(nuevoEstado, comentario);
                return;
            }

            var regla = ReglasDeTransicion.FirstOrDefault(item =>
                item.EstadoOrigen == estadoActual && item.EstadoDestino == nuevoEstado);

            if (regla is null && EstadosConSalidaLibre.Contains(estadoActual))
            {
                ValidarRol(rolActualiza, ObtenerRolesPorDestino(nuevoEstado), nuevoEstado);
                return;
            }

            if (regla is null)
            {
                if (estadoActual == TicketEstado.EnReplicaQA)
                {
                    throw new InvalidOperationException(
                        "Desde EnReplicaQA solo se puede volver a EnAnalisis o finalizar el ticket.");
                }

                if (rolActualiza is Rol.Planner or Rol.LiderTecnico)
                {
                    ValidarComentarioObligatorio(nuevoEstado, comentario);
                    return;
                }

                throw new InvalidOperationException(
                    $"Transición inválida. No se puede pasar a {nuevoEstado} desde {estadoActual}.");
            }

            ValidarRol(rolActualiza, regla.RolesPermitidos, nuevoEstado);
            if (regla.RequiereComentario)
                ValidarComentarioObligatorio(nuevoEstado, comentario);
        }

        public static bool EsEstadoAccesibleParaQa(TicketEstado estado) =>
            EstadosQa.Contains(estado);

        private static Rol[] ObtenerRolesPorDestino(TicketEstado estado) => estado switch
        {
            TicketEstado.EnProceso or
            TicketEstado.EnAnalisis or
            TicketEstado.Entregado or
            TicketEstado.DespliegueApitesting or
            TicketEstado.DespligueQA or
            TicketEstado.DespliegueProduccion => [Rol.Desarrollador, Rol.LiderTecnico, Rol.Planner],
            TicketEstado.EnReplicaQA =>
                [Rol.Desarrollador, Rol.QA, Rol.LiderTecnico, Rol.Planner],
            TicketEstado.EnRevisionApitesting or
            TicketEstado.AprobadoApitesting or
            TicketEstado.EnRevisionQA or
            TicketEstado.AprobadoQA or
            TicketEstado.PendienteCertificacion or
            TicketEstado.Certificado => [Rol.QA, Rol.LiderTecnico, Rol.Planner],
            TicketEstado.Bloqueado => Enum.GetValues<Rol>(),
            _ => [Rol.LiderTecnico, Rol.Planner]
        };

        private static void ValidarRol(Rol rol, Rol[] roles, TicketEstado destino)
        {
            if (!roles.Contains(rol))
                throw new UnauthorizedAccessException(
                    $"El rol {rol} no puede realizar la transición hacia {destino}.");
        }

        private static void ValidarComentarioObligatorio(TicketEstado destino, string? comentario)
        {
            if (string.IsNullOrWhiteSpace(comentario))
                throw new ArgumentException(
                    $"Se requiere un comentario para cambiar al estado {destino}.",
                    nameof(comentario));
        }
    }
}
