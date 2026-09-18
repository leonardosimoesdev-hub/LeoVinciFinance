using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Consolidacao.Application.Commands;
using Consolidacao.Application.Events;
using Consolidacao.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consolidacao.BackgroundServices.HostedServices;

/// <summary>
/// Reprocessador de falhas (seção 29): busca Jobs com falha que ainda não excederam o limite
/// de tentativas e republica o evento "Iniciado" para cada um. Intervalo configurável via
/// <see cref="ConsolidacaoOptions.IntervaloRetryFalhas"/>.
/// </summary>
public class SaldoDiarioConsolidadoRetryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsolidacaoOptions _options;
    private readonly ILogger<SaldoDiarioConsolidadoRetryHostedService> _logger;

    public SaldoDiarioConsolidadoRetryHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ConsolidacaoOptions> options,
        ILogger<SaldoDiarioConsolidadoRetryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.IntervaloRetryFalhas);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken)) ;
        {
            await ExecutarAsync(stoppingToken);
        }
    }

    private async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IConsolidacaoEventRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IConsolidacaoEventPublisher>();

        IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity> elegiveis;
        try
        {
            elegiveis = await eventRepository.ObterComFalhasElegiveisParaRetryAsync(_options.LimiteTentativas, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao consultar Jobs elegíveis para retry; tentará novamente no próximo ciclo.");
            return;
        }

        if (elegiveis.Count == 0)
        {
            _logger.LogInformation("Reprocessador de falhas: nenhum Job elegível para retry neste ciclo.");
            return;
        }
        _logger.LogInformation("Reprocessador de falhas: republicando {Quantidade} eventos ComFalhas elegíveis.", elegiveis.Count);

        // Agrupa por IdConta+Data e republica um evento Iniciado por grupo usando o CorrelationId mais recente
        var porGrupo = elegiveis.GroupBy(e => (e.IdConta, e.Data));
        foreach (var grupo in porGrupo)
        {
            var ultimo = grupo.OrderByDescending(g => g.Tentativas).First();
            try
            {
                await eventPublisher.PublicarIniciadoAsync(
                    new Consolidacao.Application.Events.SaldoDiarioConsolidadoIniciadoEvento(Guid.Empty, ultimo.IdConta, ultimo.Data, ultimo.CorrelationId),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao republicar evento Iniciado para IdConta={IdConta} Data={Data} durante retry.", grupo.Key.IdConta, grupo.Key.Data);
            }
        }
    }
}
