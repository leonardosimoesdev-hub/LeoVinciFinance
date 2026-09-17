namespace Relatorios.Api.Contracts;

public record CriarSaldoDiarioConsolidadoRequest(
    Guid IdConta,
    DateOnly Data,
    decimal Saldo);

public record SaldoDiarioConsolidadoResponse(Guid IdConta, DateOnly Data, decimal Saldo);
