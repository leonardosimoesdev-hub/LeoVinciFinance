using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Consolidacao.Application.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consolidacao.BackgroundServices.HostedServices;

/// <summary>
/// Detector de lacunas (seção 30): para cada conta, verifica se há datas entre
/// <see cref="ConsolidacaoOptions.DataMinimaVarreduraLacunas"/> e D-1 que ainda não possuem
/// um Job concluído, e inicia a consolidação dessas datas — cobre o cenário em que um dia
/// inteiro de consolidação foi perdido (ex.: o serviço ficou fora do ar durante a janela do
/// agendador diário). Intervalo configurável via
/// <see cref="ConsolidacaoOptions.IntervaloDeteccaoLacunas"/>.
/// </summary>
public class SaldoDiarioConsolidadoLacunasHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsolidacaoOptions _options;
    private readonly ILogger<SaldoDiarioConsolidadoLacunasHostedService> _logger;

    public SaldoDiarioConsolidadoLacunasHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ConsolidacaoOptions> options,
        ILogger<SaldoDiarioConsolidadoLacunasHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.IntervaloDeteccaoLacunas);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken)) 
        {
            await ExecutarAsync(stoppingToken);
        }
    }

    private async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var financeiroGateway = scope.ServiceProvider.GetRequiredService<IFinanceiroGateway>();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IConsolidacaoEventRepository>();
        var iniciarHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<IniciarConsolidacaoCommand, Result<bool>>>();

        var ontem = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        IReadOnlyList<ContaDto> contas;
        try
        {
            contas = await financeiroGateway.ObterTodasContasAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao obter lista de contas de Financeiro.Api; detector de lacunas tentará novamente no próximo ciclo.");
            return;
        }

        var lacunasEncontradas = 0;

        foreach (var conta in contas)
        {
            IReadOnlyList<DateOnly> datasConcluidas;
            try
            {
                datasConcluidas = await eventRepository.ObterDatasConcluidasAsync(conta.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao consultar datas concluídas da conta {IdConta}; pulando esta conta neste ciclo.", conta.Id);
                continue;
            }

            var concluidasSet = datasConcluidas.ToHashSet();

            for (var data = _options.DataMinimaVarreduraLacunas; data <= ontem; data = data.AddDays(1))
            {
                if (concluidasSet.Contains(data))
                    continue;

                lacunasEncontradas++;

                try
                {
                    await iniciarHandler.HandleAsync(new IniciarConsolidacaoCommand(conta.Id, data), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao iniciar consolidação de lacuna para IdConta={IdConta} Data={Data}.", conta.Id, data);
                }
            }
        }

        _logger.LogInformation("Detector de lacunas concluído: {Lacunas} lacuna(s) encontrada(s) e reenviada(s) para consolidação.", lacunasEncontradas);
    }
}
