using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Autenticacion
{
    public sealed record LoginRequest(
        string NombreUsuario,
        string Contrasena);

    public sealed record LoginResponse(
        string Token,
        DateTimeOffset FechaExpiracion,
        UsuarioAutenticadoDTO Usuario);

    public sealed record UsuarioAutenticadoDTO(
        long IdUsuario,
        string NombreUsuario,
        string Nombres,
        Rol Rol,
        Area? Area);

    public sealed record CambiarContrasenaRequest(
        string NombreUsuario,
        string ContrasenaActual,
        string NuevaContrasena);

    public sealed record InicializarAutenticacionRequest(
        long IdUsuario,
        string NombreUsuario,
        string Nombres,
        string? Apellidos,
        Area? IdArea,
        string Contrasena);
}
