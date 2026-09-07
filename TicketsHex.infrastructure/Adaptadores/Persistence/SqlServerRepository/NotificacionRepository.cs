using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository
{
    public sealed class NotificacionRepository : NotificacionRepositoryBase<MantenimientoContext>
    {
        public NotificacionRepository(MantenimientoContext dbContext)
            : base(dbContext) { }
    }
}
