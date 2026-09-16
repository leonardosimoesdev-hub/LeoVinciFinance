using Relatorios.Domain.Entities;

namespace Relatorios.Application.Abstractions;

public interface ISaldoDiarioConsolidadoRepository
{
    Task<SaldoDiarioConsolidado?> ObterPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);
    Task AddAsync(SaldoDiarioConsolidado saldo, CancellationToken cancellationToken);
}


/// <summary>
/// Verificação de titularidade de conta. Relatórios não tem o schema de Financeiro, então
/// esta verificação é feita via chamada síncrona HTTP (Refit) ao Financeiro.Api — a
/// interface fica em Application (abstração), a implementação concreta com Refit/Polly
/// fica em Infrastructure.
/// </summary>
public interface IFinanceiroGateway
{
    Task<bool> UsuarioPossuiContaAsync(long idUsuario, Guid idConta, CancellationToken cancellationToken);
}
