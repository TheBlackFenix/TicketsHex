using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketsHex.API.Servicios;
using TicketsHex.Application.Comun.Seguridad;
using TicketsHex.Application.Puertos.Entrada.Autenticacion;
using TicketsHex.Application.Puertos.Salida;
using Serilog;
using System.IO.Compression;
using TicketsHex.API.Endpoints;
using TicketsHex.API.Middelwares;
using TicketsHex.API.Middelwares.ExceptionHandling;
using TicketsHex.Application;
using TicketsHex.infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.OutputCaching;



Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();
try
{
    Log.Information("Iniciando el servicio");

    var builder = WebApplication.CreateBuilder(args);
    var railwayPort = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrWhiteSpace(railwayPort))
    {
        if (!int.TryParse(railwayPort, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException(
                "La variable PORT debe contener un puerto válido.");

        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    builder.Configuration
    .AddJsonFile("ErrorMessages.json", optional: false, reloadOnChange: true);

    builder.Services.Configure<ExceptionHandlingOptions>(
        builder.Configuration.GetSection("ExceptionHandling"));
    builder.Services.AddSingleton<ExceptionMessageResolver>();

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

    var jwtOptions = builder.Configuration
        .GetSection(JwtOptions.SectionName)
        .Get<JwtOptions>()
        ?? throw new InvalidOperationException("No existe la configuración JWT.");
    JwtKeyLoader.ValidarOpciones(jwtOptions);
    using var validationPrivateRsa = JwtKeyLoader.CargarClavePrivada(
        jwtOptions,
        builder.Environment);
    var publicRsa = JwtKeyLoader.CargarClavePublica(jwtOptions, builder.Environment);
    JwtKeyLoader.ValidarPar(validationPrivateRsa, publicRsa);
    var publicKey = new RsaSecurityKey(publicRsa)
    {
        KeyId = JwtKeyLoader.CrearKeyId(publicRsa)
    };

    builder.Services.Configure<JwtOptions>(
        builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.AddSingleton(publicRsa);
    builder.Services.AddSingleton<IGeneradorJwtSesion, GeneradorJwtSesion>();
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.IncludeErrorDetails = builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = publicKey,
                ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                ValidTypes = ["at+jwt"],
                TryAllIssuerSigningKeys = false,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
                NameClaimType = JwtRegisteredClaimNames.UniqueName,
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var jti = context.Principal?
                        .FindFirst(JwtRegisteredClaimNames.Jti)?
                        .Value;
                    var subject = context.Principal?
                        .FindFirst(JwtRegisteredClaimNames.Sub)?
                        .Value;
                    var issuedAt = context.Principal?
                        .FindFirst(JwtRegisteredClaimNames.Iat)?
                        .Value;
                    var clientId = context.Principal?
                        .FindFirst("client_id")?
                        .Value;
                    if (string.IsNullOrWhiteSpace(jti) ||
                        !long.TryParse(subject, out var subjectId) ||
                        !long.TryParse(issuedAt, out _) ||
                        !string.Equals(
                            clientId,
                            jwtOptions.ClientId,
                            StringComparison.Ordinal))
                    {
                        context.Fail("El token no contiene las claims obligatorias.");
                        return;
                    }

                    try
                    {
                        var autenticacion = context.HttpContext.RequestServices
                            .GetRequiredService<IAutenticacionService>();
                        var identidad = await autenticacion.ValidarSesionAsync(jti);
                        if (identidad.IdUsuario != subjectId)
                        {
                            context.Fail("El sujeto no coincide con la sesión.");
                            return;
                        }
                        var usuarioActual = context.HttpContext.RequestServices
                            .GetRequiredService<UsuarioActualTemporal>();
                        usuarioActual.Establecer(identidad.IdUsuario, identidad.Rol);
                    }
                    catch (Exception exception)
                    {
                        context.Fail(exception);
                    }
                }
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("PlannerOrLiderTecnico", policy =>
            policy.RequireRole("Planner", "LiderTecnico"));
    });
    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy(ParametricosEndpoints.CachePolicyName, policy =>
            policy
                .Expire(TimeSpan.FromHours(12))
                .Tag(ParametricosEndpoints.CacheTag));
    });
    builder.Services.AddHealthChecks();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });
    // Add services to the container.
    builder.Services.AddSerilog((services, configuration) =>
    {
        configuration.ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
    });
    // Add services to the container.1
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "Opaque",
            In = ParameterLocation.Header,
            Description = "Token obtenido desde POST /api/auth/login."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    });

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;

        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();

        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
        {
        "application/json",
        "text/plain",
        "text/csv",
        "application/xml",
        "text/xml"
        });
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    var app = builder.Build();
    app.UseGlobalExceptionHandling();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    app.UseResponseCompression();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseOutputCache();
    app.MapHealthChecks("/health", new HealthCheckOptions())
        .AllowAnonymous();
    app.MapTicketEndpoints();
    app.MapParametricosEndpoints();

    await app.RunAsync();

}
catch (Exception ex)
{
    Log.Fatal(ex, "Ocurrió un error inesperado");
}
finally
{
    Log.Information("Terminando el servicio");
    await Log.CloseAndFlushAsync();
}
