using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository;
using TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository.Context;
using TicketsHex.infrastructure.Seguridad;

namespace TicketsHex.infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Debe configurar ConnectionStrings__DefaultConnection mediante " +
                    "una variable de entorno o un proveedor seguro de secretos.");

            ValidarSeguridadConexion(connectionString, configuration);

            services.AddDbContext<MantenimientoContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IParametroRepository, ParametroRepository>();
            services.AddScoped<IAutenticacionRepository, AutenticacionRepository>();
            services.AddScoped<IRepositorioRamaRepository, RepositorioRamaRepository>();
            services.AddSingleton<IContrasenaHasher, ContrasenaHasher>();

            return services;
        }

        private static void ValidarSeguridadConexion(
            string connectionString,
            IConfiguration configuration)
        {
            if (!configuration.GetValue("Database:RequireCertificateValidation", true))
                return;

            var opciones = new NpgsqlConnectionStringBuilder(connectionString);
            if (opciones.SslMode is not SslMode.VerifyCA and not SslMode.VerifyFull)
            {
                throw new InvalidOperationException(
                    "La conexión PostgreSQL debe validar el certificado del servidor. " +
                    "Configure SSL Mode=VerifyFull y Root Certificate en la cadena segura.");
            }
        }
    }
}
