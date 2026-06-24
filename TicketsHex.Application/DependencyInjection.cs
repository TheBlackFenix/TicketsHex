using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Application.CasosUso.TicketCasosUso;
using TicketsHex.Application.Puertos.Entrada.Ticket;
using TicketsHex.Application.Puertos.Salida;

namespace TicketsHex.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITicketCommand, TicketCommand>();
            services.AddScoped<ITicketQuery, TicketQuery>();

            return services;
        }
    }
}
