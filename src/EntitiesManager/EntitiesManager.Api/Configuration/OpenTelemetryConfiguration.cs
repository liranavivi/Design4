using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace EntitiesManager.Api.Configuration;

public static class OpenTelemetryConfiguration
{
    public static IServiceCollection AddOpenTelemetryObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = "EntitiesManager";
        var serviceVersion = "1.0.0";

        // Configure resource
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddTelemetrySdk()
            .AddEnvironmentVariableDetector();

        // Add OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            // Filter out health check requests
                            return !httpContext.Request.Path.StartsWithSegments("/health");
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource("EntitiesManager.*")
                    .AddSource("MassTransit")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                    });
            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("EntitiesManager.*")
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                    });
            })
            .WithLogging(builder =>
            {
                builder
                    .SetResourceBuilder(resourceBuilder)
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317");
                    });

                // Only add console exporter in development when collector is not available
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
                var useConsoleForDev = configuration.GetValue<bool>("OpenTelemetry:UseConsoleInDevelopment", true);

                if (isDevelopment && useConsoleForDev)
                {
                    builder.AddConsoleExporter();
                }
            });

        return services;
    }
}
