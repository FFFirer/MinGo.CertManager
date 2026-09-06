using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MinGo.CertManager.Web.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var otelSection = configuration.GetSection("OpenTelemetry");
        var serviceName = otelSection["ServiceName"] ?? "MinGo.CertManager";
        var otlpEndpoint = otelSection["OtlpEndpoint"] ?? "http://localhost:4317";
        var otlpProtocol = otelSection["OtlpProtocol"]?.ToLowerInvariant() switch
        {
            "http/protobuf" or "http" => OtlpExportProtocol.HttpProtobuf,
            _ => OtlpExportProtocol.Grpc
        };

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });

                if (otelSection.GetValue<bool>("EnableOtlpExporter", true))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = otlpProtocol;
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation();

                if (otelSection.GetValue<bool>("EnableOtlpExporter", true))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                        options.Protocol = otlpProtocol;
                    });
                }

                if (otelSection.GetValue<bool>("EnablePrometheusExporter", false))
                {
                    metrics.AddPrometheusExporter();
                }
            });
            // Logs 由 Serilog.Sinks.OpenTelemetry 直接导出，不走 OTel SDK Logging Provider

        return services;
    }
}
