using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository;
using TicketsHex.infrastructure.Adaptadores.Persistence.PgRepository.Context;

namespace TicketsHex.infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Configurar la conexión a PostgreSQL
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<MantenimientoContext>(options =>
                options.UseNpgsql(connectionString));

            // 2. Registrar los Adaptadores de Salida (Repositorios)
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();

            return services;
        }
    }
}
