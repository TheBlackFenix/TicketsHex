namespace TicketsHex.Application.Puertos.Salida
{
    public interface INotificacionPublisher
    {
        Task PublicarResumenAsync();
    }
}
