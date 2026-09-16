using Financeiro.Domain.Entities;

namespace Financeiro.Application.Abstractions;

/// <summary>
/// Repositório de escrita de Lancamento. Sem genérico "CRUD de tudo" — apenas a operação
/// que o caso de uso realmente precisa (seção 15/38 da Especificação Mestre).
/// </summary>
public interface ILancamentoRepository
{
    Task AddAsync(Lancamento lancamento, CancellationToken cancellationToken);

    /// <summary>Usado pela Consolidação (GET /contas/{idConta}/lancamentos/{data}).</summary>
    Task<IReadOnlyList<Lancamento>> ObterPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken);
}

public interface IClienteReadRepository
{
    Task<Cliente?> ObterPorIdUsuarioAsync(long idUsuario, CancellationToken cancellationToken);
}

public interface IContaReadRepository
{
    Task<Conta?> ObterPorIdAsync(Guid idConta, CancellationToken cancellationToken);

    /// <summary>Contas pertencentes ao cliente vinculado ao idUsuario informado.</summary>
    Task<IReadOnlyList<Conta>> ObterPorIdUsuarioAsync(long idUsuario, CancellationToken cancellationToken);

    /// <summary>Todas as contas do sistema — uso interno (job de backfill da Consolidação).</summary>
    Task<IReadOnlyList<Conta>> ObterTodasAsync(CancellationToken cancellationToken);

    /// <summary>Verifica titularidade — usado na autorização de criação de lançamento.</summary>
    Task<bool> UsuarioPossuiContaAsync(long idUsuario, Guid idConta, CancellationToken cancellationToken);
}

