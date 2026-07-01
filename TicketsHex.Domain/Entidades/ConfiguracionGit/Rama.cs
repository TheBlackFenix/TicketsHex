namespace TicketsHex.Domain.Entidades.ConfiguracionGit
{
    public sealed class Rama
    {
        public Guid IdRama { get; private set; }
        public Guid IdRepositorio { get; private set; }
        public string NombreRama { get; private set; } = string.Empty;
        public DateTimeOffset FechaCreacion { get; private set; }

        private Rama() { }

        internal Rama(Guid idRepositorio, string nombre)
        {
            if (idRepositorio == Guid.Empty)
                throw new ArgumentException("El repositorio es obligatorio.");
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre de la rama es obligatorio.");

            var nombreNormalizado = nombre.Trim();
            if (nombreNormalizado.Length > 150)
                throw new ArgumentException("El nombre de la rama no puede superar 150 caracteres.");

            IdRama = Guid.NewGuid();
            IdRepositorio = idRepositorio;
            NombreRama = nombreNormalizado;
            FechaCreacion = DateTimeOffset.UtcNow;
        }
    }
}
