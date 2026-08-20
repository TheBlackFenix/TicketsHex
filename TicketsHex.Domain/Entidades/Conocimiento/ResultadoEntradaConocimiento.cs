using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Conocimiento
{
    public static class ResultadoEntradaConocimiento
    {
        public const int DiagnosticoConfirmado = 1;
        public const int DiagnosticoDescartado = 2;
        public const int DiagnosticoInconcluso = 3;
        public const int SolucionExitosa = 4;
        public const int SolucionFallida = 5;
        public const int SolucionParcial = 6;
        public const int SolucionNoImplementada = 7;
        public const int ValidacionAprobada = 8;
        public const int ValidacionRechazada = 9;
        public const int ValidacionConObservaciones = 10;

        public static bool PerteneceA(TipoEntradaConocimiento tipo, int idResultado) => tipo switch
        {
            TipoEntradaConocimiento.Diagnostico => idResultado is >= 1 and <= 3,
            TipoEntradaConocimiento.Solucion => idResultado is >= 4 and <= 7,
            TipoEntradaConocimiento.ValidacionQa => idResultado is >= 8 and <= 10,
            _ => false
        };
    }
}
