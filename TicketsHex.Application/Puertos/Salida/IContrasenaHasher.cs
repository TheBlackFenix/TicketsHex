namespace TicketsHex.Application.Puertos.Salida
{
    public interface IContrasenaHasher
    {
        string CrearHash(string contrasena);
        ResultadoVerificacionContrasena Verificar(string hash, string contrasena);
    }

    public enum ResultadoVerificacionContrasena
    {
        Fallida,
        Exitosa,
        ExitosaRequiereRehash
    }
}
