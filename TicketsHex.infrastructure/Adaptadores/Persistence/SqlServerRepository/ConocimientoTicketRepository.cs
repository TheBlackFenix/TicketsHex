using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    internal sealed class ConocimientoTicketRepository
        : ConocimientoTicketRepositoryBase<MantenimientoContext>
    {
        public ConocimientoTicketRepository(MantenimientoContext dbContext) : base(dbContext) { }
    }
}
