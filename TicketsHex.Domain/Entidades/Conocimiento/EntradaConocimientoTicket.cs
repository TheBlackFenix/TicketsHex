using System.Text.Json;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.Entidades.Conocimiento
{
    public sealed class EntradaConocimientoTicket
    {
        public Guid IdEntrada { get; private set; }
        public Guid IdTicket { get; private set; }
        public TipoEntradaConocimiento IdTipoEntrada { get; private set; }
        public int IdResultado { get; private set; }
        public string Resumen { get; private set; } = string.Empty;
        public string? Sintomas { get; private set; }
        public string? Comprobaciones { get; private set; }
        public string? PasosReproduccion { get; private set; }
        public int? IdAmbiente { get; private set; }
        public bool? RequiereDespliegue { get; private set; }
        public string? Observaciones { get; private set; }
        public long IdUsuarioAutor { get; private set; }
        public Rol IdRolAutor { get; private set; }
        public DateTimeOffset FechaCreacion { get; private set; }
        public DateTimeOffset? FechaUltimaActualizacion { get; private set; }
        public bool Activo { get; private set; }
        public ICollection<ReferenciaEntradaConocimiento> Referencias { get; private set; } = new List<ReferenciaEntradaConocimiento>();
        public ICollection<RevisionEntradaConocimiento> Revisiones { get; private set; } = new List<RevisionEntradaConocimiento>();

        private EntradaConocimientoTicket() { }

        public EntradaConocimientoTicket(
            Guid idTicket,
            TipoEntradaConocimiento tipo,
            int idResultado,
            string resumen,
            string? sintomas,
            string? comprobaciones,
            string? pasosReproduccion,
            int? idAmbiente,
            bool? requiereDespliegue,
            string? observaciones,
            long idUsuarioAutor,
            Rol rolAutor,
            IEnumerable<(TipoReferenciaConocimiento Tipo, string Url, string? Descripcion)>? referencias = null)
        {
            if (idTicket == Guid.Empty)
                throw new ArgumentException("El ticket es obligatorio.", nameof(idTicket));
            if (idUsuarioAutor <= 0)
                throw new ArgumentException("El autor debe ser válido.", nameof(idUsuarioAutor));

            ValidarContenido(tipo, idResultado, resumen, sintomas, comprobaciones,
                pasosReproduccion, idAmbiente, requiereDespliegue, observaciones);

            IdEntrada = Guid.NewGuid();
            IdTicket = idTicket;
            IdTipoEntrada = tipo;
            IdUsuarioAutor = idUsuarioAutor;
            IdRolAutor = rolAutor;
            FechaCreacion = DateTimeOffset.UtcNow;
            Activo = true;
            AplicarContenido(idResultado, resumen, sintomas, comprobaciones,
                pasosReproduccion, idAmbiente, requiereDespliegue, observaciones);
            ReemplazarReferencias(referencias);
        }

        public void Actualizar(
            int idResultado,
            string resumen,
            string? sintomas,
            string? comprobaciones,
            string? pasosReproduccion,
            int? idAmbiente,
            bool? requiereDespliegue,
            string? observaciones,
            IEnumerable<(TipoReferenciaConocimiento Tipo, string Url, string? Descripcion)>? referencias,
            long idUsuarioAccion,
            Rol rolUsuarioAccion,
            TicketEstado estadoTicket)
        {
            if (!Activo)
                throw new InvalidOperationException("No se puede editar una entrada inactiva.");

            ValidarContenido(IdTipoEntrada, idResultado, resumen, sintomas, comprobaciones,
                pasosReproduccion, idAmbiente, requiereDespliegue, observaciones);

            Revisiones.Add(new RevisionEntradaConocimiento(
                IdEntrada,
                JsonSerializer.Serialize(CapturarSnapshot()),
                idUsuarioAccion,
                rolUsuarioAccion,
                estadoTicket));

            AplicarContenido(idResultado, resumen, sintomas, comprobaciones,
                pasosReproduccion, idAmbiente, requiereDespliegue, observaciones);
            ReemplazarReferencias(referencias);
            FechaUltimaActualizacion = DateTimeOffset.UtcNow;
        }

        private object CapturarSnapshot() => new
        {
            IdTipoEntrada,
            IdResultado,
            Resumen,
            Sintomas,
            Comprobaciones,
            PasosReproduccion,
            IdAmbiente,
            RequiereDespliegue,
            Observaciones,
            Referencias = Referencias.Select(item => new
            {
                item.TipoReferencia,
                item.Url,
                item.Descripcion
            })
        };

        private void AplicarContenido(
            int idResultado,
            string resumen,
            string? sintomas,
            string? comprobaciones,
            string? pasosReproduccion,
            int? idAmbiente,
            bool? requiereDespliegue,
            string? observaciones)
        {
            IdResultado = idResultado;
            Resumen = resumen.Trim();
            Sintomas = Normalizar(sintomas);
            Comprobaciones = Normalizar(comprobaciones);
            PasosReproduccion = Normalizar(pasosReproduccion);
            IdAmbiente = idAmbiente;
            RequiereDespliegue = requiereDespliegue;
            Observaciones = Normalizar(observaciones);
        }

        private void ReemplazarReferencias(
            IEnumerable<(TipoReferenciaConocimiento Tipo, string Url, string? Descripcion)>? referencias)
        {
            Referencias.Clear();
            if (referencias is null)
                return;

            foreach (var referencia in referencias)
            {
                Referencias.Add(new ReferenciaEntradaConocimiento(
                    IdEntrada,
                    referencia.Tipo,
                    referencia.Url,
                    referencia.Descripcion));
            }
        }

        private static void ValidarContenido(
            TipoEntradaConocimiento tipo,
            int idResultado,
            string resumen,
            string? sintomas,
            string? comprobaciones,
            string? pasosReproduccion,
            int? idAmbiente,
            bool? requiereDespliegue,
            string? observaciones)
        {
            if (!Enum.IsDefined(tipo))
                throw new ArgumentOutOfRangeException(nameof(tipo));
            if (!ResultadoEntradaConocimiento.PerteneceA(tipo, idResultado))
                throw new ArgumentException("El resultado no corresponde al tipo de entrada.", nameof(idResultado));
            if (string.IsNullOrWhiteSpace(resumen))
                throw new ArgumentException("El resumen es obligatorio.", nameof(resumen));
            ValidarLongitud(resumen, 2000, nameof(resumen));
            ValidarLongitud(sintomas, 2000, nameof(sintomas));
            ValidarLongitud(comprobaciones, 4000, nameof(comprobaciones));
            ValidarLongitud(pasosReproduccion, 4000, nameof(pasosReproduccion));
            ValidarLongitud(observaciones, 2000, nameof(observaciones));
            if (idAmbiente <= 0)
                throw new ArgumentException("El ambiente debe ser válido.", nameof(idAmbiente));
            if (tipo != TipoEntradaConocimiento.Solucion && requiereDespliegue.HasValue)
                throw new ArgumentException("Solo una solución puede indicar si requiere despliegue.", nameof(requiereDespliegue));
        }

        private static void ValidarLongitud(string? valor, int maximo, string parametro)
        {
            if (valor?.Trim().Length > maximo)
                throw new ArgumentException($"El campo no puede superar {maximo} caracteres.", parametro);
        }

        private static string? Normalizar(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }
}
