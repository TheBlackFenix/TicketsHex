using Microsoft.EntityFrameworkCore;
using TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository.Context;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class PersistenciaAutenticacionTests
{
    [Fact]
    public void Modelo_ef_incluye_campos_de_seguridad_y_sesiones()
    {
        var options = new DbContextOptionsBuilder<MantenimientoContext>()
            .UseNpgsql("Host=localhost;Database=tickets;Username=test;Password=test")
            .Options;
        using var context = new MantenimientoContext(options);

        var usuario = context.Model.FindEntityType("TicketsHex.Domain.Entidades.Usuario.Usuario");
        var sesion = context.Model.FindEntityType("TicketsHex.Domain.Entidades.Usuario.SesionUsuario");

        Assert.NotNull(usuario?.FindProperty("ContrasenaHash"));
        Assert.NotNull(usuario?.FindProperty("IntentosFallidos"));
        Assert.NotNull(usuario?.FindProperty("Bloqueado"));
        Assert.NotNull(sesion?.FindProperty("TokenHash"));
    }
}
