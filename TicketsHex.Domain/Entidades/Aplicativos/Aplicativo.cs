namespace TicketsHex.Domain.Entidades.Aplicativos
{
    public sealed class Aplicativo
    {
        public Guid IdAplicativo { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string? Descripcion { get; private set; }
        public bool Activo { get; private set; } = true;

        private Aplicativo() { }

        public Aplicativo(string nombre, string? descripcion)
        {
            IdAplicativo = Guid.NewGuid();
            Actualizar(nombre, descripcion);
        }

        public void Actualizar(string nombre, string? descripcion)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre del aplicativo es obligatorio.", nameof(nombre));
            if (nombre.Length > 100)
                throw new ArgumentException("El nombre del aplicativo no puede superar 100 caracteres.", nameof(nombre));
            if (descripcion?.Length > 200)
                throw new ArgumentException("La descripción del aplicativo no puede superar 200 caracteres.", nameof(descripcion));

            Nombre = nombre.Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();
        }

        public void Activar() => Activo = true;
        public void Desactivar() => Activo = false;
    }
}
