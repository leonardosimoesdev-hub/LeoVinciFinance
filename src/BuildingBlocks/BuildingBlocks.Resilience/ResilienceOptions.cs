namespace BuildingBlocks.Resilience;

/// <summary>
/// Parâmetros de resiliência (retry/circuit breaker/timeout) para HttpClients.
/// Migrado para BuildingBlocks.Resilience para evitar dependência circular.
/// </summary>
public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public int TimeoutSegundos { get; set; } = 5;
    public int TentativasRetry { get; set; } = 3;
    public int RetryBackoffBaseMs { get; set; } = 200;
    public int RetryJitterMaxMs { get; set; } = 100;
    public int CircuitBreakerFalhasConsecutivas { get; set; } = 5;
    public int CircuitBreakerDuracaoSegundos { get; set; } = 30;
}
