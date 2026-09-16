using System.ComponentModel.DataAnnotations;

namespace Relatorios.Api.Contracts;

public record CriarSaldoDiarioConsolidadoRequest(
    [property: Required] Guid IdConta,
    [property: Required] DateOnly Data,
    [property: Required] decimal Saldo);

public record SaldoDiarioConsolidadoResponse(Guid IdConta, DateOnly Data, decimal Saldo);
