namespace TicketsHex.Application.DTO_s.Parametro
{
    public sealed record ParametroDTO(
        int Id,
        string Nombre,
        string? Descripcion,
        bool Activo);
}
