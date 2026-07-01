namespace TicketsHex.Domain.Entidades.ConfiguracionGit
{
    public sealed class Repositorio
    {
        private readonly List<Rama> _ramas = [];

        public Guid IdRepositorio { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string? Link { get; private set; }
        public string? Descripcion { get; private set; }
        public IReadOnlyCollection<Rama> Ramas => _ramas.AsReadOnly();

        private Repositorio() { }

        public Repositorio(string nombre, string? link, string? descripcion)
        {
            IdRepositorio = Guid.NewGuid();
            Nombre = ValidarTexto(nombre, 100, "El nombre del repositorio");
            Link = ValidarLink(link);
            Descripcion = ValidarTextoOpcional(descripcion, 500, "La descripción");
        }

        public Rama CrearRama(string nombre)
        {
            var rama = new Rama(IdRepositorio, nombre);
            _ramas.Add(rama);
            return rama;
        }

        private static string ValidarTexto(string valor, int longitudMaxima, string campo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException($"{campo} es obligatorio.");

            var valorNormalizado = valor.Trim();
            if (valorNormalizado.Length > longitudMaxima)
                throw new ArgumentException($"{campo} no puede superar {longitudMaxima} caracteres.");

            return valorNormalizado;
        }

        private static string? ValidarTextoOpcional(
            string? valor,
            int longitudMaxima,
            string campo)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return null;

            return ValidarTexto(valor, longitudMaxima, campo);
        }

        private static string? ValidarLink(string? link)
        {
            var valor = ValidarTextoOpcional(link, 255, "El enlace");
            if (valor is not null &&
                (!Uri.TryCreate(valor, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            {
                throw new ArgumentException("El enlace del repositorio debe ser una URL HTTP o HTTPS válida.");
            }

            return valor;
        }
    }
}
