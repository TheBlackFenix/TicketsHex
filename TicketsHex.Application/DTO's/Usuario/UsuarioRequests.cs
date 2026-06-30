using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Usuario
{
    public sealed record CrearUsuarioRequest(
        long IdUsuario,
        string NombreUsuario,
        string Nombres,
        string? Apellidos,
        Rol Rol,
        Area? IdArea);

    public sealed record ActualizarUsuarioRequest(
        string NombreUsuario,
        string Nombres,
        string? Apellidos,
        Rol Rol,
        Area? IdArea,
        bool Activo);
}
