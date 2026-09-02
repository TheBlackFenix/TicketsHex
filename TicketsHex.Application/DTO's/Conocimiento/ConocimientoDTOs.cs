using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Application.DTO_s.Conocimiento
{
    public sealed record ReferenciaConocimientoRequest(
        TipoReferenciaConocimiento Tipo,
        string Url,
        string? Descripcion);

    public sealed record CrearDiagnosticoRequest(
        int IdResultado,
        string Resumen,
        string? Sintomas = null,
        string? Comprobaciones = null,
        string? PasosReproduccion = null,
        int? IdAmbiente = null,
        IReadOnlyCollection<Guid>? IdsAplicativos = null,
        IReadOnlyCollection<string>? Tags = null,
        IReadOnlyCollection<ReferenciaConocimientoRequest>? Referencias = null);

    public sealed record CrearSolucionRequest(
        int IdResultado,
        string Resumen,
        bool? RequiereDespliegue = null,
        string? Observaciones = null,
        IReadOnlyCollection<ReferenciaConocimientoRequest>? Referencias = null);

    public sealed record CrearValidacionQaRequest(
        int IdResultado,
        string Resumen,
        string? Sintomas = null,
        string? Comprobaciones = null,
        int? IdAmbiente = null,
        string? Observaciones = null,
        IReadOnlyCollection<ReferenciaConocimientoRequest>? Referencias = null);

    public sealed record ActualizarEntradaConocimientoRequest(
        int IdResultado,
        string Resumen,
        string? Sintomas = null,
        string? Comprobaciones = null,
        string? PasosReproduccion = null,
        int? IdAmbiente = null,
        bool? RequiereDespliegue = null,
        string? Observaciones = null,
        IReadOnlyCollection<Guid>? IdsAplicativos = null,
        IReadOnlyCollection<string>? Tags = null,
        IReadOnlyCollection<ReferenciaConocimientoRequest>? Referencias = null);

    public sealed record ReferenciaConocimientoDTO(
        Guid IdReferencia,
        TipoReferenciaConocimiento Tipo,
        string Url,
        string? Descripcion);

    public sealed record EntradaConocimientoDTO(
        Guid IdEntrada,
        Guid IdTicket,
        TipoEntradaConocimiento Tipo,
        int IdResultado,
        string Resumen,
        string? Sintomas,
        string? Comprobaciones,
        string? PasosReproduccion,
        int? IdAmbiente,
        bool? RequiereDespliegue,
        string? Observaciones,
        long IdUsuarioAutor,
        Rol RolAutor,
        DateTimeOffset FechaCreacion,
        DateTimeOffset? FechaUltimaActualizacion,
        IReadOnlyCollection<ReferenciaConocimientoDTO> Referencias);

    public sealed record BaseConocimientoTicketDTO(
        Guid IdTicket,
        IReadOnlyCollection<string> Tags,
        IReadOnlyCollection<Guid> IdsAplicativos,
        IReadOnlyCollection<EntradaConocimientoDTO> Entradas);

    public sealed record RevisionEntradaConocimientoDTO(
        Guid IdRevision,
        Guid IdEntrada,
        string ContenidoAnterior,
        long IdUsuarioAccion,
        Rol RolUsuarioAccion,
        TicketEstado EstadoTicket,
        DateTimeOffset FechaRevision);

    public sealed record ConocimientoFiltroRequest(
        int Pagina = 1,
        int TamanoPagina = 20,
        string? Texto = null,
        Guid? IdAplicativo = null,
        string? Tag = null,
        TipoEntradaConocimiento? Tipo = null,
        int? IdResultado = null,
        int? IdAmbiente = null)
    {
        public ConocimientoFiltroRequest Normalizar() => this with
        {
            Pagina = Pagina < 1 ? 1 : Pagina,
            TamanoPagina = Math.Clamp(TamanoPagina, 1, 100),
            Texto = string.IsNullOrWhiteSpace(Texto) ? null : Texto.Trim(),
            Tag = string.IsNullOrWhiteSpace(Tag) ? null : Tag.Trim()
        };
    }

    public sealed record BusquedaConocimientoDTO(
        PaginaResultado<EntradaConocimientoDTO> Resultado);
}
