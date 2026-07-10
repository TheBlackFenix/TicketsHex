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
using TicketsHex.Application.CasosUso.ParametroCasosUso;
using TicketsHex.Application.Puertos.Entrada.Parametro;
using TicketsHex.Application.CasosUso.AutenticacionCasosUso;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using TicketsHex.Application.CasosUso.RepositorioCasosUso;
using TicketsHex.Application.Puertos.Entrada.Repositorio;
using TicketsHex.Application.CasosUso.AplicativoCasosUso;
using TicketsHex.Application.Puertos.Entrada.Aplicativo;
using TicketsHex.Application.CasosUso.NotificacionCasosUso;
using TicketsHex.Application.Puertos.Entrada.Notificacion;

namespace TicketsHex.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITicketCommand, TicketCommand>();
            services.AddScoped<ITicketQuery, TicketQuery>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IParametroQuery, ParametroQuery>();
            services.AddScoped<IAutenticacionService, AutenticacionService>();
            services.AddScoped<IRepositorioRamaService, RepositorioRamaService>();
            services.AddScoped<IAplicativoService, AplicativoService>();
            services.AddScoped<INotificacionQuery, NotificacionQuery>();
            services.AddScoped<UsuarioActualTemporal>();
            services.AddScoped<IUsuarioActual>(provider =>
                provider.GetRequiredService<UsuarioActualTemporal>());

            return services;
        }
    }
}
