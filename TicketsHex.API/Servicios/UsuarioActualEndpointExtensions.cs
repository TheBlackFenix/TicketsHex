using TicketsHex.Application.Comun.Excepciones;
using TicketsHex.Application.Comun.Seguridad;
using TicketsHex.Domain.Enums;

namespace TicketsHex.API.Servicios
{
    public static class UsuarioActualEndpointExtensions
    {
        public static RouteGroupBuilder ConUsuarioActualTemporal(this RouteGroupBuilder group)
        {
            group.AddEndpointFilter(async (context, next) =>
            {
                var request = context.HttpContext.Request;
                var idValue = request.Headers["X-User-Id"].FirstOrDefault();
                var roleValue = request.Headers["X-User-Role"].FirstOrDefault();

                if (!long.TryParse(idValue, out var idUsuario) || idUsuario <= 0)
                    throw new UsuarioNoAutenticadoException(
                        "Debe enviar un encabezado X-User-Id válido.");

                if (!Enum.TryParse<Rol>(roleValue, true, out var rol) || !Enum.IsDefined(rol))
                    throw new UsuarioNoAutenticadoException(
                        "Debe enviar un encabezado X-User-Role válido.");

                var usuarioActual = context.HttpContext.RequestServices
                    .GetRequiredService<UsuarioActualTemporal>();
                usuarioActual.Establecer(idUsuario, rol);

                return await next(context);
            });

            return group;
        }
    }
}
