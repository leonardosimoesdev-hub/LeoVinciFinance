namespace Consolidacao.Application.Abstractions;

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
