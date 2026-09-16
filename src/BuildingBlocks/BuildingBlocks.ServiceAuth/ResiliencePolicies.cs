using Polly;
using Polly.Extensions.Http;

namespace BuildingBlocks.ServiceAuth;

/// <summary>
/// Fábrica única de políticas Polly (antes duplicada em Relatorios.Infrastructure e
/// Consolidacao.Infrastructure — Ajustes round 1). Todos os parâmetros vêm de
/// <see cref="ResilienceOptions"/> (bind do appsettings), nenhum valor fica hardcoded aqui.
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>Retry com backoff exponencial + jitter, apenas em falhas transitórias (5xx/timeout/rede).</summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilienceOptions options) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(options.TentativasRetry, attempt =>
                TimeSpan.FromMilliseconds(options.RetryBackoffBaseMs * Math.Pow(2, attempt))
                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, options.RetryJitterMaxMs)));

    /// <summary>Circuit breaker: abre após N falhas consecutivas, half-open após o tempo configurado.</summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilienceOptions options) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerFalhasConsecutivas,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDuracaoSegundos));

    /// <summary>Timeout por tentativa — evita threads presas aguardando um serviço indisponível.</summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ResilienceOptions options) =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSegundos));
}
