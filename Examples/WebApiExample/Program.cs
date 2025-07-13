using AspireRunner.AspNetCore;
using AspireRunner.Installer;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure OpenTelemetry with the provided configuration
ConfigureOtel(builder.Services, builder.Configuration.GetSection("OpenTelemetry"));

// Add the aspire dashboard to the application
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAspireDashboard(options => builder.Configuration.GetSection("AspireDashboard").Bind(options));
    builder.Services.AddAspireDashboardInstaller();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
return;

static void ConfigureOtel(IServiceCollection services, IConfiguration otelConfig)
{
    services.AddOpenTelemetry()
        .WithTracing(traceBuilder => traceBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(config =>
            {
                config.Endpoint = new Uri(otelConfig["Endpoint"]!);
                config.Protocol = OtlpExportProtocol.Grpc;
            })
        )
        .WithMetrics(metricsBuilder => metricsBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(config =>
            {
                config.Endpoint = new Uri(otelConfig["Endpoint"]!);
                config.Protocol = OtlpExportProtocol.Grpc;
            })
        )
        .ConfigureResource(resourceBuilder => resourceBuilder
            .AddService("WebApiExample")
        );

    services.AddLogging(loggingBuilder => loggingBuilder
        .AddOpenTelemetry(otelLogging =>
        {
            otelLogging.IncludeScopes = true;
            otelLogging.ParseStateValues = true;
            otelLogging.AddConsoleExporter();

            otelLogging.AddOtlpExporter(config =>
            {
                config.Endpoint = new Uri(otelConfig["Endpoint"]!);
                config.Protocol = OtlpExportProtocol.Grpc;
            });
        })
    );
}