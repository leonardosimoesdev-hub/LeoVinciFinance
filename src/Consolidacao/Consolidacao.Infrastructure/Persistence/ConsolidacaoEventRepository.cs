using Consolidacao.Application.Abstractions;
using Consolidacao.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Consolidacao.Infrastructure.Persistence;

public class ConsolidacaoEventRepository : IConsolidacaoEventRepository
{
    private readonly ConsolidacaoDbContext _dbContext;

    public ConsolidacaoEventRepository(ConsolidacaoDbContext dbContext) => _dbContext = dbContext;

    public Task AddIniciadoAsync(Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity evento, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoIniciadoEventos.AddAsync(evento, cancellationToken).AsTask();

    public Task AddConcluidoAsync(Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity evento, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoConcluidoEventos.AddAsync(evento, cancellationToken).AsTask();

    public Task AddComFalhasAsync(Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity evento, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoComFalhasEventos.AddAsync(evento, cancellationToken).AsTask();

    public Task<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity?> ObterIniciadoPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoIniciadoEventos.FirstOrDefaultAsync(e => e.IdConta == idConta && e.Data == data, cancellationToken);

    public Task<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity?> ObterConcluidoPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoConcluidoEventos.FirstOrDefaultAsync(e => e.IdConta == idConta && e.Data == data, cancellationToken);

    public Task<IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>> ObterComFalhasPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoComFalhasEventos.Where(e => e.IdConta == idConta && e.Data == data).ToListAsync(cancellationToken).ContinueWith(t => (IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>)t.Result, cancellationToken);

    public Task<IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>> ObterComFalhasElegiveisParaRetryAsync(int limiteTentativas, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoComFalhasEventos
            .Where(e => e.Tentativas < limiteTentativas)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity>)t.Result, cancellationToken);

    public async Task<IReadOnlyList<DateOnly>> ObterDatasConcluidasAsync(Guid idConta, CancellationToken cancellationToken)
    {
        var concluido = await _dbContext.SaldoDiarioConsolidadoConcluidoEventos
            .Where(e => e.IdConta == idConta)
            .Select(e => e.Data)
            .Distinct()
            .ToListAsync(cancellationToken);

        return concluido;
    }
}
