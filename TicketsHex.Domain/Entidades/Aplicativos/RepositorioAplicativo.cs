namespace TicketsHex.Domain.Entidades.Aplicativos
{
    public sealed class RepositorioAplicativo
    {
        public Guid IdRepositorioAplicativo { get; private set; }
        public Guid IdRepositorio { get; private set; }
        public Guid IdAplicativo { get; private set; }

        private RepositorioAplicativo() { }

        public RepositorioAplicativo(Guid idRepositorio, Guid idAplicativo)
        {
            if (idRepositorio == Guid.Empty)
                throw new ArgumentException("El repositorio es obligatorio.", nameof(idRepositorio));
            if (idAplicativo == Guid.Empty)
                throw new ArgumentException("El aplicativo es obligatorio.", nameof(idAplicativo));

            IdRepositorioAplicativo = Guid.NewGuid();
            IdRepositorio = idRepositorio;
            IdAplicativo = idAplicativo;
        }
    }
}
