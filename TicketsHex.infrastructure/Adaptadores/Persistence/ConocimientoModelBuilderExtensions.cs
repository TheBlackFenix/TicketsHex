using Microsoft.EntityFrameworkCore;
using TicketsHex.Domain.Entidades.Aplicativos;
using TicketsHex.Domain.Entidades.Conocimiento;
using TicketsHex.Domain.Entidades.Parametros;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;

namespace TicketsHex.infrastructure.Adaptadores.Persistence
{
    internal static class ConocimientoModelBuilderExtensions
    {
        public static void ConfigurarConocimiento(this ModelBuilder modelBuilder, bool esSqlServer)
        {
            modelBuilder.Entity<TipoEntradaConocimientoParametro>(b =>
            {
                b.ToTable("tiposentradaconocimiento");
                b.HasKey(item => item.IdTipoEntrada);
                b.Property(item => item.Nombre).HasMaxLength(50).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(200);
            });

            modelBuilder.Entity<ResultadoEntradaConocimientoParametro>(b =>
            {
                b.ToTable("resultadosentradaconocimiento");
                b.HasKey(item => item.IdResultado);
                b.Property(item => item.Nombre).HasMaxLength(50).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(200);
                b.HasIndex(item => new { item.IdTipoEntrada, item.Nombre }).IsUnique();
                b.HasOne<TipoEntradaConocimientoParametro>()
                    .WithMany()
                    .HasForeignKey(item => item.IdTipoEntrada)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AmbienteTicketParametro>(b =>
            {
                b.ToTable("ambientesticket");
                b.HasKey(item => item.IdAmbiente);
                b.Property(item => item.Nombre).HasMaxLength(50).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(200);
            });

            modelBuilder.Entity<EntradaConocimientoTicket>(b =>
            {
                b.ToTable("entradasconocimientoticket");
                b.HasKey(item => item.IdEntrada);
                b.Property(item => item.IdEntrada).ValueGeneratedNever();
                b.Property(item => item.IdTipoEntrada)
                    .HasColumnName("idtipoentrada")
                    .HasConversion<int>();
                b.Property(item => item.Resumen).HasMaxLength(2000).IsRequired();
                b.Property(item => item.Sintomas).HasMaxLength(2000);
                b.Property(item => item.Comprobaciones).HasMaxLength(4000);
                b.Property(item => item.PasosReproduccion).HasMaxLength(4000);
                b.Property(item => item.Observaciones).HasMaxLength(2000);
                b.Property(item => item.IdRolAutor)
                    .HasColumnName("idrolautor")
                    .HasConversion<int>();
                b.HasIndex(item => new { item.IdTicket, item.FechaCreacion });
                b.HasIndex(item => new { item.IdTipoEntrada, item.IdResultado });
                b.HasOne<Ticket>()
                    .WithMany()
                    .HasForeignKey(item => item.IdTicket)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne<ResultadoEntradaConocimientoParametro>()
                    .WithMany()
                    .HasForeignKey(item => item.IdResultado)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne<AmbienteTicketParametro>()
                    .WithMany()
                    .HasForeignKey(item => item.IdAmbiente)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(item => item.IdUsuarioAutor)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasMany(item => item.Referencias)
                    .WithOne()
                    .HasForeignKey(item => item.IdEntrada)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasMany(item => item.Revisiones)
                    .WithOne()
                    .HasForeignKey(item => item.IdEntrada)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReferenciaEntradaConocimiento>(b =>
            {
                b.ToTable("referenciasentradaconocimiento");
                b.HasKey(item => item.IdReferencia);
                b.Property(item => item.IdReferencia).ValueGeneratedNever();
                b.Property(item => item.Tipo)
                    .HasColumnName("tiporeferencia")
                    .HasConversion<int>();
                b.Property(item => item.Url).HasMaxLength(2048).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(300);
            });

            modelBuilder.Entity<RevisionEntradaConocimiento>(b =>
            {
                b.ToTable("revisionesentradaconocimiento");
                b.HasKey(item => item.IdRevision);
                b.Property(item => item.IdRevision).ValueGeneratedNever();
                b.Property(item => item.ContenidoAnterior)
                    .HasColumnType(esSqlServer ? "varchar(max)" : "text")
                    .IsRequired();
                b.Property(item => item.RolUsuarioAccion)
                    .HasColumnName("idrolusuarioaccion")
                    .HasConversion<int>();
                b.Property(item => item.EstadoTicket)
                    .HasColumnName("idestadoticket")
                    .HasConversion<int>();
                b.HasIndex(item => new { item.IdEntrada, item.FechaRevision });
                b.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(item => item.IdUsuarioAccion)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TagConocimiento>(b =>
            {
                b.ToTable("tags");
                b.HasKey(item => item.IdTag);
                b.Property(item => item.IdTag).ValueGeneratedNever();
                b.Property(item => item.Nombre).HasMaxLength(50).IsRequired();
                b.Property(item => item.NombreNormalizado).HasMaxLength(50).IsRequired();
                b.HasIndex(item => item.NombreNormalizado).IsUnique();
            });

            modelBuilder.Entity<TagTicket>(b =>
            {
                b.ToTable("tagsticket");
                b.HasKey(item => item.IdTagTicket);
                b.Property(item => item.IdTagTicket).ValueGeneratedNever();
                b.HasIndex(item => new { item.IdTicket, item.IdTag }).IsUnique();
                b.HasOne<Ticket>()
                    .WithMany()
                    .HasForeignKey(item => item.IdTicket)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne<TagConocimiento>()
                    .WithMany()
                    .HasForeignKey(item => item.IdTag)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
