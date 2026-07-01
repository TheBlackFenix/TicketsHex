using TicketsHex.Domain.Entidades.ConfiguracionGit;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class RepositorioTests
{
    [Fact]
    public void Crea_repositorio_y_rama_con_datos_normalizados()
    {
        var repositorio = new Repositorio(
            "  tickets-api  ",
            "https://git.example.com/tickets-api",
            " API de tickets ");

        var rama = repositorio.CrearRama(" feature/asignacion-rama ");

        Assert.Equal("tickets-api", repositorio.Nombre);
        Assert.Equal("API de tickets", repositorio.Descripcion);
        Assert.Equal("feature/asignacion-rama", rama.NombreRama);
        Assert.Equal(repositorio.IdRepositorio, rama.IdRepositorio);
        Assert.Contains(rama, repositorio.Ramas);
    }

    [Fact]
    public void Rechaza_link_que_no_sea_http_o_https()
    {
        Assert.Throws<ArgumentException>(() =>
            new Repositorio("tickets-api", "ftp://example.com/repo", null));
    }

    [Fact]
    public void Rama_ticket_requiere_identificadores_validos()
    {
        Assert.Throws<ArgumentException>(() =>
            new RamaTicket(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() =>
            new RamaTicket(Guid.NewGuid(), Guid.Empty));
    }
}
