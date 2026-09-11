using Microsoft.Extensions.Options;
using TicketsHex.Domain.Comun.Errores;

namespace TicketsHex.API.Middelwares.ExceptionHandling
{
    public sealed class ExceptionMessageResolver
    {
        private readonly IOptionsMonitor<ExceptionHandlingOptions> _optionsMonitor;

        public ExceptionMessageResolver(IOptionsMonitor<ExceptionHandlingOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor;
        }

        public ExceptionMessageOptions Resolve(Exception exception)
        {
            var options = _optionsMonitor.CurrentValue;
            var codigo = exception.ObtenerCodigo();

            if (!string.IsNullOrWhiteSpace(codigo) &&
                options.Codes.TryGetValue(codigo, out var codeOptions))
            {
                return Copiar(codeOptions, codigo);
            }
            if (!string.IsNullOrWhiteSpace(codigo))
                return CrearFallback(codigo);

            var exceptionKey = GetExceptionKey(exception);
            if (options.Mappings.TryGetValue(exceptionKey, out var mappedOptions))
                return Copiar(mappedOptions, mappedOptions.Code);

            var exceptionTypeName = exception.GetType().Name;
            if (options.Mappings.TryGetValue(exceptionTypeName, out var directOptions))
                return Copiar(directOptions, directOptions.Code);

            return Copiar(options.Default, options.Default.Code);
        }

        public ExceptionMessageOptions Resolve(string codigo)
        {
            var options = _optionsMonitor.CurrentValue;
            return options.Codes.TryGetValue(codigo, out var codeOptions)
                ? Copiar(codeOptions, codigo)
                : CrearFallback(codigo);
        }

        private static string GetExceptionKey(Exception exception) => exception switch
        {
            Application.Comun.Excepciones.RecursoNoEncontradoException => "RecursoNoEncontradoException",
            Application.Comun.Excepciones.ConflictoException => "ConflictoException",
            Application.Comun.Excepciones.UsuarioNoAutenticadoException => "UsuarioNoAutenticadoException",
            Application.Comun.Excepciones.CuentaBloqueadaException => "CuentaBloqueadaException",
            Application.Comun.Excepciones.ContrasenaExpiradaException => "ContrasenaExpiradaException",
            System.Data.Common.DbException => "DbException",
            System.Data.DataException => "DataException",
            ArgumentNullException => "ArgumentNullException",
            ArgumentException => "ArgumentException",
            FileNotFoundException => "FileNotFoundException",
            UnauthorizedAccessException => "UnauthorizedAccessException",
            BadHttpRequestException => "BadHttpRequestException",
            IOException => "IOException",
            InvalidOperationException => "InvalidOperationException",
            _ => exception.GetType().Name
        };

        private static ExceptionMessageOptions Copiar(
            ExceptionMessageOptions origen,
            string codigo) => new()
        {
            Code = codigo,
            StatusCode = origen.StatusCode,
            Title = origen.Title,
            Detail = origen.Detail,
            UseExceptionMessage = origen.UseExceptionMessage
        };

        private static ExceptionMessageOptions CrearFallback(string codigo) => codigo switch
        {
            CodigosError.ErrorValidacion => Crear(
                codigo, 400, "Error de validación", "La solicitud contiene datos inválidos.", true),
            CodigosError.SolicitudInvalida => Crear(
                codigo, 400, "Solicitud inválida", "No fue posible interpretar la solicitud."),
            CodigosError.CredencialesInvalidas => Crear(
                codigo, 401, "Credenciales inválidas", "Usuario o contraseña inválidos."),
            CodigosError.SesionInvalida => Crear(
                codigo, 401, "Sesión inválida", "La sesión no es válida o expiró."),
            CodigosError.CambioContrasenaRequerido => Crear(
                codigo, 403, "Cambio de contraseña requerido", "Debe cambiar la contraseña antes de utilizar el resto de la API."),
            CodigosError.ContrasenaActualInvalida => Crear(
                codigo, 400, "Contraseña actual inválida", "La contraseña actual no es correcta."),
            CodigosError.CuentaBloqueada => Crear(
                codigo, 403, "Cuenta bloqueada", "La cuenta está bloqueada por intentos fallidos.", true),
            CodigosError.AccionNoPermitida or CodigosError.TicketNoAsignado => Crear(
                codigo, 403, "Acción no permitida", "No tiene permiso para realizar esta acción.", true),
            CodigosError.RecursoNoEncontrado or
            CodigosError.TicketNoEncontrado or
            CodigosError.UsuarioNoEncontrado => Crear(
                codigo, 404, "Recurso no encontrado", "El recurso solicitado no existe.", true),
            CodigosError.TransicionInvalida or
            CodigosError.DesarrolladorNoAsignado or
            CodigosError.QaNoAsignado or
            CodigosError.TicketFinalizado or
            CodigosError.FuenteConocimientoRequerida or
            CodigosError.RecursoDuplicado or
            CodigosError.RepositorioNoAsociado or
            CodigosError.RelacionRepositorioEnUso or
            CodigosError.AplicativoConRamasAsociadas or
            CodigosError.Conflicto or
            CodigosError.OperacionInvalida => Crear(
                codigo, 409, "Conflicto", "No se pudo completar la operación solicitada.", true),
            _ => Crear(
                codigo, 500, "Error interno del servidor", "Ocurrió un error inesperado en el sistema.")
        };

        private static ExceptionMessageOptions Crear(
            string codigo,
            int statusCode,
            string title,
            string detail,
            bool useExceptionMessage = false) => new()
        {
            Code = codigo,
            StatusCode = statusCode,
            Title = title,
            Detail = detail,
            UseExceptionMessage = useExceptionMessage
        };
    }
}
