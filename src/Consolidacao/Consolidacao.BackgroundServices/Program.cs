using BuildingBlocks.WebHost.Observability;
using Consolidacao.BackgroundServices.HostedServices;
using Consolidacao.Infrastructure;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.UseSharedSerilog(serviceName: "Consolidacao.BackgroundServices");

builder.Services.AddConsolidacaoModule(builder.Configuration);

builder.Services.AddSharedOpenTelemetry(
    builder.Configuration,
    serviceName: "Consolidacao.BackgroundServices");

builder.Services.AddHostedService<SaldoDiarioConsolidadoAgendadorHostedService>();
builder.Services.AddHostedService<SaldoDiarioConsolidadoRetryHostedService>();
builder.Services.AddHostedService<SaldoDiarioConsolidadoLacunasHostedService>();

builder.Services.AddHealthChecks();

var host = builder.Build();

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
