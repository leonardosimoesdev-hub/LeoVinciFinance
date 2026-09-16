using Refit;

namespace Consolidacao.Infrastructure.Gateways;

public interface IFinanceiroApiClient
{
    [Get("/api/financeiro/contas")]
    Task<IReadOnlyList<ContaResponseDto>> ObterTodasContasAsync([Header("Authorization")] string authorization);

    [Get("/api/financeiro/contas/{idConta}/lancamentos/{data}")]
    Task<IReadOnlyList<LancamentoResponseDto>> ObterLancamentosPorContaEDataAsync(
        Guid idConta, DateOnly data, [Header("Authorization")] string authorization);
}

public record ContaResponseDto(Guid Id, Guid IdCliente);
public record LancamentoResponseDto(Guid Id, Guid IdConta, decimal Valor, DateOnly Data);

public interface IRelatoriosApiClient
{
    [Get("/api/relatorios/saldo-diario-consolidado")]
    Task<ApiResponse<SaldoResponseDto>> ObterSaldoAsync(Guid idConta, DateOnly data, [Header("Authorization")] string authorization);

    [Post("/api/relatorios/saldo-diario-consolidado")]
    Task PublicarSaldoDiarioConsolidadoAsync([Body] PublicarSaldoRequestDto request, [Header("Authorization")] string authorization);
}

public record SaldoResponseDto(Guid IdConta, DateOnly Data, decimal Saldo);
public record PublicarSaldoRequestDto(Guid IdConta, DateOnly Data, decimal Saldo);
