namespace BuildingBlocks.ServiceAuth;

/// <summary>
/// Parâmetros de resiliência (retry/circuit breaker/timeout) para HttpClients que chamam
/// outros serviços internos. Antes hardcoded e duplicados em Relatorios.Infrastructure e
/// Consolidacao.Infrastructure (Ajustes round 1) — agora únicos e configuráveis via a seção
/// "Resilience" do appsettings, sem nenhum "magic number" no código.
/// </summary>
public class ResilienceOptions
{
    public const string SectionName = "Resilience";

    /// <summary>Timeout por tentativa de chamada HTTP.</summary>
    public int TimeoutSegundos { get; set; } = 5;

    /// <summary>Número de tentativas de retry em falhas transitórias (5xx, timeout, erro de rede).</summary>
    public int TentativasRetry { get; set; } = 3;

    /// <summary>Base do backoff exponencial entre tentativas, em milissegundos.</summary>
    public int RetryBackoffBaseMs { get; set; } = 200;

    /// <summary>Jitter aleatório máximo somado ao backoff, em milissegundos.</summary>
    public int RetryJitterMaxMs { get; set; } = 100;

    /// <summary>Quantidade de falhas consecutivas até abrir o circuit breaker.</summary>
    public int CircuitBreakerFalhasConsecutivas { get; set; } = 5;

    /// <summary>Tempo que o circuit breaker permanece aberto antes de ir para half-open.</summary>
    public int CircuitBreakerDuracaoSegundos { get; set; } = 30;
}
