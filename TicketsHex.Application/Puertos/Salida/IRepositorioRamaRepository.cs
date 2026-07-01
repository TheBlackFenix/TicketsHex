using TicketsHex.Domain.Entidades.ConfiguracionGit;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IRepositorioRamaRepository
    {
        Task<IReadOnlyCollection<Repositorio>> ObtenerRepositoriosAsync();
        Task<Repositorio?> ObtenerRepositorioAsync(Guid idRepositorio);
        Task<Repositorio?> ObtenerRepositorioPorNombreAsync(string nombre);
        Task<Rama?> ObtenerRamaAsync(Guid idRama);
        Task<Rama?> ObtenerRamaPorNombreAsync(Guid idRepositorio, string nombre);
        Task<IReadOnlyCollection<RamaTicket>> ObtenerAsignacionesTicketAsync(Guid idTicket);
        Task<bool> ExisteAsignacionAsync(Guid idTicket, Guid idRama);
        Task GuardarRepositorioAsync(Repositorio repositorio);
        Task GuardarRamaAsync(Rama rama);
        Task GuardarAsignacionAsync(RamaTicket asignacion);
        Task EliminarAsignacionAsync(Guid idTicket, Guid idRama);
    }
}
