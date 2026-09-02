using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Usuario
{
    public sealed record UsuarioDTO(
        long IdUsuario,
        string NombreUsuario,
        string Nombres,
        string? Apellidos,
        Rol Rol,
        Area? IdArea,
        string? ImagenPerfilBase64,
        bool Activo,
        bool Bloqueado,
        int IntentosFallidos,
        DateTimeOffset? FechaBloqueo,
        DateTimeOffset? ContrasenaExpiraEn);
}
