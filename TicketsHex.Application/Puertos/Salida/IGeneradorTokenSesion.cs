namespace TicketsHex.Application.Puertos.Salida
{
    public interface IGeneradorTokenSesion
    {
        string Generar();
        string CrearHash(string token);
    }
}
