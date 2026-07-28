using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Usuario
{
    public sealed record CrearUsuarioRequest(
        long IdUsuario,
        string NombreUsuario,
        string Nombres,
        string? Apellidos,
        Rol Rol,
        Area? IdArea,
        string? ImagenPerfilBase64 = null);

    public sealed record ActualizarUsuarioRequest(
        string NombreUsuario,
        string Nombres,
        string? Apellidos,
        Rol Rol,
        Area? IdArea,
        bool Activo,
        string? ImagenPerfilBase64 = null);
}
