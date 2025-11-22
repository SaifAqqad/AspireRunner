using AspireRunner.AspNetCore;
using AspireRunner.Installer;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure OpenTelemetry with the provided configuration
var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
builder.Services.AddOpenTelemetry()
    .WithTracing(traceBuilder => traceBuilder
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(ConfigureOtelExporter))
    .WithMetrics(metricsBuilder => metricsBuilder
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(ConfigureOtelExporter))
    .WithLogging(logsBuilder => logsBuilder
        .AddOtlpExporter(ConfigureOtelExporter))
    .ConfigureResource(resourceBuilder => resourceBuilder
        .AddService("WebApiExample"));

// Add the Aspire Dashboard and installer to the application
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAspireDashboard(options => builder.Configuration.GetSection("AspireDashboard").Bind(options));
    builder.Services.AddAspireDashboardInstaller();
}

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(scalar => scalar
    .WithClassicLayout()
    .WithTheme(ScalarTheme.Laserwave)
);

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () => Enumerable
        .Range(1, 5)
        .Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        })
        .ToArray())
    .WithName("GetWeatherForecast");

app.MapGet("/", () => Results.Redirect("/scalar"))
    .ExcludeFromDescription();

app.Run();


void ConfigureOtelExporter(OtlpExporterOptions config)
{
    config.Endpoint = new Uri(otelConfig["Endpoint"]!);
    config.Protocol = OtlpExportProtocol.Grpc;
}

record WeatherForecast
{
    public DateOnly Date { get; init; }

    public int TemperatureC { get; init; }

    public string? Summary { get; init; }
}