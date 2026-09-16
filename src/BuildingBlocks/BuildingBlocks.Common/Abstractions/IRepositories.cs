namespace BuildingBlocks.Common.Abstractions;

/// <summary>
/// Abstração de escrita. Cada módulo declara suas próprias interfaces específicas
/// (ex.: ILancamentoWriteRepository) herdando/compondo estas — evitamos repository
/// genérico "CRUD de tudo" que esconderia funcionalidades do EF Core sem necessidade
/// (Especificação Mestre, seção 15 e 38).
/// </summary>
public interface IWriteRepository<TEntity, in TId>
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken);
}

/// <summary>
/// Abstração de leitura pura (sem tracking) para consultas.
/// </summary>
public interface IReadRepository<TEntity, in TId>
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken);
}

/// <summary>
/// Unit of Work do módulo — persiste as alterações feitas via repositórios de escrita
/// em uma única transação lógica.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
