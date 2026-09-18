using System.Net;
using System.Net.Http;
using BuildingBlocks.ServiceAuth;
using Consolidacao.Application.Abstractions;

namespace Consolidacao.Infrastructure.Gateways;

public class FinanceiroGateway : IFinanceiroGateway
{
    private readonly IFinanceiroApiClient _client;
    private readonly IServiceTokenProvider _tokenProvider;

    public FinanceiroGateway(IFinanceiroApiClient client, IServiceTokenProvider tokenProvider)
    {
        _client = client;
        _tokenProvider = tokenProvider;
    }

    public async Task<IReadOnlyList<ContaDto>> ObterTodasContasAsync(CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.ObterTokenAsync(cancellationToken);
        var contas = await _client.ObterTodasContasAsync($"Bearer {token}");
        return contas.Select(c => new ContaDto(c.Id, c.IdCliente)).ToList();
    }

    public async Task<IReadOnlyList<LancamentoDto>> ObterLancamentosPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.ObterTokenAsync(cancellationToken);
        var lancamentos = await _client.ObterLancamentosPorContaEDataAsync(idConta, data.ToString("yyyy-MM-dd"), $"Bearer {token}");
        return lancamentos.Select(l => new LancamentoDto(l.Id, l.IdConta, l.Valor, l.Data)).ToList();
    }
}

public class RelatoriosGateway : IRelatoriosGateway
{
    private readonly IRelatoriosApiClient _client;
    private readonly IServiceTokenProvider _tokenProvider;

    public RelatoriosGateway(IRelatoriosApiClient client, IServiceTokenProvider tokenProvider)
    {
        _client = client;
        _tokenProvider = tokenProvider;
    }

    public async Task<decimal?> ObterSaldoAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.ObterTokenAsync(cancellationToken);
        var response = await _client.ObterSaldoAsync(idConta, data, $"Bearer {token}");

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
            throw new HttpRequestException($"Request failed with status code {response.StatusCode}");

        return response.Content?.Saldo;
    }

    public async Task PublicarSaldoDiarioConsolidadoAsync(Guid idConta, DateOnly data, decimal saldo, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.ObterTokenAsync(cancellationToken);
        await _client.PublicarSaldoDiarioConsolidadoAsync(new PublicarSaldoRequestDto(idConta, data, saldo), $"Bearer {token}");
    }
}
