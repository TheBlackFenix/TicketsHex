using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.infrastructure.Seguridad;
using SqlServerContext = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.Context.MantenimientoContext;
using SqlServerAplicativoRepository = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.AplicativoRepository;
using SqlServerAutenticacionRepository = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.AutenticacionRepository;
using SqlServerParametroRepository = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.ParametroRepository;
using SqlServerRepositorioRamaRepository = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.RepositorioRamaRepository;
using SqlServerTicketRepository = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.TicketRepository;
using SqlServerUsuarioRepository = TicketsHex.infrastructure.Adaptadores.Persistence.SqlServerRepository.UsuarioRepository;
using PostgreSqlContext = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.Context.MantenimientoContext;
using PostgreSqlAplicativoRepository = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.AplicativoRepository;
using PostgreSqlAutenticacionRepository = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.AutenticacionRepository;
using PostgreSqlParametroRepository = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.ParametroRepository;
using PostgreSqlRepositorioRamaRepository = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.RepositorioRamaRepository;
using PostgreSqlTicketRepository = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.TicketRepository;
using PostgreSqlUsuarioRepository = TicketsHex.infrastructure.Adaptadores.Persistence.PostgreSqlRepository.UsuarioRepository;

namespace TicketsHex.infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var databaseProvider = configuration["DatabaseProvider"];

            if (string.Equals(databaseProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase))
                RegistrarPostgreSql(services, connectionString);
            else
                RegistrarSqlServer(services, connectionString);

            services.AddSingleton<IContrasenaHasher, ContrasenaHasher>();

            return services;
        }

        private static void RegistrarSqlServer(IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<SqlServerContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<ITicketRepository, SqlServerTicketRepository>();
            services.AddScoped<IUsuarioRepository, SqlServerUsuarioRepository>();
            services.AddScoped<IParametroRepository, SqlServerParametroRepository>();
            services.AddScoped<IAutenticacionRepository, SqlServerAutenticacionRepository>();
            services.AddScoped<IRepositorioRamaRepository, SqlServerRepositorioRamaRepository>();
            services.AddScoped<IAplicativoRepository, SqlServerAplicativoRepository>();
        }

        private static void RegistrarPostgreSql(IServiceCollection services, string? connectionString)
        {
            services.AddDbContext<PostgreSqlContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<ITicketRepository, PostgreSqlTicketRepository>();
            services.AddScoped<IUsuarioRepository, PostgreSqlUsuarioRepository>();
            services.AddScoped<IParametroRepository, PostgreSqlParametroRepository>();
            services.AddScoped<IAutenticacionRepository, PostgreSqlAutenticacionRepository>();
            services.AddScoped<IRepositorioRamaRepository, PostgreSqlRepositorioRamaRepository>();
            services.AddScoped<IAplicativoRepository, PostgreSqlAplicativoRepository>();
        }
    }
}
