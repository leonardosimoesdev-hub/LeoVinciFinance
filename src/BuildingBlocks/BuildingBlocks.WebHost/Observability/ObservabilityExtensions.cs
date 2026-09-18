using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;

namespace BuildingBlocks.WebHost.Observability;

/// <summary>
/// Observabilidade compartilhada: Serilog (logs estruturados) + OpenTelemetry (traces),
/// exportando para o Aspire Dashboard Standalone via OTLP (Especificação Mestre, seção 31).
/// </summary>
public static class ObservabilityExtensions
{
    public static IHostBuilder UseSharedSerilog(this IHostBuilder hostBuilder, string serviceName) =>
        hostBuilder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithMachineName()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] ({Service}) {CorrelationId} {Message:lj}{NewLine}{Exception}");
        }, writeToProviders: true);

    public static HostApplicationBuilder UseSharedSerilog(
    this HostApplicationBuilder builder,
    string serviceName)
    {
        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .Enrich.WithMachineName()
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] ({Service}) {CorrelationId} {Message:lj}{NewLine}{Exception}");
        },writeToProviders: true);

        return builder;
    }

    public static IServiceCollection AddSharedOpenTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var otlpEndpoint = configuration["Otel:Endpoint"] ?? "http://aspire-dashboard:18889";

        services.AddHttpContextAccessor();
        services.AddSingleton<BuildingBlocks.Common.Abstractions.ICorrelationContextAccessor, HttpCorrelationContextAccessor>();

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;
            options.AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint));
        }));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(otlp => otlp.Endpoint = new Uri(otlpEndpoint)));

        return services;
    }

    /// <summary>
    /// Middleware que garante um IdCorrelationId por requisição (lido do header
    /// "X-Correlation-Id" quando presente — propagado a partir da chamada anterior — ou
    /// gerado quando ausente) e o injeta no contexto de log estruturado.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            const string headerName = "X-Correlation-Id";

            var correlationId = context.Request.Headers.TryGetValue(headerName, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.ToString()
                : Guid.NewGuid().ToString();

            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers[headerName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }
}
