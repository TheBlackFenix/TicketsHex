using TicketsHex.Application.DTO_s.Repositorio;

namespace TicketsHex.Application.Puertos.Entrada.Repositorio
{
    public interface IRepositorioRamaService
    {
        Task<IReadOnlyCollection<RepositorioDTO>> ObtenerRepositoriosAsync();
        Task<IReadOnlyCollection<RamaDTO>> ObtenerRamasAsync(Guid idRepositorio);
        Task<IReadOnlyCollection<RamaTicketDTO>> ObtenerRamasTicketAsync(Guid idTicket);
        Task<Guid> CrearRepositorioAsync(CrearRepositorioRequest request);
        Task<Guid> CrearRamaAsync(Guid idRepositorio, CrearRamaRequest request);
        Task<Guid> AsignarRamaAsync(Guid idTicket, AsignarRamaTicketRequest request);
        Task DesasignarRamaAsync(Guid idTicket, Guid idRama);
    }
}
