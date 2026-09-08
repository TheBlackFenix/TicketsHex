using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using TicketsHex.API.Servicios;
using TicketsHex.Application.Comun.Paginacion;
using TicketsHex.Application.DTO_s.Autenticacion;
using TicketsHex.Application.DTO_s.Ticket;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using TicketsHex.Application.Puertos.Entrada.Notificacion;
using TicketsHex.Application.Puertos.Salida;
using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Entidades.Usuario;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public sealed class ApiAutorizacionIntegrationTests
{
    [Fact]
    public async Task Endpoint_protegido_sin_token_devuelve_error_estable()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/tickets/{factory.Ticket.IdTicket}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("SESION_INVALIDA", await ObtenerCodigoAsync(response));
    }

    [Fact]
    public async Task Ticket_inexistente_devuelve_codigo_especifico()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(1, Rol.Planner);

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("TICKET_NO_ENCONTRADO", await ObtenerCodigoAsync(response));
    }

    [Fact]
    public async Task Desarrollador_no_asignado_no_puede_modificar_ticket_por_uuid()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(11, Rol.Desarrollador);

        var response = await CambiarEstadoAsync(
            client,
            factory.Ticket.IdTicket,
            TicketEstado.EnProceso);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("TICKET_NO_ASIGNADO", await ObtenerCodigoAsync(response));
        Assert.Equal(TicketEstado.EnAnalisis, factory.Ticket.IdEstado);
    }

    [Fact]
    public async Task Desarrollador_asignado_puede_ejecutar_transicion_secuencial()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(10, Rol.Desarrollador);

        var response = await CambiarEstadoAsync(
            client,
            factory.Ticket.IdTicket,
            TicketEstado.EnProceso);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TicketEstado.EnProceso, factory.Ticket.IdEstado);
        Assert.Equal(1, factory.Repository.Actualizaciones);
    }

    [Fact]
    public async Task Transicion_no_secuencial_devuelve_conflicto_funcional()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(10, Rol.Desarrollador);

        var response = await CambiarEstadoAsync(
            client,
            factory.Ticket.IdTicket,
            TicketEstado.Certificado);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TRANSICION_INVALIDA", await ObtenerCodigoAsync(response));
    }

    [Fact]
    public async Task Planner_puede_ejecutar_override_de_estado_con_comentario()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(1, Rol.Planner);

        var response = await CambiarEstadoAsync(
            client,
            factory.Ticket.IdTicket,
            TicketEstado.Certificado,
            "Ajuste operativo autorizado.");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TicketEstado.Certificado, factory.Ticket.IdEstado);
    }

    [Fact]
    public async Task Politica_de_rol_devuelve_problem_details_uniforme()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(10, Rol.Desarrollador);

        var response = await client.PostAsJsonAsync("/api/usuarios", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("ACCION_NO_PERMITIDA", await ObtenerCodigoAsync(response));
    }

    [Fact]
    public async Task Token_con_cambio_obligatorio_solo_puede_cambiar_contrasena()
    {
        await using var factory = new TicketsApiFactory();
        using var client = factory.CrearClienteAutenticado(
            10,
            Rol.Desarrollador,
            debeCambiarContrasena: true);

        var bloqueada = await client.GetAsync($"/api/tickets/{factory.Ticket.IdTicket}");
        var cambio = await client.PostAsJsonAsync(
            "/api/auth/cambiar-contrasena",
            new
            {
                ContrasenaActual = "Temporal#2026",
                NuevaContrasena = "Nueva#2026"
            });

        Assert.Equal(HttpStatusCode.Forbidden, bloqueada.StatusCode);
        Assert.Equal("CAMBIO_CONTRASENA_REQUERIDO", await ObtenerCodigoAsync(bloqueada));
        Assert.Equal(HttpStatusCode.OK, cambio.StatusCode);
        Assert.Equal(10, factory.Authentication.UltimoUsuarioCambioContrasena);
    }

    private static Task<HttpResponseMessage> CambiarEstadoAsync(
        HttpClient client,
        Guid idTicket,
        TicketEstado estado,
        string? comentario = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/tickets/{idTicket}")
        {
            Content = JsonContent.Create(new
            {
                NuevoEstado = estado.ToString(),
                Comentario = comentario
            })
        };
        return client.SendAsync(request);
    }

    private static async Task<string?> ObtenerCodigoAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);
        return json.RootElement.GetProperty("code").GetString();
    }

    private sealed class TicketsApiFactory : WebApplicationFactory<Program>
    {
        private static readonly RSA Rsa;

        static TicketsApiFactory()
        {
            Rsa = ConfigurarEntornoPruebas();
        }

        public TicketsApiFactory()
        {
            Ticket = new Ticket(
                "CASO-HTTP-001",
                "Ticket para pruebas HTTP",
                "Descripción suficientemente extensa para pruebas HTTP.",
                10,
                1,
                TicketTipo.Incidente,
                TicketPrioridad.Alta,
                TicketImpacto.Alto,
                usuarioQa: 20);
            Repository = new TicketRepositoryFake(Ticket);
            Authentication = new AutenticacionServiceFake();
        }

        public Ticket Ticket { get; }
        public TicketRepositoryFake Repository { get; }
        public AutenticacionServiceFake Authentication { get; }

        public HttpClient CrearClienteAutenticado(
            long idUsuario,
            Rol rol,
            bool debeCambiarContrasena = false)
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                CrearToken(idUsuario, rol, debeCambiarContrasena));
            return client;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHostedService>();
                services.RemoveAll<IAutenticacionService>();
                services.RemoveAll<ITicketRepository>();
                services.RemoveAll<IUsuarioRepository>();
                services.RemoveAll<INotificacionTicketService>();

                services.AddSingleton<IAutenticacionService>(Authentication);
                services.AddSingleton<ITicketRepository>(Repository);
                services.AddSingleton<IUsuarioRepository>(new UsuarioRepositoryFake());
                services.AddScoped<INotificacionTicketService, NotificacionTicketServiceFake>();
            });
        }

        private static RSA ConfigurarEntornoPruebas()
        {
            var rsa = RSA.Create(2048);
            Environment.SetEnvironmentVariable("DatabaseProvider", "SqlServer");
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__DefaultConnection",
                "Server=(local);Database=TicketsHexTests;Trusted_Connection=True;TrustServerCertificate=True");
            Environment.SetEnvironmentVariable("Jwt__Issuer", "TicketsHex.Tests");
            Environment.SetEnvironmentVariable("Jwt__Audience", "TicketsHex.Tests.API");
            Environment.SetEnvironmentVariable("Jwt__ClientId", "TicketsHex.Tests.Client");
            Environment.SetEnvironmentVariable(
                "Jwt__PrivateKeyBase64",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ExportPkcs8PrivateKeyPem())));
            Environment.SetEnvironmentVariable(
                "Jwt__PublicKeyBase64",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(rsa.ExportSubjectPublicKeyInfoPem())));
            Environment.SetEnvironmentVariable("Jwt__AccessTokenMinutes", "15");
            Environment.SetEnvironmentVariable("Jwt__PasswordChangeTokenMinutes", "10");
            Environment.SetEnvironmentVariable("Jwt__ClockSkewSeconds", "0");
            Environment.SetEnvironmentVariable(
                "Usuarios__ContrasenaPorDefecto",
                "Temporal#2026");
            return rsa;
        }

        private string CrearToken(long idUsuario, Rol rol, bool debeCambiarContrasena)
        {
            var ahora = DateTimeOffset.UtcNow;
            var jti = $"{idUsuario}:{(int)rol}:{debeCambiarContrasena}";
            var key = new RsaSecurityKey(Rsa)
            {
                KeyId = JwtKeyLoader.CrearKeyId(Rsa)
            };
            var jwt = new JwtSecurityToken(
                issuer: "TicketsHex.Tests",
                audience: "TicketsHex.Tests.API",
                claims:
                [
                    new Claim(JwtRegisteredClaimNames.Sub, idUsuario.ToString()),
                    new Claim(JwtRegisteredClaimNames.UniqueName, $"usuario-{idUsuario}"),
                    new Claim(JwtRegisteredClaimNames.Jti, jti),
                    new Claim(
                        JwtRegisteredClaimNames.Iat,
                        ahora.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64),
                    new Claim("client_id", "TicketsHex.Tests.Client")
                ],
                notBefore: ahora.AddMinutes(-1).UtcDateTime,
                expires: ahora.AddMinutes(15).UtcDateTime,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
            jwt.Header[JwtHeaderParameterNames.Typ] = "at+jwt";
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }

    private sealed class AutenticacionServiceFake : IAutenticacionService
    {
        public long? UltimoUsuarioCambioContrasena { get; private set; }
        public Task<UsuarioAutenticadoDTO> ValidarSesionAsync(string jti)
        {
            var partes = jti.Split(':');
            return Task.FromResult(new UsuarioAutenticadoDTO(
                long.Parse(partes[0]),
                $"usuario-{partes[0]}",
                "Usuario HTTP",
                (Rol)int.Parse(partes[1]),
                Area.Mantenimiento,
                bool.Parse(partes[2])));
        }

        public Task CambiarContrasenaAsync(long idUsuario, CambiarContrasenaRequest request)
        {
            UltimoUsuarioCambioContrasena = idUsuario;
            return Task.CompletedTask;
        }

        public Task InicializarAsync(InicializarAutenticacionRequest request) =>
            throw new NotSupportedException();
        public Task<LoginResponse> IniciarSesionAsync(LoginRequest request) =>
            throw new NotSupportedException();
        public Task CerrarSesionAsync(string jti) => throw new NotSupportedException();
    }

    private sealed class TicketRepositoryFake(Ticket ticket) : ITicketRepository
    {
        public int Actualizaciones { get; private set; }

        public Task<Ticket?> ObtenerPorIdAsync(Guid id, bool incluirEliminados = false) =>
            Task.FromResult<Ticket?>(id == ticket.IdTicket ? ticket : null);

        public Task<PaginaResultado<Ticket>> ObtenerPaginaAsync(TicketFiltroRequest filtro) =>
            CrearPagina();
        public Task<PaginaResultado<Ticket>> ObtenerPaginaParaQaAsync(long idUsuario, TicketFiltroRequest filtro) =>
            CrearPagina();
        public Task<PaginaResultado<Ticket>> ObtenerPaginaPorAsignacionHistoricaAsync(long idUsuario, TicketFiltroRequest filtro) =>
            CrearPagina();
        public Task<IReadOnlyCollection<Ticket>> ObtenerCargaActivaUsuarioAsync(long idUsuario) =>
            Task.FromResult<IReadOnlyCollection<Ticket>>([]);
        public Task GuardarAsync(Ticket nuevoTicket) => Task.CompletedTask;
        public Task ActualizarRangoAsync(IReadOnlyCollection<Ticket> tickets) => Task.CompletedTask;

        public Task ActualizarAsync(Ticket ticketActualizado)
        {
            Actualizaciones++;
            return Task.CompletedTask;
        }

        private Task<PaginaResultado<Ticket>> CrearPagina() =>
            Task.FromResult(new PaginaResultado<Ticket>([ticket], 1, 20, 1));
    }

    private sealed class UsuarioRepositoryFake : IUsuarioRepository
    {
        public Task<Usuario?> ObtenerPorIdAsync(long idUsuario) => Task.FromResult<Usuario?>(null);
        public Task<IReadOnlyCollection<Usuario>> ObtenerTodosAsync(bool incluirInactivos) =>
            Task.FromResult<IReadOnlyCollection<Usuario>>([]);
        public Task<bool> ExisteAsync(long idUsuario) => Task.FromResult(true);
        public Task GuardarAsync(Usuario usuario) => Task.CompletedTask;
        public Task ActualizarAsync(Usuario usuario) => Task.CompletedTask;
    }

    private sealed class NotificacionTicketServiceFake : INotificacionTicketService
    {
        public ContextoNotificacionTicket CapturarContexto(Ticket ticket) =>
            new(ticket.IdEstado, false, false);
        public Task<PreparacionNotificacionTicket> PrepararCreacionAsync(Ticket ticket) =>
            Task.FromResult(PreparacionNotificacionTicket.Vacia);
        public Task<PreparacionNotificacionTicket> PrepararActualizacionAsync(
            Ticket ticket,
            ContextoNotificacionTicket contextoAnterior) =>
            Task.FromResult(PreparacionNotificacionTicket.Vacia);
        public Task<PreparacionNotificacionTicket> PrepararAsignacionAsync(
            Ticket ticket,
            long idUsuario,
            string mensaje) => Task.FromResult(PreparacionNotificacionTicket.Vacia);
        public Task<PreparacionNotificacionTicket> PrepararEliminacionAsync(Ticket ticket) =>
            Task.FromResult(PreparacionNotificacionTicket.Vacia);
        public Task PublicarAsync(
            Ticket ticket,
            PreparacionNotificacionTicket preparacion,
            bool publicarResumen = true) => Task.CompletedTask;
    }
}
