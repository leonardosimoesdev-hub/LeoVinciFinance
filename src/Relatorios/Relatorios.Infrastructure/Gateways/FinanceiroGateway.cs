using BuildingBlocks.ServiceAuth;
using Microsoft.Extensions.Logging;
using Relatorios.Application.Abstractions;

namespace Relatorios.Infrastructure.Gateways;

/// <summary>
/// Implementação concreta de IFinanceiroGateway. A resiliência (timeout, retry, circuit
/// breaker) é configurada no HttpClient nomeado via Polly em RelatoriosModuleExtensions,
/// não aqui — este gateway apenas orquestra a chamada.
/// </summary>
public class FinanceiroGateway : IFinanceiroGateway
{
    private readonly IFinanceiroApiClient _client;
    private readonly IServiceTokenProvider _tokenProvider;
    private readonly ILogger<FinanceiroGateway> _logger;

    public FinanceiroGateway(IFinanceiroApiClient client, IServiceTokenProvider tokenProvider, ILogger<FinanceiroGateway> logger)
    {
        _client = client;
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    public async Task<bool> UsuarioPossuiContaAsync(long idUsuario, Guid idConta, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _tokenProvider.ObterTokenAsync(cancellationToken);
            var contas = await _client.ObterContasPorUsuarioAsync(idUsuario, $"Bearer {token}");

            return contas.Any(c => c.Id == idConta);
        }
        catch (Exception ex)
        {
            // Operação de leitura, idempotente por natureza: uma falha aqui não deve derrubar
            // a consulta de saldo com 500 genérico — é tratada como "sem titularidade
            // confirmada" e a Api decide o retorno (geralmente 403), registrando o motivo.
            // Ver docs/adr/0006-resiliencia-polly.md.
            _logger.LogError(ex, "Falha ao verificar titularidade da conta {IdConta} para o usuário {IdUsuario} em Financeiro.Api.", idConta, idUsuario);
            return false;
        }
    }
}
