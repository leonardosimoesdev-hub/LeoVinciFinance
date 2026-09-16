using System.ComponentModel.DataAnnotations;

namespace Financeiro.Api.Contracts;

public record CriarLancamentoRequest(
    [property: Required] Guid IdConta,
    [property: Required] decimal Valor);

public record LancamentoResponse(Guid Id, Guid IdConta, decimal Valor, DateOnly Data);

public record ContaResponse(Guid Id, Guid IdCliente);
