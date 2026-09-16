using Consolidacao.Domain.Entities;

namespace Consolidacao.Application.Abstractions;

public interface IJobRepository
{
    Task<SaldoDiarioConsolidadoJob?> ObterPorIdAsync(Guid idJob, CancellationToken cancellationToken);

    Task<SaldoDiarioConsolidadoJob?> ObterPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);

    /// <summary>
    /// Jobs com falha (última etapa = ComFalhas) que ainda não excederam o limite de
    /// tentativas — candidatos a reprocessamento pelo SaldoDiarioConsolidadoComFalhasHostedService
    /// (seção 29).
    /// </summary>
    Task<IReadOnlyList<SaldoDiarioConsolidadoJob>> ObterElegiveisParaRetryAsync(CancellationToken cancellationToken);

    /// <summary>Datas já concluídas com sucesso para a conta — usado na detecção de lacunas (seção 30).</summary>
    Task<IReadOnlyList<DateOnly>> ObterDatasConcluidasAsync(Guid idConta, CancellationToken cancellationToken);

    Task AddAsync(SaldoDiarioConsolidadoJob job, CancellationToken cancellationToken);
}

public record ContaDto(Guid Id, Guid IdCliente);

public record LancamentoDto(Guid Id, Guid IdConta, decimal Valor, DateOnly Data);

/// <summary>Gateway HTTP resiliente (Refit + Polly via BuildingBlocks.ServiceAuth) para Financeiro.Api.</summary>
public interface IFinanceiroGateway
{
    Task<IReadOnlyList<ContaDto>> ObterTodasContasAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<LancamentoDto>> ObterLancamentosPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);
}

/// <summary>Gateway HTTP resiliente para Relatorios.Api.</summary>
public interface IRelatoriosGateway
{
    /// <summary>Retorna o saldo já existente para (idConta, data), ou null se ainda não consolidado.</summary>
    Task<decimal?> ObterSaldoAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);

    Task PublicarSaldoDiarioConsolidadoAsync(Guid idConta, DateOnly data, decimal saldo, CancellationToken cancellationToken);
}
