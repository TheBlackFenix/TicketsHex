namespace TicketsHex.API.Middelwares.ExceptionHandling
{
    public class ExceptionMessageOptions
    {
        public int StatusCode { get; set; } = StatusCodes.Status500InternalServerError;
        public string Title { get; set; } = "Error interno del servidor";
        public string Detail { get; set; } = "Ocurrió un error inesperado en el sistema.";
        public bool UseExceptionMessage { get; set; } = false;
    }
}
