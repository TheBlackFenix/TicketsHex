using TicketsHex.Application.Comun.Configuracion;
using TicketsHex.Application.Comun.Seguridad;

namespace TicketsHex.API.Servicios
{
    public static class ConfiguracionAplicacion
    {
        public static void Registrar(
            IServiceCollection services,
            IConfiguration configuration)
        {
            var usuarios = configuration
                .GetSection(UsuariosOptions.SectionName)
                .Get<UsuariosOptions>() ?? new UsuariosOptions();
            var notificaciones = configuration
                .GetSection(NotificacionesOptions.SectionName)
                .Get<NotificacionesOptions>() ?? new NotificacionesOptions();
            var parametricos = configuration
                .GetSection(ParametricosOptions.SectionName)
                .Get<ParametricosOptions>() ?? new ParametricosOptions();
            var refresh = configuration
                .GetSection(RefreshOptions.SectionName)
                .Get<RefreshOptions>() ?? new RefreshOptions();

            Validar(usuarios, notificaciones, parametricos);
            if (refresh.DiasVigencia is < 1 or > 30)
                throw new InvalidOperationException("Refresh:DiasVigencia debe estar entre 1 y 30.");
            if (!Enum.TryParse<SameSiteMode>(refresh.CookieSameSite, true, out var sameSite) ||
                sameSite is not (SameSiteMode.Lax or SameSiteMode.None or SameSiteMode.Strict))
                throw new InvalidOperationException("Refresh:CookieSameSite debe ser Lax, None o Strict.");
            if (sameSite == SameSiteMode.None && !refresh.CookieSecure)
                throw new InvalidOperationException("Refresh:CookieSecure debe ser true cuando CookieSameSite es None.");
            services.AddSingleton(usuarios);
            services.AddSingleton(notificaciones);
            services.AddSingleton(parametricos);
            services.AddSingleton(refresh);
        }

        public static void Validar(
            UsuariosOptions usuarios,
            NotificacionesOptions notificaciones,
            ParametricosOptions parametricos)
        {
            if (usuarios.MaximoIntentosFallidos is < 1 or > 20)
                throw new InvalidOperationException(
                    "Usuarios:MaximoIntentosFallidos debe estar entre 1 y 20.");
            if (usuarios.DiasVigenciaContrasena is < 1 or > 3650)
                throw new InvalidOperationException(
                    "Usuarios:DiasVigenciaContrasena debe estar entre 1 y 3650.");
            if (notificaciones.DiasRetencion is < 1 or > 365)
                throw new InvalidOperationException(
                    "Notificaciones:DiasRetencion debe estar entre 1 y 365.");
            if (notificaciones.HorasIntervaloLimpieza is < 1 or > 168)
                throw new InvalidOperationException(
                    "Notificaciones:HorasIntervaloLimpieza debe estar entre 1 y 168.");
            if (parametricos.HorasCache is < 1 or > 168)
                throw new InvalidOperationException(
                    "Parametricos:HorasCache debe estar entre 1 y 168.");
        }
    }
}
