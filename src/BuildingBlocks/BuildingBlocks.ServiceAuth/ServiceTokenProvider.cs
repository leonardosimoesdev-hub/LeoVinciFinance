using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.ServiceAuth;

public interface IServiceTokenProvider
{
    /// <summary>Retorna um token JWT válido para a conta de serviço, para uso no header Authorization.</summary>
    Task<string> ObterTokenAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Implementação única (antes duplicada em Relatorios.Infrastructure e
/// Consolidacao.Infrastructure — Ajustes round 1) de obtenção de token da conta de serviço.
///
/// Estratégia (Ajustes round 1: "ao invés de invocar Login a cada chamada, já armazenar um
/// token válido no appsettings que não expira e verificar se é válido pelo endpoint
/// ValidarToken em Auth"):
///   1. Se há um token em cache (em memória) ainda dentro da janela de validade local, retorna-o
///      direto — nem chama Auth.Api.
///   2. Senão, pega o token configurado em `ServiceAccount:Token` (ver README) e VALIDA via
///      `POST /api/auth/token` — não faz login.
///   3. Só realiza `POST /api/auth/login` (com `ServiceAccount:Username`/`Senha`) se o token
///      configurado estiver ausente, expirado ou inválido — é o caminho de exceção/fallback,
///      não o caminho principal.
/// </summary>
public class ServiceTokenProvider : IServiceTokenProvider
{
    private const string CacheKey = "service-account-token";

    private readonly IAuthApiClient _authApiClient;
    private readonly IMemoryCache _cache;
    private readonly ServiceAccountOptions _options;
    private readonly ILogger<ServiceTokenProvider> _logger;

    public ServiceTokenProvider(
        IAuthApiClient authApiClient,
        IMemoryCache cache,
        IOptions<ServiceAccountOptions> options,
        ILogger<ServiceTokenProvider> logger)
    {
        _authApiClient = authApiClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ObterTokenAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue<string>(CacheKey, out var cached) && cached is not null)
            return cached;

        if (!string.IsNullOrWhiteSpace(_options.Token))
        {
            var validacao = await _authApiClient.ValidarTokenAsync(new ValidarTokenRequestDto(_options.Token));
            if (validacao.Valido)
            {
                _cache.Set(CacheKey, _options.Token, TimeSpan.FromMinutes(_options.CacheMinutos));
                return _options.Token;
            }

            _logger.LogWarning(
                "Token de conta de serviço configurado em ServiceAccount:Token está inválido/expirado; " +
                "efetuando login de fallback com ServiceAccount:Username/Senha.");
        }

        var login = await _authApiClient.LoginAsync(new LoginRequestDto(_options.Username, _options.Senha));

        _logger.LogWarning(
            "Novo token emitido via login de fallback para {Username}. Considere atualizar " +
            "'ServiceAccount:Token' na configuração para evitar esse fallback nas próximas execuções.",
            _options.Username);

        _cache.Set(CacheKey, login.Token, TimeSpan.FromMinutes(_options.CacheMinutos));

        return login.Token;
    }
}
