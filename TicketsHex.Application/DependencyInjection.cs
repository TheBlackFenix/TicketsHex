using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.CasosUso.TicketCasosUso;
using TicketsHex.Application.CasosUso.UsuarioCasosUso;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Entrada.Usuario;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Application.Comun.Seguridad;

namespace TicketsHex.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITicketCommand, TicketCommand>();
            services.AddScoped<ITicketQuery, TicketQuery>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<UsuarioActualTemporal>();
            services.AddScoped<IUsuarioActual>(provider =>
                provider.GetRequiredService<UsuarioActualTemporal>());

            return services;
        }
    }
}
