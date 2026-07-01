using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.Puertos.Salida
{
    public interface IGeneradorJwtSesion
    {
        TokenJwtGenerado Generar(
            long idUsuario,
            string nombreUsuario,
            Rol rol,
            string jti,
            DateTimeOffset fechaCreacion);
    }

    public sealed record TokenJwtGenerado(
        string Token,
        DateTimeOffset FechaExpiracion);
}
