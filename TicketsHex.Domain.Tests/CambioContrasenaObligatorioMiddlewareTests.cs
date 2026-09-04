using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TicketsHex.API.Middelwares;
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

        await middleware.InvokeAsync(contexto);

        Assert.False(siguienteFueInvocado);
        Assert.Equal(StatusCodes.Status403Forbidden, contexto.Response.StatusCode);
        contexto.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(contexto.Response.Body);
        Assert.Equal(
            "PASSWORD_CHANGE_REQUIRED",
            json.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("/api/auth/cambiar-contrasena")]
    [InlineData("/api/auth/logout")]
    public async Task Token_restringido_solo_permite_cambio_de_contrasena_y_logout(string ruta)
    {
        var siguienteFueInvocado = false;
        var middleware = new CambioContrasenaObligatorioMiddleware(_ =>
        {
            siguienteFueInvocado = true;
            return Task.CompletedTask;
        });
        var contexto = CrearContexto(ruta);

        await middleware.InvokeAsync(contexto);

        Assert.True(siguienteFueInvocado);
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
}
