using TicketsHex.API.Servicios;
using TicketsHex.Application.Comun.Configuracion;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class ConfiguracionTests
{
    [Fact]
    public void Usuario_respeta_limite_de_intentos_y_vigencia_configurados()
    {
        var fechaCambio = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var usuario = new Usuario(
            1,
            "developer",
            "Developer",
            null,
            Rol.Desarrollador,
            Area.Mantenimiento,
            "hash");
        usuario.CambiarContrasena("hash", fechaCambio);

        usuario.RegistrarIntentoFallido(fechaCambio.AddDays(1), maximoIntentosFallidos: 2);
        Assert.False(usuario.Bloqueado);
        usuario.RegistrarIntentoFallido(fechaCambio.AddDays(1), maximoIntentosFallidos: 2);

        Assert.True(usuario.Bloqueado);
        Assert.False(usuario.ContrasenaEstaExpirada(fechaCambio.AddDays(6), diasVigencia: 7));
        Assert.True(usuario.ContrasenaEstaExpirada(fechaCambio.AddDays(7), diasVigencia: 7));
    }

    [Fact]
    public void Configuracion_operativa_invalida_es_rechazada_al_iniciar()
    {
        var usuarios = new UsuariosOptions();
        var notificaciones = new NotificacionesOptions
        {
            DiasRetencion = 0
        };

        Assert.Throws<InvalidOperationException>(() =>
            ConfiguracionAplicacion.Validar(
                usuarios,
                notificaciones,
                new ParametricosOptions()));
    }
}
