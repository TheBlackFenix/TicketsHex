using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using TicketsHex.Domain.Servicios;
using TicketsHex.infrastructure.Seguridad;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class AutenticacionTests
{
    [Fact]
    public void Contrasena_debe_cumplir_todas_las_reglas()
    {
        Assert.Throws<ArgumentException>(() => ValidadorContrasena.Validar("Corta#1"));
        Assert.Throws<ArgumentException>(() => ValidadorContrasena.Validar("minuscula#1"));
        Assert.Throws<ArgumentException>(() => ValidadorContrasena.Validar("SinNumero#"));
        Assert.Throws<ArgumentException>(() => ValidadorContrasena.Validar("SinEspecial1"));

        ValidadorContrasena.Validar("Valida#2026");
    }

    [Fact]
    public void Usuario_se_bloquea_al_quinto_intento_fallido()
    {
        var usuario = CrearUsuario();
        var ahora = DateTimeOffset.UtcNow;

        for (var intento = 0; intento < 5; intento++)
            usuario.RegistrarIntentoFallido(ahora);

        Assert.True(usuario.Bloqueado);
        Assert.Equal(5, usuario.IntentosFallidos);
        Assert.Equal(ahora, usuario.FechaBloqueo);
    }

    [Fact]
    public void Contrasena_expira_a_los_treinta_dias()
    {
        var usuario = CrearUsuario();
        var fechaCambio = DateTimeOffset.UtcNow;
        usuario.CambiarContrasena("hash", fechaCambio);

        Assert.False(usuario.ContrasenaEstaExpirada(fechaCambio.AddDays(29)));
        Assert.True(usuario.ContrasenaEstaExpirada(fechaCambio.AddDays(30)));
    }

    [Fact]
    public void Password_hasher_usa_sal_y_verifica_sin_guardar_texto_plano()
    {
        var hasher = new ContrasenaHasher();
        const string contrasena = "Valida#2026";

        var primerHash = hasher.CrearHash(contrasena);
        var segundoHash = hasher.CrearHash(contrasena);

        Assert.NotEqual(primerHash, segundoHash);
        Assert.DoesNotContain(contrasena, primerHash);
        Assert.NotEqual(
            ResultadoVerificacionContrasena.Fallida,
            hasher.Verificar(primerHash, contrasena));
        Assert.Equal(
            ResultadoVerificacionContrasena.Fallida,
            hasher.Verificar(primerHash, "Incorrecta#1"));
    }

    [Fact]
    public void Sesion_puede_revocarse_y_deja_de_ser_vigente()
    {
        var ahora = DateTimeOffset.UtcNow;
        var sesion = new SesionUsuario(1, "hash-token", ahora, ahora.AddHours(8));

        Assert.True(sesion.EstaVigente(ahora.AddMinutes(1)));

        sesion.Revocar(ahora.AddMinutes(2));

        Assert.False(sesion.EstaVigente(ahora.AddMinutes(3)));
    }

    private static Usuario CrearUsuario() => new(
        1,
        "planner",
        "Usuario",
        "Planner",
        Rol.Planner,
        Area.Mantenimiento,
        "hash");
}
