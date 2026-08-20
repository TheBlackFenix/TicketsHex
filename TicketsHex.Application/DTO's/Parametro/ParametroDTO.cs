namespace TicketsHex.Application.DTO_s.Parametro
{
    public sealed record ParametroDTO(
        int Id,
        string Nombre,
        string? Descripcion,
        bool Activo);

    public sealed record ResultadoEntradaParametroDTO(
        int Id,
        int IdTipoEntrada,
        string Nombre,
        string? Descripcion,
        bool Activo);
}
