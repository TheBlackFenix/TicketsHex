namespace TicketsHex.Domain.Comun.Errores
{
    public static class CodigosError
    {
        public const string ErrorInterno = "ERROR_INTERNO";
        public const string ErrorValidacion = "ERROR_VALIDACION";
        public const string CredencialesInvalidas = "CREDENCIALES_INVALIDAS";
        public const string SesionInvalida = "SESION_INVALIDA";
        public const string CambioContrasenaRequerido = "CAMBIO_CONTRASENA_REQUERIDO";
        public const string ContrasenaActualInvalida = "CONTRASENA_ACTUAL_INVALIDA";
        public const string CuentaBloqueada = "CUENTA_BLOQUEADA";
        public const string AccionNoPermitida = "ACCION_NO_PERMITIDA";
        public const string RecursoNoEncontrado = "RECURSO_NO_ENCONTRADO";
        public const string TicketNoEncontrado = "TICKET_NO_ENCONTRADO";
        public const string UsuarioNoEncontrado = "USUARIO_NO_ENCONTRADO";
        public const string TransicionInvalida = "TRANSICION_INVALIDA";
        public const string TicketNoAsignado = "TICKET_NO_ASIGNADO";
        public const string DesarrolladorNoAsignado = "DESARROLLADOR_NO_ASIGNADO";
        public const string QaNoAsignado = "QA_NO_ASIGNADO";
        public const string TicketFinalizado = "TICKET_FINALIZADO";
        public const string FuenteConocimientoRequerida = "FUENTE_CONOCIMIENTO_REQUERIDA";
        public const string RecursoDuplicado = "RECURSO_DUPLICADO";
        public const string Conflicto = "CONFLICTO";
        public const string OperacionInvalida = "OPERACION_INVALIDA";
        public const string SolicitudInvalida = "SOLICITUD_INVALIDA";

        internal const string DataKey = "TicketsHex.CodigoError";

        public static T ConCodigo<T>(this T exception, string codigo)
            where T : Exception
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(codigo);
            exception.Data[DataKey] = codigo;
            return exception;
        }

        public static string? ObtenerCodigo(this Exception exception) =>
            exception.Data[DataKey] as string;
    }
}
