namespace TicketsHex.API.Middelwares.ExceptionHandling
{
    public class ExceptionHandlingOptions
    {
        public ExceptionMessageOptions Default { get; set; } = new();

        public Dictionary<string, ExceptionMessageOptions> Mappings { get; set; } = new();
    }
}
