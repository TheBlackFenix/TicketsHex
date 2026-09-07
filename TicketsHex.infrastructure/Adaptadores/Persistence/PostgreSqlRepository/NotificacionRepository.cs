using TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository
{
    public sealed class NotificacionRepository : NotificacionRepositoryBase<MantenimientoContext>
    {
        public NotificacionRepository(MantenimientoContext dbContext)
            : base(dbContext) { }
    }
}
