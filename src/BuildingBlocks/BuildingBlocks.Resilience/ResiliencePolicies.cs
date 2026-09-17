using System.Net.Http;
using Polly;
using Polly.Extensions.Http;


namespace BuildingBlocks.Resilience;

/// <summary>
/// Fábrica única de políticas Polly.
/// </summary>
public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilienceOptions options) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(options.TentativasRetry, attempt =>
                TimeSpan.FromMilliseconds(options.RetryBackoffBaseMs * Math.Pow(2, attempt))
                + TimeSpan.FromMilliseconds(Random.Shared.Next(0, options.RetryJitterMaxMs)));

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilienceOptions options) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: options.CircuitBreakerFalhasConsecutivas,
                durationOfBreak: TimeSpan.FromSeconds(options.CircuitBreakerDuracaoSegundos));

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ResilienceOptions options) =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(options.TimeoutSegundos));
}
