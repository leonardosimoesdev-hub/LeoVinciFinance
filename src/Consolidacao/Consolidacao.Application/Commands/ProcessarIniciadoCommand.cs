using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Consolidacao.Application.Events;
using Microsoft.Extensions.Logging;

namespace Consolidacao.Application.Commands;

/// <summary>
/// Consumidor do evento "Iniciado" (seção 25/26): busca os lançamentos do dia em
/// Financeiro.Api, soma, e publica o resultado em Relatorios.Api — ou publica "ComFalhas" se
/// qualquer etapa falhar. Não muta o estado do Job diretamente: quem registra a etapa/execução
/// definitiva são os handlers de "Concluído"/"ComFalhas" (separação entre "fazer o trabalho" e
/// "registrar o resultado", consistente com os 3 consumidores desenhados em
/// docs/c4/component.md).
/// </summary>
public record ProcessarIniciadoCommand(Guid IdJob, Guid IdConta, DateOnly Data, Guid CorrelationId) : ICommand<Result<bool>>;

public class ProcessarIniciadoCommandHandler : ICommandHandler<ProcessarIniciadoCommand, Result<bool>>
{
    private readonly IFinanceiroGateway _financeiroGateway;
    private readonly IRelatoriosGateway _relatoriosGateway;
    private readonly IConsolidacaoEventPublisher _eventPublisher;
    private readonly ILogger<ProcessarIniciadoCommandHandler> _logger;

    public ProcessarIniciadoCommandHandler(
        IFinanceiroGateway financeiroGateway,
        IRelatoriosGateway relatoriosGateway,
        IConsolidacaoEventPublisher eventPublisher,
        ILogger<ProcessarIniciadoCommandHandler> logger)
    {
        _financeiroGateway = financeiroGateway;
        _relatoriosGateway = relatoriosGateway;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ProcessarIniciadoCommand command, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = command.CorrelationId,
            ["IdJob"] = command.IdJob
        });

        try
        {
            var saldoExistente = await _relatoriosGateway.ObterSaldoAsync(command.IdConta, command.Data, cancellationToken);
            if (saldoExistente is not null)
            {
                // Idempotência de sistema (ADR 0007): Relatorios já tem o saldo dessa
                // conta/data — provavelmente reprocessamento do mesmo Job. Considera concluído
                // sem chamar Financeiro/Relatorios de novo.
                await _eventPublisher.PublicarConcluidoAsync(
                    new SaldoDiarioConsolidadoConcluidoEvento(command.IdJob, command.IdConta, command.Data, saldoExistente.Value, command.CorrelationId,
                        "Saldo já existia em Relatorios.Api (reprocessamento idempotente)."),
                    cancellationToken);

                return Result<bool>.Success(true);
            }

            var lancamentos = await _financeiroGateway.ObterLancamentosPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);

            if (lancamentos.Count == 0)
            {
                await _eventPublisher.PublicarConcluidoAsync(
                    new SaldoDiarioConsolidadoConcluidoEvento(command.IdJob, command.IdConta, command.Data, 0m, command.CorrelationId,
                        "Nenhum lançamento encontrado para a data; nada a consolidar."),
                    cancellationToken);

                return Result<bool>.Success(true);
            }

            var saldo = lancamentos.Sum(l => l.Valor);

            if (saldo == 0)
            {
                // Empate exato entre créditos e débitos no dia — Relatorios.Domain rejeita
                // saldo zero (mesma regra do Lancamento). Tratado como conclusão "sem
                // publicação", não como falha (não é um erro transitório recuperável).
                await _eventPublisher.PublicarConcluidoAsync(
                    new SaldoDiarioConsolidadoConcluidoEvento(command.IdJob, command.IdConta, command.Data, 0m, command.CorrelationId,
                        "Saldo consolidado igual a zero; não publicado em Relatorios.Api (regra de domínio)."),
                    cancellationToken);

                return Result<bool>.Success(true);
            }

            await _relatoriosGateway.PublicarSaldoDiarioConsolidadoAsync(command.IdConta, command.Data, saldo, cancellationToken);

            await _eventPublisher.PublicarConcluidoAsync(
                new SaldoDiarioConsolidadoConcluidoEvento(command.IdJob, command.IdConta, command.Data, saldo, command.CorrelationId,
                    "Saldo consolidado e publicado com sucesso."),
                cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao processar consolidação IdJob={IdJob} IdConta={IdConta} Data={Data}.", command.IdJob, command.IdConta, command.Data);

            await _eventPublisher.PublicarComFalhasAsync(
                new SaldoDiarioConsolidadoComFalhasEvento(command.IdJob, command.IdConta, command.Data, command.CorrelationId, ex.Message),
                cancellationToken);

            return Result<bool>.Success(false);
        }
    }
}
