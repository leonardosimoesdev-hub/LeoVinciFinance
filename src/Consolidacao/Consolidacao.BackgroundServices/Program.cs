using Consolidacao.BackgroundServices.HostedServices;
using Consolidacao.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using BuildingBlocks.WebHost.Observability;
using Serilog;

var hostBuilder = new HostBuilder()
    .ConfigureHostConfiguration(cfg =>
    {
        cfg.AddEnvironmentVariables();
        cfg.AddCommandLine(args);
    })
    .UseSharedSerilog(serviceName: "Consolidacao.BackgroundServices")
    .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
    {
        services.AddConsolidacaoModule(context.Configuration);
        services.AddHostedService<SaldoDiarioConsolidadoAgendadorHostedService>();
        services.AddHostedService<SaldoDiarioConsolidadoRetryHostedService>();
        services.AddHostedService<SaldoDiarioConsolidadoLacunasHostedService>();
        services.AddHealthChecks();
    });

var host = hostBuilder.Build();

try
{
    Log.Information("Consolidacao.BackgroundServices iniciando (consumidores Kafka + agendador + retry + detecção de lacunas)...");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Consolidacao.BackgroundServices encerrado de forma inesperada.");
}
finally
{
    Log.CloseAndFlush();
}
