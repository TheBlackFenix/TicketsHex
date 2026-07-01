using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketsHex.Application.Comun.Seguridad;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Enums;

namespace TicketsHex.API.Servicios
{
    public sealed class GeneradorJwtSesion : IGeneradorJwtSesion, IDisposable
    {
        private readonly JwtOptions _options;
        private readonly RSA _privateKey;
        private readonly SigningCredentials _credentials;

        public GeneradorJwtSesion(
            IOptions<JwtOptions> options,
            IHostEnvironment environment)
        {
            _options = options.Value;
            JwtKeyLoader.ValidarOpciones(_options);
            _privateKey = JwtKeyLoader.CargarClavePrivada(_options, environment);
            var securityKey = new RsaSecurityKey(_privateKey)
            {
                KeyId = JwtKeyLoader.CrearKeyId(_privateKey)
            };
            _credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.RsaSha256);
        }

        public TokenJwtGenerado Generar(
            long idUsuario,
            string nombreUsuario,
            Rol rol,
            string jti,
            DateTimeOffset fechaCreacion)
        {
            var expiracion = fechaCreacion.AddMinutes(_options.AccessTokenMinutes);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, nombreUsuario),
                new Claim(JwtRegisteredClaimNames.Jti, jti),
                new Claim(
                    JwtRegisteredClaimNames.Iat,
                    fechaCreacion.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64),
                new Claim("client_id", _options.ClientId),
                new Claim(ClaimTypes.Role, rol.ToString())
            };

            var jwt = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: fechaCreacion.UtcDateTime,
                expires: expiracion.UtcDateTime,
                signingCredentials: _credentials);
            jwt.Header[JwtHeaderParameterNames.Typ] = "at+jwt";

            return new TokenJwtGenerado(
                new JwtSecurityTokenHandler().WriteToken(jwt),
                expiracion);
        }

        public void Dispose() => _privateKey.Dispose();

    }
}
