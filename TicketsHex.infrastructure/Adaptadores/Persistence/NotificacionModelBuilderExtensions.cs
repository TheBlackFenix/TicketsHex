using Microsoft.EntityFrameworkCore;
using TicketsHex.Domain.Entidades.Notificacion;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;

namespace TicketsHex.infrastructure.Adaptadores.Persistence
{
    internal static class NotificacionModelBuilderExtensions
    {
        public static void ConfigurarNotificaciones(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NotificacionUsuario>(b =>
            {
                b.ToTable("notificacionesusuario");
                b.HasKey(item => item.IdNotificacion);
                b.Property(item => item.IdNotificacion).ValueGeneratedNever();
                b.Property(item => item.IdTipoNotificacion).HasConversion<int>();
                b.Property(item => item.Mensaje).HasMaxLength(250).IsRequired();
                b.Ignore(item => item.Leida);
                b.HasIndex(item => new { item.IdUsuarioDestinatario, item.FechaCreacion });
                b.HasIndex(item => new
                {
                    item.IdUsuarioDestinatario,
                    item.FechaLectura,
                    item.FechaExpiracion
                });
                b.HasIndex(item => item.IdTicket);
                b.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(item => item.IdUsuarioDestinatario)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne<Ticket>()
                    .WithMany()
                    .HasForeignKey(item => item.IdTicket)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
