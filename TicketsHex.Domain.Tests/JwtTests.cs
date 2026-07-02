using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TicketsHex.API.Servicios;
using TicketsHex.Application.Comun.Seguridad;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class JwtTests
{
    [Fact]
    public void JwtKeyLoader_carga_claves_desde_base64()
    {
        using var original = RSA.Create(2048);
        var options = new JwtOptions
        {
            Issuer = "TicketsHex.Tests",
            Audience = "TicketsHex.Tests.API",
            ClientId = "TicketsHex.Tests.Client",
            PrivateKeyBase64 = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(original.ExportPkcs8PrivateKeyPem())),
            PublicKeyBase64 = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(original.ExportSubjectPublicKeyInfoPem()))
        };
        var environment = new HostEnvironmentFake(Path.GetTempPath());

        using var privateKey = JwtKeyLoader.CargarClavePrivada(options, environment);
        using var publicKey = JwtKeyLoader.CargarClavePublica(options, environment);

        JwtKeyLoader.ValidarOpciones(options);
        JwtKeyLoader.ValidarPar(privateKey, publicKey);
    }

    [Fact]
    public void Jwt_usa_rs256_y_valida_firma_emisor_audiencia_y_claims()
    {
        var directory = Directory.CreateTempSubdirectory("ticketshex-jwt-");
        try
        {
            using var rsa = RSA.Create(2048);
            var privatePath = Path.Combine(directory.FullName, "private.pem");
            var publicPath = Path.Combine(directory.FullName, "public.pem");
            File.WriteAllText(privatePath, rsa.ExportPkcs8PrivateKeyPem());
            File.WriteAllText(publicPath, rsa.ExportSubjectPublicKeyInfoPem());

            var options = new JwtOptions
            {
                Issuer = "TicketsHex.Tests",
                Audience = "TicketsHex.Tests.API",
                ClientId = "TicketsHex.Tests.Client",
                PrivateKeyPath = privatePath,
                PublicKeyPath = publicPath,
                AccessTokenMinutes = 15,
                ClockSkewSeconds = 0
            };
            using var generator = new GeneradorJwtSesion(
                Options.Create(options),
                new HostEnvironmentFake(directory.FullName));
            var ahora = DateTimeOffset.UtcNow;
            const string jti = "0123456789abcdef0123456789abcdef";

            var resultado = generator.Generar(
                7,
                "developer",
                Rol.Desarrollador,
                jti,
                ahora);

            using var publicRsa = RSA.Create();
            publicRsa.ImportFromPem(File.ReadAllText(publicPath));
            var handler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            var principal = handler.ValidateToken(
                resultado.Token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(publicRsa),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ValidTypes = ["at+jwt"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out var validatedToken);

            var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);
            Assert.Equal(SecurityAlgorithms.RsaSha256, jwt.Header.Alg);
            Assert.Equal("at+jwt", jwt.Header.Typ);
            Assert.Equal(jti, principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value);
            Assert.Equal("7", principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
            Assert.Equal(
                options.ClientId,
                principal.FindFirst("client_id")?.Value);
            Assert.InRange(
                resultado.FechaExpiracion,
                ahora.AddMinutes(14),
                ahora.AddMinutes(16));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private sealed class HostEnvironmentFake : IHostEnvironment
    {
        public HostEnvironmentFake(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }

        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "TicketsHex.Tests";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
