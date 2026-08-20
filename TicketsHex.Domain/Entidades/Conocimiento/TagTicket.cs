namespace TicketsHex.Domain.Entidades.Conocimiento
{
    public sealed class TagTicket
    {
        public Guid IdTagTicket { get; private set; }
        public Guid IdTicket { get; private set; }
        public Guid IdTag { get; private set; }
        public DateTimeOffset FechaAsignacion { get; private set; }

        private TagTicket() { }

        public TagTicket(Guid idTicket, Guid idTag)
        {
            if (idTicket == Guid.Empty)
                throw new ArgumentException("El ticket es obligatorio.", nameof(idTicket));
            if (idTag == Guid.Empty)
                throw new ArgumentException("El tag es obligatorio.", nameof(idTag));

            IdTagTicket = Guid.NewGuid();
            IdTicket = idTicket;
            IdTag = idTag;
            FechaAsignacion = DateTimeOffset.UtcNow;
        }
    }
}
