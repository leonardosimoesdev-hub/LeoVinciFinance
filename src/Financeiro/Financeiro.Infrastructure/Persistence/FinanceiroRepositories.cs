using Financeiro.Application.Abstractions;
using Financeiro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Financeiro.Infrastructure.Persistence;

public class LancamentoRepository : ILancamentoRepository
{
    private readonly FinanceiroDbContext _dbContext;

    public LancamentoRepository(FinanceiroDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(Lancamento lancamento, CancellationToken cancellationToken) =>
        await _dbContext.Lancamentos.AddAsync(lancamento, cancellationToken);

    public Task<IReadOnlyList<Lancamento>> ObterPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.Lancamentos.AsNoTracking()
            .Where(l => l.IdConta == idConta && l.Data == data)
            .OrderBy(l => l.CriadoEm)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Lancamento>)t.Result, cancellationToken);
}

public class ClienteReadRepository : IClienteReadRepository
{
    private readonly FinanceiroDbContext _dbContext;

    public ClienteReadRepository(FinanceiroDbContext dbContext) => _dbContext = dbContext;

    public Task<Cliente?> ObterPorIdUsuarioAsync(long idUsuario, CancellationToken cancellationToken) =>
        _dbContext.Clientes.AsNoTracking()
            .Include(c => c.Contas)
            .FirstOrDefaultAsync(c => c.IdUsuario == idUsuario, cancellationToken);
}

public class ContaReadRepository : IContaReadRepository
{
    private readonly FinanceiroDbContext _dbContext;

    public ContaReadRepository(FinanceiroDbContext dbContext) => _dbContext = dbContext;

    public Task<Conta?> ObterPorIdAsync(Guid idConta, CancellationToken cancellationToken) =>
        _dbContext.Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == idConta, cancellationToken);

    public Task<IReadOnlyList<Conta>> ObterPorIdUsuarioAsync(long idUsuario, CancellationToken cancellationToken) =>
        (from conta in _dbContext.Contas.AsNoTracking()
         join cliente in _dbContext.Clientes.AsNoTracking() on conta.IdCliente equals cliente.Id
         where cliente.IdUsuario == idUsuario
         select conta)
        .ToListAsync(cancellationToken)
        .ContinueWith(t => (IReadOnlyList<Conta>)t.Result, cancellationToken);

    public Task<IReadOnlyList<Conta>> ObterTodasAsync(CancellationToken cancellationToken) =>
        _dbContext.Contas.AsNoTracking()
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Conta>)t.Result, cancellationToken);

    public async Task<bool> UsuarioPossuiContaAsync(long idUsuario, Guid idConta, CancellationToken cancellationToken)
    {
        return await (from conta in _dbContext.Contas.AsNoTracking()
                       join cliente in _dbContext.Clientes.AsNoTracking() on conta.IdCliente equals cliente.Id
                       where cliente.IdUsuario == idUsuario && conta.Id == idConta
                       select conta.Id)
            .AnyAsync(cancellationToken);
    }
}

public class UnitOfWork : BuildingBlocks.Common.Abstractions.IUnitOfWork
{
    private readonly FinanceiroDbContext _dbContext;

    public UnitOfWork(FinanceiroDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
