using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Conocimiento
{
    public sealed class ReferenciaEntradaConocimiento
    {
        public Guid IdReferencia { get; private set; }
        public Guid IdEntrada { get; private set; }
        public TipoReferenciaConocimiento TipoReferencia { get; private set; }
        public string Url { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }

        private ReferenciaEntradaConocimiento() { }

        public ReferenciaEntradaConocimiento(
            Guid idEntrada,
            TipoReferenciaConocimiento tipo,
            string url,
            string? descripcion)
        {
            if (idEntrada == Guid.Empty)
                throw new ArgumentException("La entrada es obligatoria.", nameof(idEntrada));
            if (!Enum.IsDefined(tipo))
                throw new ArgumentOutOfRangeException(nameof(tipo));
            if (string.IsNullOrWhiteSpace(url) ||
                !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("La referencia debe ser una URL absoluta HTTP o HTTPS.", nameof(url));
            }
            if (url.Trim().Length > 2048)
                throw new ArgumentException("La URL no puede superar 2048 caracteres.", nameof(url));
            if (descripcion?.Trim().Length > 300)
                throw new ArgumentException("La descripción no puede superar 300 caracteres.", nameof(descripcion));

            IdReferencia = Guid.NewGuid();
            IdEntrada = idEntrada;
            TipoReferencia = tipo;
            Url = url.Trim();
            Descripcion = Normalizar(descripcion);
        }

        private static string? Normalizar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
