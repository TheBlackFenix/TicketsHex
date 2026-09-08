namespace TicketsHex.Application.Comun.Configuracion
{
    public sealed class NotificacionesOptions
    {
        public const string SectionName = "Notificaciones";

        public int DiasRetencion { get; set; } = 14;
        public int HorasIntervaloLimpieza { get; set; } = 24;
    }
}
