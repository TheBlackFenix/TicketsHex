using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;

namespace TicketsHex.infrastructure.Adaptadores.Persistence
{
    internal static class HistorialAsignacionTicketRepositoryBase
    {
        public static Task<Ticket?> ObtenerTicketParaValidarAccesoAsync(
            IQueryable<Ticket> tickets,
            Guid idTicket,
            long idUsuario,
            bool incluirEliminados) =>
            tickets
                .AsNoTracking()
                .Include(ticket => ticket.Responsables)
                .Include(ticket => ticket.HistoricoAsignaciones.Where(
                    movimiento => movimiento.IdUsuarioAsignado == idUsuario))
                .AsSplitQuery()
                .Where(ticket => incluirEliminados || ticket.Activo)
                .FirstOrDefaultAsync(ticket => ticket.IdTicket == idTicket);

        public static async Task<PaginaResultado<HistorialAsignacionTicketDTO>> ObtenerPaginaAsync(
            IQueryable<HistoricoAsignacionTicket> historicos,
            IQueryable<Usuario> usuarios,
            Guid idTicket,
            HistorialAsignacionTicketFiltroRequest filtro)
        {
            var movimientosTicket = historicos
                .AsNoTracking()
                .Where(movimiento => movimiento.IdTicket == idTicket);
            var total = await movimientosTicket.CountAsync();

            var consulta =
                from movimiento in movimientosTicket
                join usuarioNuevo in usuarios.AsNoTracking()
                    on movimiento.IdUsuarioAsignado equals usuarioNuevo.IdUsuario
                join usuarioAccion in usuarios.AsNoTracking()
                    on movimiento.IdUsuarioAccion equals usuarioAccion.IdUsuario
                join usuarioAnterior in usuarios.AsNoTracking()
                    on movimiento.IdUsuarioAnterior equals (long?)usuarioAnterior.IdUsuario
                    into usuariosAnteriores
                from usuarioAnterior in usuariosAnteriores.DefaultIfEmpty()
                orderby movimiento.FechaAsignacion descending,
                    movimiento.IdHistoricoAsignacion descending
                select new
                {
                    Movimiento = movimiento,
                    AnteriorId = usuarioAnterior == null ? (long?)null : usuarioAnterior.IdUsuario,
                    AnteriorUsuario = usuarioAnterior == null ? null : usuarioAnterior.NombreUsuario,
                    AnteriorNombres = usuarioAnterior == null ? null : usuarioAnterior.Nombres,
                    AnteriorApellidos = usuarioAnterior == null ? null : usuarioAnterior.Apellidos,
                    NuevoId = usuarioNuevo.IdUsuario,
                    NuevoUsuario = usuarioNuevo.NombreUsuario,
                    NuevoNombres = usuarioNuevo.Nombres,
                    NuevoApellidos = usuarioNuevo.Apellidos,
                    AccionId = usuarioAccion.IdUsuario,
                    AccionUsuario = usuarioAccion.NombreUsuario,
                    AccionNombres = usuarioAccion.Nombres,
                    AccionApellidos = usuarioAccion.Apellidos
                };

            var filas = await consulta
                .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
                .Take(filtro.TamanoPagina)
                .ToListAsync();

            var elementos = filas.Select(fila => new HistorialAsignacionTicketDTO(
                fila.Movimiento.IdHistoricoAsignacion,
                fila.AnteriorId.HasValue
                    ? new UsuarioReferenciaTicketDTO(
                        fila.AnteriorId.Value,
                        ObtenerNombreVisible(
                            fila.AnteriorNombres,
                            fila.AnteriorApellidos,
                            fila.AnteriorUsuario!))
                    : null,
                new UsuarioReferenciaTicketDTO(
                    fila.NuevoId,
                    ObtenerNombreVisible(
                        fila.NuevoNombres,
                        fila.NuevoApellidos,
                        fila.NuevoUsuario)),
                new UsuarioReferenciaTicketDTO(
                    fila.AccionId,
                    ObtenerNombreVisible(
                        fila.AccionNombres,
                        fila.AccionApellidos,
                        fila.AccionUsuario)),
                fila.Movimiento.IdEstado,
                fila.Movimiento.IdTipoMovimiento,
                fila.Movimiento.Comentario,
                fila.Movimiento.FechaAsignacion))
                .ToArray();

            return new PaginaResultado<HistorialAsignacionTicketDTO>(
                elementos,
                filtro.Pagina,
                filtro.TamanoPagina,
                total);
        }

        private static string ObtenerNombreVisible(
            string? nombres,
            string? apellidos,
            string nombreUsuario)
        {
            var nombreCompleto = $"{nombres} {apellidos}".Trim();
            return string.IsNullOrWhiteSpace(nombreCompleto)
                ? nombreUsuario
                : nombreCompleto;
        }
    }
}
