namespace TicketsHex.Domain.Servicios
{
    public static class ValidadorContrasena
    {
        public static void Validar(string contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < 8)
                throw new ArgumentException("La contraseña debe tener mínimo 8 caracteres.");
            if (!contrasena.Any(char.IsUpper))
                throw new ArgumentException("La contraseña debe contener al menos una mayúscula.");
            if (!contrasena.Any(char.IsDigit))
                throw new ArgumentException("La contraseña debe contener al menos un número.");
            if (!contrasena.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
                throw new ArgumentException("La contraseña debe contener al menos un carácter especial.");
        }
    }
}
