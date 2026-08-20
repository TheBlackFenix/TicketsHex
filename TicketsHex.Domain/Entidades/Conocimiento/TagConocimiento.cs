namespace TicketsHex.Domain.Entidades.Conocimiento
{
    public sealed class TagConocimiento
    {
        public Guid IdTag { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string NombreNormalizado { get; private set; } = string.Empty;
        public bool Activo { get; private set; }

        private TagConocimiento() { }

        public TagConocimiento(string nombre)
        {
            var nombreLimpio = LimpiarNombre(nombre);
            IdTag = Guid.NewGuid();
            Nombre = nombreLimpio;
            NombreNormalizado = NormalizarNombre(nombreLimpio);
            Activo = true;
        }

        public static string NormalizarNombre(string nombre) =>
            LimpiarNombre(nombre).ToUpperInvariant();

        private static string LimpiarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del tag es obligatorio.", nameof(nombre));

            var nombreLimpio = nombre.Trim();
            if (nombreLimpio.Length > 50)
                throw new ArgumentException("El nombre del tag no puede superar 50 caracteres.", nameof(nombre));

            return nombreLimpio;
        }
    }
}
