namespace TicketsHex.Application.Comun.Seguridad
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string PrivateKeyPath { get; set; } = string.Empty;
        public string PublicKeyPath { get; set; } = string.Empty;
        public string PrivateKeyBase64 { get; set; } = string.Empty;
        public string PublicKeyBase64 { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 15;
        public int ClockSkewSeconds { get; set; } = 30;
    }
}
