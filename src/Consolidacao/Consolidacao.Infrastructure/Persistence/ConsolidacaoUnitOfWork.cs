namespace Consolidacao.Infrastructure.Persistence;

public class UnitOfWork : BuildingBlocks.Common.Abstractions.IUnitOfWork
{
    private readonly ConsolidacaoDbContext _dbContext;

    public UnitOfWork(ConsolidacaoDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
