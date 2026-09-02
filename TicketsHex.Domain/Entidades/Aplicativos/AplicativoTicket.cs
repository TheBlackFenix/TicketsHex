namespace TicketsHex.Domain.Entidades.Aplicativos
{
    public sealed class AplicativoTicket
    {
        public Guid IdAplicativoTicket { get; private set; }
        public Guid IdTicket { get; private set; }
        public Guid IdAplicativo { get; private set; }
        public DateTimeOffset FechaAsignacion { get; private set; }

        private AplicativoTicket() { }

        public AplicativoTicket(Guid idTicket, Guid idAplicativo)
        {
            if (idTicket == Guid.Empty)
                throw new ArgumentException("El ticket es obligatorio.", nameof(idTicket));
            if (idAplicativo == Guid.Empty)
                throw new ArgumentException("El aplicativo es obligatorio.", nameof(idAplicativo));

            IdAplicativoTicket = Guid.NewGuid();
            IdTicket = idTicket;
            IdAplicativo = idAplicativo;
            FechaAsignacion = DateTimeOffset.UtcNow;
        }
    }
}
