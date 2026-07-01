using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IUsuarioActual
    {
        long IdUsuario { get; }
        Rol Rol { get; }
    }
}
