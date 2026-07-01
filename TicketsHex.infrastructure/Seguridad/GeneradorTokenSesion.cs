using System.Security.Cryptography;
using System.Text;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.infrastructure.Seguridad
{
    public sealed class GeneradorTokenSesion : IGeneradorTokenSesion
    {
        public string Generar()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public string CrearHash(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash);
        }
    }
}
