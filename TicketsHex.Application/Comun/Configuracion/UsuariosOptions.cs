namespace TicketsHex.Application.Comun.Configuracion
{
    public sealed class UsuariosOptions
    {
        public const string SectionName = "Usuarios";

        public string ContrasenaPorDefecto { get; set; } = string.Empty;
        public int MaximoIntentosFallidos { get; set; } = 5;
        public int DiasVigenciaContrasena { get; set; } = 30;
    }
}
