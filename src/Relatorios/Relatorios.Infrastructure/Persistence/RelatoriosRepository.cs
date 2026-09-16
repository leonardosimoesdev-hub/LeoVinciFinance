using Microsoft.EntityFrameworkCore;
using Relatorios.Application.Abstractions;
using Relatorios.Domain.Entities;

namespace Relatorios.Infrastructure.Persistence;

public class SaldoDiarioConsolidadoRepository : ISaldoDiarioConsolidadoRepository
{
    private readonly RelatoriosDbContext _dbContext;

    public SaldoDiarioConsolidadoRepository(RelatoriosDbContext dbContext) => _dbContext = dbContext;

    public Task<SaldoDiarioConsolidado?> ObterPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.SaldosDiariosConsolidados.AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdConta == idConta && s.Data == data, cancellationToken);

    public async Task AddAsync(SaldoDiarioConsolidado saldo, CancellationToken cancellationToken) =>
        await _dbContext.SaldosDiariosConsolidados.AddAsync(saldo, cancellationToken);
}

public class UnitOfWork : BuildingBlocks.Common.Abstractions.IUnitOfWork
{
    private readonly RelatoriosDbContext _dbContext;

    public UnitOfWork(RelatoriosDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
