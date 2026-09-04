using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TicketsHex.Domain.Entidades.Conocimiento;
using TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context;
using Xunit;
using PostgreSqlContext = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context.MantenimientoContext;

namespace TicketsHex.Domain.Tests;

public class PersistenciaAutenticacionTests
{
    [Fact]
    public void Modelo_ef_incluye_campos_de_seguridad_y_sesiones()
    {
        var options = new DbContextOptionsBuilder<MantenimientoContext>()
            .UseSqlServer("Server=localhost,1433;Database=tickets;User Id=test;Password=test;TrustServerCertificate=True")
            .Options;
        using var context = new MantenimientoContext(options);

        var usuario = context.Model.FindEntityType("TicketsHex.Domain.Entidades.Usuario.Usuario");
        var sesion = context.Model.FindEntityType("TicketsHex.Domain.Entidades.Usuario.SesionUsuario");

        Assert.NotNull(usuario?.FindProperty("ContrasenaHash"));
        Assert.NotNull(usuario?.FindProperty("IntentosFallidos"));
        Assert.NotNull(usuario?.FindProperty("Bloqueado"));
        Assert.NotNull(sesion?.FindProperty("Jti"));
    }

    [Fact]
    public void Modelo_ef_incluye_historico_de_asignaciones_de_tickets()
    {
        var options = new DbContextOptionsBuilder<MantenimientoContext>()
            .UseSqlServer("Server=localhost,1433;Database=tickets;User Id=test;Password=test;TrustServerCertificate=True")
            .Options;
        using var context = new MantenimientoContext(options);

        var historico = context.Model.FindEntityType(
            "TicketsHex.Domain.Entidades.Ticket.HistoricoAsignacionTicket");
        var responsable = context.Model.FindEntityType(
            "TicketsHex.Domain.Entidades.Ticket.ResponsableTicket");

        Assert.NotNull(historico?.FindProperty("IdUsuarioAnterior"));
        Assert.NotNull(historico?.FindProperty("IdUsuarioAsignado"));
        Assert.NotNull(historico?.FindProperty("IdUsuarioAccion"));
        Assert.NotNull(historico?.FindProperty("IdTipoMovimiento"));
        Assert.NotNull(historico?.FindProperty("FechaAsignacion"));
        Assert.NotNull(responsable?.FindProperty("IdTipoResponsabilidad"));
        Assert.Contains(
            responsable!.GetIndexes(),
            indice => indice.IsUnique &&
                indice.Properties.Select(propiedad => propiedad.Name)
                    .SequenceEqual(["IdTicket", "IdTipoResponsabilidad"]));
    }

    [Fact]
    public void Modelo_ef_incluye_base_de_conocimiento_y_revisiones()
    {
        var options = new DbContextOptionsBuilder<MantenimientoContext>()
            .UseSqlServer("Server=localhost,1433;Database=tickets;User Id=test;Password=test;TrustServerCertificate=True")
            .Options;
        using var context = new MantenimientoContext(options);

        var entrada = context.Model.FindEntityType(
            "TicketsHex.Domain.Entidades.Conocimiento.EntradaConocimientoTicket");
        var revision = context.Model.FindEntityType(
            "TicketsHex.Domain.Entidades.Conocimiento.RevisionEntradaConocimiento");
        var tag = context.Model.FindEntityType(
            "TicketsHex.Domain.Entidades.Conocimiento.TagConocimiento");

        Assert.NotNull(entrada?.FindProperty("IdResultado"));
        Assert.NotNull(entrada?.FindProperty("Resumen"));
        Assert.NotNull(revision?.FindProperty("ContenidoAnterior"));
        Assert.True(tag?.FindIndex(tag.FindProperty("NombreNormalizado")!)?.IsUnique);
    }

    [Fact]
    public void Modelos_ef_respetan_nombres_fisicos_de_columnas_de_conocimiento()
    {
        var sqlServerOptions = new DbContextOptionsBuilder<MantenimientoContext>()
            .UseSqlServer("Server=localhost,1433;Database=tickets;User Id=test;Password=test;TrustServerCertificate=True")
            .Options;
        using var sqlServerContext = new MantenimientoContext(sqlServerOptions);

        var postgreSqlOptions = new DbContextOptionsBuilder<PostgreSqlContext>()
            .UseNpgsql("Host=localhost;Database=tickets;Username=test;Password=test")
            .Options;
        using var postgreSqlContext = new PostgreSqlContext(postgreSqlOptions);

        ValidarNombresColumnasConocimiento(sqlServerContext.Model, "dbo");
        ValidarNombresColumnasConocimiento(postgreSqlContext.Model, "public");
    }

    private static void ValidarNombresColumnasConocimiento(IModel model, string esquema)
    {
        var entrada = model.FindEntityType(typeof(EntradaConocimientoTicket))!;
        var tablaEntrada = StoreObjectIdentifier.Table("entradasconocimientoticket", esquema);
        Assert.Equal(
            "idtipoentrada",
            entrada.FindProperty(nameof(EntradaConocimientoTicket.IdTipoEntrada))!
                .GetColumnName(tablaEntrada));
        Assert.Equal(
            "idrolautor",
            entrada.FindProperty(nameof(EntradaConocimientoTicket.IdRolAutor))!
                .GetColumnName(tablaEntrada));

        var referencia = model.FindEntityType(typeof(ReferenciaEntradaConocimiento))!;
        var tablaReferencia = StoreObjectIdentifier.Table("referenciasentradaconocimiento", esquema);
        Assert.Equal(
            "tiporeferencia",
            referencia.FindProperty(nameof(ReferenciaEntradaConocimiento.TipoReferencia))!
                .GetColumnName(tablaReferencia));

        var revision = model.FindEntityType(typeof(RevisionEntradaConocimiento))!;
        var tablaRevision = StoreObjectIdentifier.Table("revisionesentradaconocimiento", esquema);
        Assert.Equal(
            "idrolusuarioaccion",
            revision.FindProperty(nameof(RevisionEntradaConocimiento.IdRolUsuarioAccion))!
                .GetColumnName(tablaRevision));
        Assert.Equal(
            "idestadoticket",
            revision.FindProperty(nameof(RevisionEntradaConocimiento.IdEstadoTicket))!
                .GetColumnName(tablaRevision));
    }
}
