using Microsoft.EntityFrameworkCore;
using TicketsHex.Application.DTO_s.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    public sealed class NotificacionRepository : INotificacionRepository
    {
        private readonly MantenimientoContext _dbContext;

        public NotificacionRepository(MantenimientoContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinHuAsync() =>
            await TicketsDesarrollo()
                .Where(ticket =>
                    string.IsNullOrWhiteSpace(ticket.NombreHu) ||
                    string.IsNullOrWhiteSpace(ticket.UrlHu))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosAsync() =>
            await TicketsDesarrollo()
                .Where(ticket => string.IsNullOrWhiteSpace(ticket.CarpetaMedios))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinRamasAsync() =>
            await TicketsDesarrollo()
                .Where(ticket => !_dbContext.RamasTicket.Any(rama => rama.IdTicket == ticket.IdTicket))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        public async Task<IReadOnlyCollection<TicketNotificacionDTO>> ObtenerTicketsDesarrolloSinCarpetaMediosORamasAsync() =>
            await TicketsDesarrollo()
                .Where(ticket =>
                    string.IsNullOrWhiteSpace(ticket.CarpetaMedios) ||
                    !_dbContext.RamasTicket.Any(rama => rama.IdTicket == ticket.IdTicket))
                .OrderByDescending(ticket => ticket.FechaAsignacion)
                .Select(ticket => new TicketNotificacionDTO(
                    ticket.IdTicket,
                    ticket.CodigoCaso.Valor,
                    ticket.Titulo.Value))
                .ToArrayAsync();

        private IQueryable<Domain.Entidades.Ticket.Ticket> TicketsDesarrollo() =>
            _dbContext.Tickets
                .AsNoTracking()
                .Where(ticket => ticket.Activo && ticket.EsDesarrollo);
    }
}
