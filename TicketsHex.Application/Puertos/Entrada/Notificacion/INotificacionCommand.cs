namespace TicketsHex.Application.Puertos.Entrada.Notificacion
{
    public interface INotificacionCommand
    {
        Task MarcarLeidaAsync(Guid idNotificacion);
        Task<int> MarcarTodasLeidasAsync();
    }
}
