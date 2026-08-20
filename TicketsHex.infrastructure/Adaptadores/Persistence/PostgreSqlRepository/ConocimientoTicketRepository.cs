using TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository
{
    internal sealed class ConocimientoTicketRepository
        : ConocimientoTicketRepositoryBase<MantenimientoContext>
    {
        public ConocimientoTicketRepository(MantenimientoContext dbContext) : base(dbContext) { }
    }
}
