using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TicketsHex.API.Middelwares.ExceptionHandling;
using TicketsHex.Domain.Comun.Errores;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class ExceptionHandlingTests
{
    [Fact]
    public void Mensaje_funcional_puede_sobrescribirse_desde_configuracion()
    {
        var options = new ExceptionHandlingOptions
        {
            Codes =
            {
                [CodigosError.TransicionInvalida] = new ExceptionMessageOptions
                {
                    StatusCode = StatusCodes.Status409Conflict,
                    Title = "Flujo no permitido",
                    Detail = "Mensaje configurado para el ambiente."
                }
            }
        };
        var resolver = new ExceptionMessageResolver(
            new OptionsMonitorFake<ExceptionHandlingOptions>(options));

        var resultado = resolver.Resolve(CodigosError.TransicionInvalida);

        Assert.Equal(CodigosError.TransicionInvalida, resultado.Code);
        Assert.Equal(StatusCodes.Status409Conflict, resultado.StatusCode);
        Assert.Equal("Flujo no permitido", resultado.Title);
        Assert.Equal("Mensaje configurado para el ambiente.", resultado.Detail);
    }

    [Fact]
    public void Codigo_conocido_con_configuracion_ausente_usa_fallback_seguro()
    {
        var resolver = new ExceptionMessageResolver(
            new OptionsMonitorFake<ExceptionHandlingOptions>(new ExceptionHandlingOptions()));

        var resultado = resolver.Resolve(CodigosError.TicketFinalizado);

        Assert.Equal(CodigosError.TicketFinalizado, resultado.Code);
        Assert.Equal(StatusCodes.Status409Conflict, resultado.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Detail));
    }

    private sealed class OptionsMonitorFake<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
