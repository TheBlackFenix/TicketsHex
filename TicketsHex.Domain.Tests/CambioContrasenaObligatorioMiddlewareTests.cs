using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TicketsHex.API.Middelwares;
using TicketsHex.API.Middelwares.ExceptionHandling;
using TicketsHex.Domain.Comun.Errores;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class CambioContrasenaObligatorioMiddlewareTests
{
    [Fact]
    public async Task Token_restringido_bloquea_cualquier_endpoint_de_negocio()
    {
        var siguienteFueInvocado = false;
        var middleware = new CambioContrasenaObligatorioMiddleware(_ =>
        {
            siguienteFueInvocado = true;
            return Task.CompletedTask;
        });
        var contexto = CrearContexto("/api/tickets");

        await middleware.InvokeAsync(contexto, CrearWriter());

        Assert.False(siguienteFueInvocado);
        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.Equal(
            CodigosError.CambioContrasenaRequerido,
            json.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("/api/auth/cambiar-contrasena")]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/inicializar")]
    [InlineData("/health")]
    [InlineData("/.well-known/jwks.json")]
    [InlineData("/swagger/index.html")]
    public async Task Token_restringido_permite_solo_rutas_de_recuperacion_o_tecnicas(string ruta)
    {
        var siguienteFueInvocado = false;
        var middleware = new CambioContrasenaObligatorioMiddleware(_ =>
        {
            siguienteFueInvocado = true;
            return Task.CompletedTask;
        });
        var contexto = CrearContexto(ruta);

        await middleware.InvokeAsync(contexto, CrearWriter());

        Assert.True(siguienteFueInvocado);
    }

    [Fact]
    public async Task Token_restringido_no_permite_logout()
    {
        var siguienteFueInvocado = false;
        var middleware = new CambioContrasenaObligatorioMiddleware(_ =>
        {
            siguienteFueInvocado = true;
            return Task.CompletedTask;
        });
        var contexto = CrearContexto("/api/auth/logout");

        await middleware.InvokeAsync(contexto, CrearWriter());

        Assert.False(siguienteFueInvocado);
        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
    }

    private static DefaultHttpContext CrearContexto(string ruta)
    {
        var contexto = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() },
            Request = { Path = ruta },
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(CambioContrasenaObligatorioMiddleware.ClaimName, bool.TrueString)],
                "Bearer"))
        };
        return contexto;
    }

    private static ApiProblemDetailsWriter CrearWriter()
    {
        var options = new ExceptionHandlingOptions
        {
            Codes =
            {
                [CodigosError.CambioContrasenaRequerido] = new ExceptionMessageOptions
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Title = "Cambio de contraseña requerido",
                    Detail = "Debe cambiar la contraseña antes de utilizar el resto de la API."
                }
            }
        };
        return new ApiProblemDetailsWriter(
            new ExceptionMessageResolver(new OptionsMonitorFake<ExceptionHandlingOptions>(options)),
            new HostEnvironmentFake());
    }

    private sealed class OptionsMonitorFake<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class HostEnvironmentFake : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "TicketsHex.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
