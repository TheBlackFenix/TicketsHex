using Microsoft.AspNetCore.Identity;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.infrastructure.Seguridad
{
    public sealed class ContrasenaHasher : IContrasenaHasher
    {
        private readonly PasswordHasher<string> _hasher = new();

        public string CrearHash(string contrasena) =>
            _hasher.HashPassword(string.Empty, contrasena);

        public ResultadoVerificacionContrasena Verificar(string hash, string contrasena)
        {
            var resultado = _hasher.VerifyHashedPassword(string.Empty, hash, contrasena);
            return resultado switch
            {
                PasswordVerificationResult.Success =>
                    ResultadoVerificacionContrasena.Exitosa,
                PasswordVerificationResult.SuccessRehashNeeded =>
                    ResultadoVerificacionContrasena.ExitosaRequiereRehash,
                _ => ResultadoVerificacionContrasena.Fallida
            };
        }
    }
}
