namespace TicketsHex.Domain.Entidades.ConfiguracionGit
{
    public sealed class RamaTicket
    {
        public Guid IdRamaTicket { get; private set; }
        public Guid IdTicket { get; private set; }
        public Guid IdRama { get; private set; }
        public DateTimeOffset FechaAsignacion { get; private set; }

        private RamaTicket() { }

        public RamaTicket(Guid idTicket, Guid idRama)
        {
            if (idTicket == Guid.Empty)
                throw new ArgumentException("El ticket es obligatorio.");
            if (idRama == Guid.Empty)
                throw new ArgumentException("La rama es obligatoria.");

            IdRamaTicket = Guid.NewGuid();
            IdTicket = idTicket;
            IdRama = idRama;
            FechaAsignacion = DateTimeOffset.UtcNow;
        }
    }
}
