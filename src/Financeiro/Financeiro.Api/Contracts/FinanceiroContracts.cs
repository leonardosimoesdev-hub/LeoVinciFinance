namespace Financeiro.Api.Contracts;

public record CriarLancamentoRequest(Guid IdConta, decimal Valor);

public record LancamentoResponse(Guid Id, Guid IdConta, decimal Valor, DateOnly Data);

public record ContaResponse(Guid Id, Guid IdCliente);
