namespace TicketsHex.Application.DTO_s.Repositorio
{
    public sealed record RepositorioDTO(
        Guid IdRepositorio,
        string Nombre,
        string? Link,
        string? Descripcion);

    public sealed record RamaDTO(
        Guid IdRama,
        Guid IdRepositorio,
        string Nombre,
        DateTimeOffset FechaCreacion);

    public sealed record RamaTicketDTO(
        Guid IdRamaTicket,
        Guid IdTicket,
        Guid IdRepositorio,
        string Repositorio,
        Guid IdRama,
        string Rama,
        DateTimeOffset FechaAsignacion);

    public sealed record CrearRepositorioRequest(
        string Nombre,
        string? Link,
        string? Descripcion);

    public sealed record CrearRamaRequest(string Nombre);

    public sealed record AsignarRamaTicketRequest(
        Guid IdRepositorio,
        Guid IdRama);
}
