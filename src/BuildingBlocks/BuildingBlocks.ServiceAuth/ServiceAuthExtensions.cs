using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace BuildingBlocks.ServiceAuth;

public static class ServiceAuthExtensions
{
    /// <summary>
    /// Registra IServiceTokenProvider + o cliente Refit de Auth.Api (login/validação da conta
    /// de serviço) + IMemoryCache. Deve ser chamado uma vez por módulo consumidor
    /// (Relatorios.Infrastructure, Consolidacao.Infrastructure).
    /// </summary>
    public static IServiceCollection AddServiceAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<ServiceAccountOptions>(configuration.GetSection(ServiceAccountOptions.SectionName));
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));

        var authBaseUrl = configuration["Auth:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'Auth:BaseUrl' ausente.");

        var resilience = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();

        services.AddRefitClient<IAuthApiClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(authBaseUrl))
            .AddPolicyHandler(ResiliencePolicies.GetRetryPolicy(resilience))
            .AddPolicyHandler(ResiliencePolicies.GetTimeoutPolicy(resilience));

        services.AddSingleton<IServiceTokenProvider, ServiceTokenProvider>();

        return services;
    }

    /// <summary>
    /// Registra um cliente Refit resiliente genérico (retry + circuit breaker + timeout, todos
    /// parametrizados por <see cref="ResilienceOptions"/>) apontando para <paramref name="baseUrl"/>.
    /// Usado para os clientes de Financeiro.Api e Relatorios.Api a partir de outros módulos.
    /// </summary>
    public static IServiceCollection AddResilientRefitClient<TClient>(this IServiceCollection services, IConfiguration configuration, string baseUrl)
        where TClient : class
    {
        var resilience = configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>() ?? new ResilienceOptions();

        services.AddRefitClient<TClient>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
            .AddPolicyHandler(ResiliencePolicies.GetRetryPolicy(resilience))
            .AddPolicyHandler(ResiliencePolicies.GetCircuitBreakerPolicy(resilience))
            .AddPolicyHandler(ResiliencePolicies.GetTimeoutPolicy(resilience));

        return services;
    }
}
