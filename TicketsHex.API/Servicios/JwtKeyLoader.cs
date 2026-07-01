using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TicketsHex.Application.Comun.Seguridad;

namespace TicketsHex.API.Servicios
{
    public static class JwtKeyLoader
    {
        public static RSA CargarClavePrivada(
            JwtOptions options,
            IHostEnvironment environment)
        {
            var path = ResolverPath(options.PrivateKeyPath, environment);
            if (!File.Exists(path))
                throw new InvalidOperationException(
                    $"No se encontró la clave privada JWT en '{path}'.");

            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(path));
            ValidarTamano(rsa, "privada");
            if (rsa.ExportParameters(includePrivateParameters: true).D is null)
                throw new InvalidOperationException(
                    "El archivo configurado no contiene una clave privada RSA.");
            return rsa;
        }

        public static RSA CargarClavePublica(
            JwtOptions options,
            IHostEnvironment environment)
        {
            var path = ResolverPath(options.PublicKeyPath, environment);
            if (!File.Exists(path))
                throw new InvalidOperationException(
                    $"No se encontró la clave pública JWT en '{path}'.");

            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(path));
            ValidarTamano(rsa, "pública");
            return rsa;
        }

        public static void ValidarPar(RSA privateKey, RSA publicKey)
        {
            var privada = privateKey.ExportParameters(includePrivateParameters: false);
            var publica = publicKey.ExportParameters(includePrivateParameters: false);

            if (privada.Modulus is null ||
                publica.Modulus is null ||
                !CryptographicOperations.FixedTimeEquals(privada.Modulus, publica.Modulus) ||
                privada.Exponent is null ||
                publica.Exponent is null ||
                !privada.Exponent.SequenceEqual(publica.Exponent))
                throw new InvalidOperationException(
                    "Las claves JWT privada y pública no pertenecen al mismo par.");
        }

        public static string CrearKeyId(RSA rsa)
        {
            var hash = SHA256.HashData(rsa.ExportSubjectPublicKeyInfo());
            return Base64UrlEncoder.Encode(hash);
        }

        public static void ValidarOpciones(JwtOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.Issuer))
                throw new InvalidOperationException("Jwt:Issuer es obligatorio.");
            if (string.IsNullOrWhiteSpace(options.Audience))
                throw new InvalidOperationException("Jwt:Audience es obligatorio.");
            if (string.IsNullOrWhiteSpace(options.ClientId))
                throw new InvalidOperationException("Jwt:ClientId es obligatorio.");
            if (string.IsNullOrWhiteSpace(options.PrivateKeyPath))
                throw new InvalidOperationException("Jwt:PrivateKeyPath es obligatorio.");
            if (string.IsNullOrWhiteSpace(options.PublicKeyPath))
                throw new InvalidOperationException("Jwt:PublicKeyPath es obligatorio.");
            if (options.AccessTokenMinutes is < 1 or > 60)
                throw new InvalidOperationException(
                    "Jwt:AccessTokenMinutes debe estar entre 1 y 60.");
            if (options.ClockSkewSeconds is < 0 or > 60)
                throw new InvalidOperationException(
                    "Jwt:ClockSkewSeconds debe estar entre 0 y 60.");
        }

        private static string ResolverPath(string path, IHostEnvironment environment) =>
            Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, path));

        private static void ValidarTamano(RSA rsa, string tipo)
        {
            if (rsa.KeySize < 2048)
                throw new InvalidOperationException(
                    $"La clave RSA {tipo} debe tener al menos 2048 bits.");
        }
    }
}
