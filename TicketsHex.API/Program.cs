using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using Serilog;
using System.IO.Compression;
using TicketsHex.API.Endpoints;
using TicketsHex.API.Middelwares;
using TicketsHex.API.Middelwares.ExceptionHandling;
using TicketsHex.Application;
using TicketsHex.infrastructure;



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
    builder.Configuration
    .AddJsonFile("ErrorMessages.json", optional: false, reloadOnChange: true);

    builder.Services.Configure<ExceptionHandlingOptions>(
        builder.Configuration.GetSection("ExceptionHandling"));
    builder.Services.AddSingleton<ExceptionMessageResolver>();

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
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
    app.UseHttpsRedirection();
    app.MapTicketEndpoints();

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
