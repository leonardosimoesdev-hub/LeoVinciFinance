using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Consolidacao.Application.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consolidacao.BackgroundServices.HostedServices;

/// <summary>
/// Agendador diário (seção 25): para cada conta do sistema, inicia a consolidação de D-1.
/// Intervalo de execução vem de <see cref="ConsolidacaoOptions.IntervaloAgendador"/> — sem
/// magic numbers no código (Ajustes round 1).
/// </summary>
public class SaldoDiarioConsolidadoAgendadorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsolidacaoOptions _options;
    private readonly ILogger<SaldoDiarioConsolidadoAgendadorHostedService> _logger;

    public SaldoDiarioConsolidadoAgendadorHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ConsolidacaoOptions> options,
        ILogger<SaldoDiarioConsolidadoAgendadorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.IntervaloAgendador);

        do
        {
            await ExecutarAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExecutarAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var financeiroGateway = scope.ServiceProvider.GetRequiredService<IFinanceiroGateway>();
        var iniciarHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler<IniciarConsolidacaoCommand, Result<bool>>>();

        var ontem = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        _logger.LogInformation("Agendador diário: iniciando consolidação de {Data} para todas as contas.", ontem);

        IReadOnlyList<ContaDto> contas;
        try
        {
            contas = await financeiroGateway.ObterTodasContasAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao obter lista de contas de Financeiro.Api; agendador tentará novamente no próximo ciclo.");
            return;
        }

        var criados = 0;
        foreach (var conta in contas)
        {
            try
            {
                var resultado = await iniciarHandler.HandleAsync(new IniciarConsolidacaoCommand(conta.Id, ontem), cancellationToken);
                if (resultado.IsSuccess && resultado.Value)
                    criados++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao iniciar consolidação da conta {IdConta} na data {Data}.", conta.Id, ontem);
            }
        }

        _logger.LogInformation("Agendador diário concluído para {Data}: {Criados}/{Total} Jobs criados (demais já existiam).", ontem, criados, contas.Count);
    }
}
