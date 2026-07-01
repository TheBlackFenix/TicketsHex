using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Comun.Seguridad;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;

namespace TicketsHex.API.Servicios
{
    public static class UsuarioActualEndpointExtensions
    {
        public static RouteGroupBuilder ConUsuarioAutenticado(this RouteGroupBuilder group)
        {
            group.AddEndpointFilter(async (context, next) =>
            {
                var token = ObtenerTokenBearer(context.HttpContext.Request);
                var autenticacion = context.HttpContext.RequestServices
                    .GetRequiredService<IAutenticacionService>();
                var identidad = await autenticacion.ValidarSesionAsync(token);

                var usuarioActual = context.HttpContext.RequestServices
                    .GetRequiredService<UsuarioActualTemporal>();
                usuarioActual.Establecer(identidad.IdUsuario, identidad.Rol);

                return await next(context);
            });

            return group;
        }

        public static string ObtenerTokenBearer(HttpRequest request)
        {
            var authorization = request.Headers.Authorization.FirstOrDefault();
            const string scheme = "Bearer ";

            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                throw new UsuarioNoAutenticadoException(
                    "Debe enviar un token Bearer válido.");

            var token = authorization[scheme.Length..].Trim();
            if (string.IsNullOrWhiteSpace(token))
                throw new UsuarioNoAutenticadoException(
                    "Debe enviar un token Bearer válido.");

            return token;
        }
    }
}
