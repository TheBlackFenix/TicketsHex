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
        public virtual ICollection<HistoricoAsignacionTicket> HistoricoAsignaciones { get; set; } = new List<HistoricoAsignacionTicket>();
        public virtual ICollection<ResponsableTicket> Responsables { get; set; } = new List<ResponsableTicket>();

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
            bool esDesarrollo = false,
            long? usuarioQa = null)
        {
            if (usuarioAsignado is not null && usuarioAsignado <= 0)
                throw new ArgumentException("El ID del usuario asignado debe ser un número positivo.", nameof(usuarioAsignado));
            if (idUsuarioCreador <= 0)
                throw new ArgumentException("El ID del usuario creador debe ser positivo.", nameof(idUsuarioCreador));
            if (usuarioQa is not null && usuarioQa <= 0)
                throw new ArgumentException("El ID del QA debe ser positivo.", nameof(usuarioQa));

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

            if (usuarioAsignado.HasValue)
            {
                Responsables.Add(new ResponsableTicket(
                    IdTicket,
                    TipoResponsabilidadTicket.Desarrollo,
                    usuarioAsignado.Value,
                    idUsuarioCreador));
                if (usuarioQa.HasValue)
                {
                    Responsables.Add(new ResponsableTicket(
                        IdTicket,
                        TipoResponsabilidadTicket.QA,
                        usuarioQa.Value,
                        idUsuarioCreador));
                }

                RegistrarAsignacion(
                    null,
                    usuarioAsignado.Value,
                    idUsuarioCreador,
                    "Asignación inicial del ticket.",
                    TipoMovimientoAsignacionTicket.AsignacionInicial);
            }
        }

        public void ActualizarEstado(TicketEstado nuevoEstado, long idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            ValidarModificable();

            if (nuevoEstado == IdEstado)
                throw new InvalidOperationException("El nuevo estado debe ser diferente al estado actual.");

            ValidarAutorizacionTransicion(nuevoEstado, idUsuarioActualizacion, rolActualiza);
            TicketWorkflow.ValidarTransicion(IdEstado, nuevoEstado, rolActualiza, comentario);
            ValidarResponsableRequerido(nuevoEstado);

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

            TransferirCustodiaPorEstado(estadoAnterior, nuevoEstado, idUsuarioActualizacion, comentario);
        }

        public void ReasignarTicket(
            long nuevoIdUsuarioAsignado,
            long idUsuarioActualizacion,
            Rol rolActualiza,
            string? comentario,
            bool esTransferenciaMasiva = false)
        {
            ValidarModificable();

            if (nuevoIdUsuarioAsignado <= 0)
                throw new ArgumentException("El ID del nuevo usuario asignado debe ser un número positivo.", nameof(nuevoIdUsuarioAsignado));
            if (nuevoIdUsuarioAsignado == IdUsuarioAsignado)
                throw new InvalidOperationException("El nuevo usuario asignado debe ser diferente al actual.");
            if (rolActualiza != Rol.LiderTecnico && rolActualiza != Rol.Planner)
                throw new UnauthorizedAccessException("Solo los roles de Líder Técnico o Planner pueden cambiar la custodia actual.");
            if (string.IsNullOrWhiteSpace(comentario))
                throw new ArgumentException("Debe indicar el motivo del cambio de custodia.", nameof(comentario));

            var idUsuarioAnterior = IdUsuarioAsignado;
            IdUsuarioAsignado = nuevoIdUsuarioAsignado;
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;

            RegistrarAsignacion(
                idUsuarioAnterior,
                nuevoIdUsuarioAsignado,
                idUsuarioActualizacion,
                comentario,
                esTransferenciaMasiva
                    ? TipoMovimientoAsignacionTicket.TransferenciaMasivaCarga
                    : TipoMovimientoAsignacionTicket.TransferenciaManualCustodia);

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
            ValidarModificable();
            ArgumentNullException.ThrowIfNull(nuevaDescripcion);

            ValidarDesarrolladorAsignadoOSupervisor(idUsuarioActualizacion, rolActualiza);

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
            ValidarModificable();

            if (string.IsNullOrWhiteSpace(nuevoComentario))
                throw new ArgumentException("El comentario no puede estar vacío.", nameof(nuevoComentario));
            if (!PuedeComentar(idUsuarioActualizacion, rolActualiza))
                throw new UnauthorizedAccessException("El usuario no puede comentar este ticket en su estado actual.");

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
            ValidarModificable();

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
            ValidarModificable();

            ValidarDesarrolladorAsignadoOSupervisor(idUsuarioActualizacion, rolActualiza);

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
            string? carpetaMedios,
            long idUsuarioActualizacion,
            Rol rolActualiza)
        {
            ValidarModificable();

            if (esDesarrollo.HasValue || carpetaMedios is not null)
                ValidarDesarrolladorAsignadoOSupervisor(idUsuarioActualizacion, rolActualiza);
            if ((nombreHu is not null || urlHu is not null) && !EsSupervisor(rolActualiza))
                throw new UnauthorizedAccessException("Solo Planner o Líder Técnico pueden actualizar la HU.");

            var nuevoEsDesarrollo = esDesarrollo ?? EsDesarrollo;
            var nombreHuSolicitado = nombreHu is null ? null : NormalizarTextoOpcional(nombreHu);
            var urlHuSolicitada = urlHu is null ? null : NormalizarTextoOpcional(urlHu);
            var carpetaMediosSolicitada = carpetaMedios is null ? null : NormalizarTextoOpcional(carpetaMedios);
            var nuevoNombreHu = nombreHu is null ? NombreHu : nombreHuSolicitado;
            var nuevaUrlHu = urlHu is null ? UrlHu : urlHuSolicitada;
            var nuevaCarpetaMedios = carpetaMedios is null ? CarpetaMedios : carpetaMediosSolicitada;

            if (!nuevoEsDesarrollo)
            {
                if (nombreHuSolicitado is not null ||
                    urlHuSolicitada is not null ||
                    carpetaMediosSolicitada is not null)
                {
                    throw new InvalidOperationException("No se pueden registrar datos de desarrollo en un ticket que no es de desarrollo.");
                }

                nuevoNombreHu = null;
                nuevaUrlHu = null;
                nuevaCarpetaMedios = null;
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

                if (nuevaCarpetaMedios?.Length > 200)
                    throw new ArgumentException("La carpeta de medios no puede superar 200 caracteres.", nameof(carpetaMedios));
            }

            EsDesarrollo = nuevoEsDesarrollo;
            NombreHu = nuevoNombreHu;
            UrlHu = nuevaUrlHu;
            CarpetaMedios = nuevaCarpetaMedios;
            RegistrarAuditoria(
                idUsuarioActualizacion,
                EsDesarrollo ? "Datos de desarrollo y HU actualizados." : "Ticket marcado como no desarrollo.");
        }

        public void EliminarLogicamente(long idUsuarioActualizacion, Rol rolActualiza, string? comentario)
        {
            ValidarModificable();

            if (rolActualiza != Rol.Planner)
                throw new UnauthorizedAccessException("Solo el Planner puede eliminar tickets.");

            Activo = false;
            FechaEliminacion = DateTimeOffset.UtcNow;
            IdUsuarioEliminacion = idUsuarioActualizacion;
            RegistrarAuditoria(idUsuarioActualizacion, $"Ticket eliminado lógicamente. {comentario}".Trim());
        }

        public long? ObtenerIdResponsable(TipoResponsabilidadTicket tipoResponsabilidad) =>
            Responsables.FirstOrDefault(item => item.IdTipoResponsabilidad == tipoResponsabilidad)?.IdUsuario;

        public bool EsResponsableFuncional(long idUsuario, TipoResponsabilidadTicket tipoResponsabilidad) =>
            ObtenerIdResponsable(tipoResponsabilidad) == idUsuario;

        public bool PuedeConsultar(long idUsuario, Rol rol) =>
            Activo &&
            (EsSupervisor(rol) ||
             IdUsuarioAsignado == idUsuario ||
             EsResponsableFuncional(idUsuario, TipoResponsabilidadTicket.Desarrollo) ||
             EsResponsableFuncional(idUsuario, TipoResponsabilidadTicket.QA) ||
             HistoricoAsignaciones.Any(item => item.IdUsuarioAsignado == idUsuario) ||
             (rol == Rol.QA && TicketWorkflow.EsEstadoAccesibleParaQa(IdEstado)));

        public bool PuedeComentar(long idUsuario, Rol rol) =>
            Activo && IdEstado != TicketEstado.Finalizado &&
            (EsSupervisor(rol) ||
             IdUsuarioAsignado == idUsuario ||
             (rol == Rol.QA && TicketWorkflow.EsEstadoAccesibleParaQa(IdEstado)));

        public bool PuedeEditarDatosDeDesarrollo(long idUsuario, Rol rol) =>
            Activo && IdEstado != TicketEstado.Finalizado &&
            (EsSupervisor(rol) ||
             (rol == Rol.Desarrollador &&
              IdUsuarioAsignado == idUsuario &&
              EsResponsableFuncional(idUsuario, TipoResponsabilidadTicket.Desarrollo)));

        public void AsignarResponsable(
            TipoResponsabilidadTicket tipoResponsabilidad,
            long nuevoIdUsuario,
            long idUsuarioActualizacion,
            Rol rolActualiza,
            string? comentario,
            bool esTransferenciaMasiva = false)
        {
            ValidarModificable();
            if (!EsSupervisor(rolActualiza))
                throw new UnauthorizedAccessException("Solo Planner o Líder Técnico pueden asignar responsables funcionales.");
            if (nuevoIdUsuario <= 0)
                throw new ArgumentException("El ID del responsable debe ser positivo.", nameof(nuevoIdUsuario));

            var responsable = Responsables.FirstOrDefault(item => item.IdTipoResponsabilidad == tipoResponsabilidad);
            var idUsuarioAnterior = responsable?.IdUsuario;
            if (idUsuarioAnterior == nuevoIdUsuario)
                throw new InvalidOperationException("El nuevo responsable debe ser diferente al actual.");
            if (responsable is not null && string.IsNullOrWhiteSpace(comentario))
                throw new ArgumentException("Debe indicar el motivo de la reasignación.", nameof(comentario));

            if (responsable is null)
            {
                Responsables.Add(new ResponsableTicket(
                    IdTicket,
                    tipoResponsabilidad,
                    nuevoIdUsuario,
                    idUsuarioActualizacion));
            }
            else
            {
                responsable.Reemplazar(nuevoIdUsuario, idUsuarioActualizacion);
            }

            if (IdUsuarioAsignado is null || IdUsuarioAsignado == idUsuarioAnterior)
                IdUsuarioAsignado = nuevoIdUsuario;

            var tipoMovimiento = esTransferenciaMasiva
                ? TipoMovimientoAsignacionTicket.TransferenciaMasivaCarga
                : tipoResponsabilidad == TipoResponsabilidadTicket.Desarrollo
                    ? TipoMovimientoAsignacionTicket.ReasignacionDesarrollo
                    : TipoMovimientoAsignacionTicket.ReasignacionQA;

            RegistrarAsignacion(
                idUsuarioAnterior,
                nuevoIdUsuario,
                idUsuarioActualizacion,
                comentario,
                tipoMovimiento);
            RegistrarAuditoria(
                idUsuarioActualizacion,
                $"Responsable de {tipoResponsabilidad} actualizado a {nuevoIdUsuario}. {comentario}".Trim());
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

        private void ValidarModificable()
        {
            ValidarActivo();
            if (IdEstado == TicketEstado.Finalizado)
                throw new InvalidOperationException("Un ticket finalizado no admite modificaciones.");
        }

        private void RegistrarAsignacion(
            long? idUsuarioAnterior,
            long idUsuarioAsignado,
            long idUsuarioAccion,
            string? comentario,
            TipoMovimientoAsignacionTicket tipoMovimiento)
        {
            HistoricoAsignaciones.Add(new HistoricoAsignacionTicket
            {
                IdHistoricoAsignacion = Guid.NewGuid(),
                IdTicket = IdTicket,
                IdUsuarioAnterior = idUsuarioAnterior,
                IdUsuarioAsignado = idUsuarioAsignado,
                IdUsuarioAccion = idUsuarioAccion,
                IdEstado = IdEstado,
                IdTipoMovimiento = tipoMovimiento,
                Comentario = string.IsNullOrWhiteSpace(comentario)
                    ? null
                    : comentario.Trim(),
                FechaAsignacion = DateTimeOffset.UtcNow
            });
        }

        private void ValidarAutorizacionTransicion(
            TicketEstado nuevoEstado,
            long idUsuarioActualizacion,
            Rol rolActualiza)
        {
            if (EsSupervisor(rolActualiza))
                return;

            if (nuevoEstado == TicketEstado.Bloqueado && IdUsuarioAsignado == idUsuarioActualizacion)
                return;

            if (rolActualiza == Rol.Desarrollador)
            {
                ValidarDesarrolladorAsignadoOSupervisor(idUsuarioActualizacion, rolActualiza);
                return;
            }

            if (rolActualiza == Rol.QA)
            {
                if (nuevoEstado == TicketEstado.Bloqueado && IdUsuarioAsignado != idUsuarioActualizacion)
                    throw new UnauthorizedAccessException("Solo el responsable actual puede bloquear el ticket.");

                if (nuevoEstado != TicketEstado.BUG &&
                    !TicketWorkflow.EsEstadoAccesibleParaQa(IdEstado) &&
                    !TicketWorkflow.EsEstadoAccesibleParaQa(nuevoEstado))
                {
                    throw new UnauthorizedAccessException("QA no puede realizar esta transición en el estado actual.");
                }

                return;
            }

            throw new UnauthorizedAccessException("El usuario no puede cambiar el estado de este ticket.");
        }

        private void ValidarResponsableRequerido(TicketEstado nuevoEstado)
        {
            if (RequiereResponsableQa(nuevoEstado) &&
                ObtenerIdResponsable(TipoResponsabilidadTicket.QA) is null)
            {
                throw new InvalidOperationException("QA_NO_ASIGNADO: El ticket requiere un responsable de QA.");
            }

            if (RequiereResponsableDesarrollo(nuevoEstado) &&
                ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo) is null)
            {
                throw new InvalidOperationException("DESARROLLADOR_NO_ASIGNADO: El ticket requiere un responsable de desarrollo.");
            }

            if (IdEstado == TicketEstado.EnReplicaQA &&
                nuevoEstado == TicketEstado.EnAnalisis &&
                ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo) is null)
            {
                throw new InvalidOperationException("DESARROLLADOR_NO_ASIGNADO: El ticket requiere un responsable de desarrollo.");
            }
        }

        private void TransferirCustodiaPorEstado(
            TicketEstado estadoAnterior,
            TicketEstado nuevoEstado,
            long idUsuarioActualizacion,
            string? comentario)
        {
            TipoResponsabilidadTicket? destino = nuevoEstado switch
            {
                TicketEstado.EnReplicaQA or
                TicketEstado.EnRevisionApitesting or
                TicketEstado.EnRevisionQA => TipoResponsabilidadTicket.QA,
                TicketEstado.AprobadoApitesting or
                TicketEstado.Certificado or
                TicketEstado.BUG or
                TicketEstado.Rollback => TipoResponsabilidadTicket.Desarrollo,
                _ when estadoAnterior == TicketEstado.EnReplicaQA && nuevoEstado == TicketEstado.EnAnalisis =>
                    TipoResponsabilidadTicket.Desarrollo,
                _ => null
            };

            if (!destino.HasValue)
                return;

            var nuevoResponsable = ObtenerIdResponsable(destino.Value)!.Value;
            if (IdUsuarioAsignado == nuevoResponsable)
                return;

            var idUsuarioAnterior = IdUsuarioAsignado;
            IdUsuarioAsignado = nuevoResponsable;
            RegistrarAsignacion(
                idUsuarioAnterior,
                nuevoResponsable,
                idUsuarioActualizacion,
                comentario,
                TipoMovimientoAsignacionTicket.TransferenciaAutomaticaEstado);
        }

        private void ValidarDesarrolladorAsignadoOSupervisor(long idUsuario, Rol rol)
        {
            if (EsSupervisor(rol))
                return;
            if (rol != Rol.Desarrollador ||
                IdUsuarioAsignado != idUsuario ||
                !EsResponsableFuncional(idUsuario, TipoResponsabilidadTicket.Desarrollo))
            {
                throw new UnauthorizedAccessException("Solo el desarrollador asignado, Planner o Líder Técnico pueden realizar esta acción.");
            }
        }

        private static bool RequiereResponsableQa(TicketEstado estado) =>
            estado is TicketEstado.EnReplicaQA or
                TicketEstado.EnRevisionApitesting or
                TicketEstado.AprobadoApitesting or
                TicketEstado.EnRevisionQA or
                TicketEstado.AprobadoQA or
                TicketEstado.PendienteCertificacion or
                TicketEstado.Certificado;

        private static bool RequiereResponsableDesarrollo(TicketEstado estado) =>
            estado is TicketEstado.EnProceso or
                TicketEstado.Entregado or
                TicketEstado.DespliegueApitesting or
                TicketEstado.DespligueQA or
                TicketEstado.DespliegueProduccion or
                TicketEstado.BUG or
                TicketEstado.Rollback;

        private static bool EsSupervisor(Rol rol) => rol is Rol.Planner or Rol.LiderTecnico;

        private static string? NormalizarTextoOpcional(string valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
