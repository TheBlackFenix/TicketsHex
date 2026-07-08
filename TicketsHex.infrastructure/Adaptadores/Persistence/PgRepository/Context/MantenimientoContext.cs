using Microsoft.EntityFrameworkCore;
using TicketsHex.Domain.Entidades.ConfiguracionGit;
using TicketsHex.Domain.Entidades.Parametros;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.ValueObjects.Ticket;

namespace TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository.Context
{
    public class MantenimientoContext : DbContext
    {
        public MantenimientoContext(DbContextOptions<MantenimientoContext> options)
            : base(options) { }

        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<HistoricoEstadosTicket> HistoricoEstados => Set<HistoricoEstadosTicket>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<RolParametro> Roles => Set<RolParametro>();
        public DbSet<EstadoTicketParametro> EstadosTicket => Set<EstadoTicketParametro>();
        public DbSet<OrigenTicketParametro> OrigenesTicket => Set<OrigenTicketParametro>();
        public DbSet<AreaTicketParametro> AreasTicket => Set<AreaTicketParametro>();
        public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
        public DbSet<Repositorio> Repositorios => Set<Repositorio>();
        public DbSet<Rama> Ramas => Set<Rama>();
        public DbSet<RamaTicket> RamasTicket => Set<RamaTicket>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<Ticket>(b =>
            {
                b.ToTable("tickets");
                b.HasKey(t => t.IdTicket);

                b.Property(t => t.CodigoCaso)
                    .HasConversion(vo => vo.Valor, value => new CodigoCasoVO(value))
                    .HasMaxLength(20)
                    .IsRequired();

                b.Property(t => t.Titulo)
                    .HasConversion(vo => vo.Value, value => new TituloVO(value))
                    .HasMaxLength(100)
                    .IsRequired();

                b.Property(t => t.Descripcion)
                    .HasConversion(vo => vo.Value, value => new DescripcionVO(value))
                    .HasMaxLength(500)
                    .IsRequired();

                b.Property(t => t.IdOrigen).HasConversion<int>();
                b.Property(t => t.IdEstado).HasConversion<int>();
                b.Property(t => t.CausaRaiz).HasMaxLength(1000);
                b.Property(t => t.SolucionPropuesta).HasMaxLength(1000);
                b.Property(t => t.EsDesarrollo)
                    .HasDefaultValue(false)
                    .IsRequired();
                b.Property(t => t.NombreHu).HasMaxLength(100);
                b.Property(t => t.UrlHu).HasMaxLength(2048);

                b.HasMany(t => t.HistoricoEstados)
                    .WithOne()
                    .HasForeignKey(h => h.IdTicket)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(t => t.IdUsuarioAsignado)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HistoricoEstadosTicket>(b =>
            {
                b.ToTable("historicoestadosticket");
                b.HasKey(e => e.IdHistorico);
                b.Property(e => e.IdHistorico).ValueGeneratedNever();
                b.Property(h => h.IdEstadoOrigen).HasConversion<int?>();
                b.Property(h => h.IdEstadoDestino).HasConversion<int>();
                b.Property(h => h.Comentario).HasMaxLength(1000);

                b.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(h => h.IdUsuarioAccion)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Usuario>(b =>
            {
                b.ToTable("usuarios");
                b.HasKey(u => u.IdUsuario);
                b.Property(u => u.NombreUsuario).HasMaxLength(50).IsRequired();
                b.Property(u => u.Nombres).HasMaxLength(100).IsRequired();
                b.Property(u => u.Apellidos).HasMaxLength(100);
                b.Property(u => u.IdRol).HasConversion<int>();
                b.Property(u => u.IdArea).HasConversion<int?>();
                b.Property(u => u.ContrasenaHash).HasMaxLength(500);
                b.HasIndex(u => u.NombreUsuario).IsUnique();
            });

            modelBuilder.Entity<SesionUsuario>(b =>
            {
                b.ToTable("sesionesusuario");
                b.HasKey(s => s.IdSesion);
                b.Property(s => s.Jti).HasMaxLength(64).IsRequired();
                b.HasIndex(s => s.Jti).IsUnique();
                b.HasIndex(s => s.IdUsuario)
                    .IsUnique()
                    .HasFilter("\"fecharevocacion\" IS NULL");
                b.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(s => s.IdUsuario)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RolParametro>(b =>
            {
                b.ToTable("roles");
                b.HasKey(item => item.IdRol);
                b.Property(item => item.NombreRol).HasMaxLength(50).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(200);
            });

            modelBuilder.Entity<EstadoTicketParametro>(b =>
            {
                b.ToTable("estadosticket");
                b.HasKey(item => item.IdEstado);
                b.Property(item => item.Estado).HasMaxLength(50).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(500);
            });

            modelBuilder.Entity<OrigenTicketParametro>(b =>
            {
                b.ToTable("origenesticket");
                b.HasKey(item => item.IdOrigen);
                b.Property(item => item.Origen).IsRequired();
            });

            modelBuilder.Entity<AreaTicketParametro>(b =>
            {
                b.ToTable("areasticket");
                b.HasKey(item => item.IdArea);
                b.Property(item => item.Area).IsRequired();
                b.Property(item => item.Descripcion).HasMaxLength(200);
            });

            modelBuilder.Entity<Repositorio>(b =>
            {
                b.ToTable("repositorios");
                b.HasKey(item => item.IdRepositorio);
                b.Property(item => item.Nombre)
                    .HasColumnName("repositorio")
                    .HasMaxLength(100)
                    .IsRequired();
                b.Property(item => item.Link).HasMaxLength(255);
                b.Property(item => item.Descripcion).HasMaxLength(500);
                b.HasMany(item => item.Ramas)
                    .WithOne()
                    .HasForeignKey(item => item.IdRepositorio)
                    .OnDelete(DeleteBehavior.Cascade);
                b.Navigation(item => item.Ramas)
                    .UsePropertyAccessMode(PropertyAccessMode.Field);
            });

            modelBuilder.Entity<Rama>(b =>
            {
                b.ToTable("ramas");
                b.HasKey(item => item.IdRama);
                b.Property(item => item.NombreRama).HasMaxLength(150).IsRequired();
                b.HasIndex(item => new { item.IdRepositorio, item.NombreRama }).IsUnique();
            });

            modelBuilder.Entity<RamaTicket>(b =>
            {
                b.ToTable("ramasticket");
                b.HasKey(item => item.IdRamaTicket);
                b.HasIndex(item => new { item.IdTicket, item.IdRama }).IsUnique();
                b.HasOne<Ticket>()
                    .WithMany()
                    .HasForeignKey(item => item.IdTicket)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne<Rama>()
                    .WithMany()
                    .HasForeignKey(item => item.IdRama)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                    property.SetColumnName(property.Name.ToLowerInvariant());
            }

            modelBuilder.Entity<Repositorio>()
                .Property(item => item.Nombre)
                .HasColumnName("repositorio");
        }
    }
}
