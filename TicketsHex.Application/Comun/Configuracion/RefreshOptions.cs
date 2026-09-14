namespace TicketsHex.Application.Comun.Configuracion
{
    public sealed class RefreshOptions
    {
        public const string SectionName = "Refresh";
        public int DiasVigencia { get; set; } = 7;
        public string CookieSameSite { get; set; } = "Lax";
        public bool CookieSecure { get; set; } = true;
    }
}
