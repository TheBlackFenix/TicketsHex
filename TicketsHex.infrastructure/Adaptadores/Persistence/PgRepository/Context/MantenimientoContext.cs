using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.ValueObjects.Ticket;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.SqliteRepository.Context
{
    public class MantenimientoContext : DbContext
    {
        public MantenimientoContext(DbContextOptions<MantenimientoContext> options) : base(options) { }

        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<HistoricoEstadosTicket> HistoricoEstados => Set<HistoricoEstadosTicket>();
        //public DbSet<Usuario> Usuarios => Set<Usuario>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuramos el esquema por defecto si decidiste usar uno personalizado
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<Ticket>(b =>
            {
                b.ToTable("tickets");
                b.HasKey(t => t.IdTicket);

                // Conversión limpia de los Value Objects a columnas VARCHAR de Postgres
                b.Property(t => t.CodigoCaso)
                    .HasColumnName("IdCaso")
                    .HasConversion(vo => vo.Valor, value => new CodigoCasoVO(value))
                    .IsRequired();

                b.Property(t => t.Titulo)
                    .HasConversion(vo => vo.Value, value => new TituloVO(value))
                    .IsRequired();

                b.Property(t => t.Descripcion)
                    .HasConversion(vo => vo.Value, value => new DescripcionVO(value))
                    .IsRequired();

                // Mapeo del Enum como entero
                b.Property(t => t.IdEstado).HasColumnName("IdEstado").HasConversion<int>();

                // Relación clásica de EF Core: 1 ticket tiene muchos históricos
                b.HasMany(t => t.HistoricoEstados)
                    .WithOne()
                    .HasForeignKey(h => h.IdTicket)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<HistoricoEstadosTicket>(b =>
            {
                b.ToTable("historicoestadosticket");
                b.HasKey(h => h.IdHistorico);
                b.Property(h => h.IdEstadoOrigen).HasConversion<int?>();
                b.Property(h => h.IdEstadoDestino).HasConversion<int>();
            });

            //modelBuilder.Entity<Usuario>(b =>
            //{
            //    b.ToTable("Usuarios");
            //    b.HasKey(u => u.IdUsuario);
            //});
            // Convertir automáticamente todos los nombres de columnas a minúsculas
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    // Esto le quita el CamelCase y lo deja todo en minúsculas para Postgres
                    property.SetColumnName(property.Name.ToLowerInvariant());
                    // Nota: Si .ToLowerScalar() no te aparece, puedes usar .ToLower()
                }
            }
        }
    }
}
