using Consolidacao.Application.Abstractions;
using Consolidacao.Domain.Entities;
using Consolidacao.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Consolidacao.Infrastructure.Persistence;

public class JobRepository : IJobRepository
{
    private readonly ConsolidacaoDbContext _dbContext;

    public JobRepository(ConsolidacaoDbContext dbContext) => _dbContext = dbContext;

    public Task<SaldoDiarioConsolidadoJob?> ObterPorIdAsync(Guid idJob, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoJobs
            .Include(j => j.Etapas)
            .Include(j => j.Execucoes)
            .FirstOrDefaultAsync(j => j.Id == idJob, cancellationToken);

    public Task<SaldoDiarioConsolidadoJob?> ObterPorContaEDataAsync(Guid idConta, DateOnly data, CancellationToken cancellationToken) =>
        _dbContext.SaldoDiarioConsolidadoJobs
            .Include(j => j.Etapas)
            .Include(j => j.Execucoes)
            .FirstOrDefaultAsync(j => j.IdConta == idConta && j.Data == data, cancellationToken);

    public async Task<IReadOnlyList<SaldoDiarioConsolidadoJob>> ObterElegiveisParaRetryAsync(CancellationToken cancellationToken)
    {
        // Um Job é elegível para retry quando: não foi concluído, teve ao menos uma falha, e a
        // quantidade de falhas ainda não atingiu o próprio limite de tentativas configurado.
        // Filtragem por "não concluído"/"teve falha" é feita em SQL (via EXISTS); o corte por
        // limite de tentativas é aplicado em memória por depender de duas colunas agregadas
        // (contagem de execuções com erro vs. LimiteTentativas), evitando uma query
        // excessivamente complexa aqui.
        var candidatos = await _dbContext.SaldoDiarioConsolidadoJobs
            .Include(j => j.Etapas)
            .Include(j => j.Execucoes)
            .Where(j => j.Execucoes.Any(e => e.Status == StatusExecucao.Erro))
            .Where(j => !j.Execucoes.Any(e => e.Evento == EventoTipo.SaldoDiarioConsolidadoConcluido && e.Status == StatusExecucao.Sucesso))
            .ToListAsync(cancellationToken);

        return candidatos.Where(j => !j.ExcedeuLimiteTentativas()).ToList();
    }

    public async Task<IReadOnlyList<DateOnly>> ObterDatasConcluidasAsync(Guid idConta, CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.SaldoDiarioConsolidadoJobs
            .Include(j => j.Execucoes)
            .Where(j => j.IdConta == idConta)
            .ToListAsync(cancellationToken);

        return jobs.Where(j => j.FoiConcluido()).Select(j => j.Data).Distinct().ToList();
    }

    public async Task AddAsync(SaldoDiarioConsolidadoJob job, CancellationToken cancellationToken) =>
        await _dbContext.SaldoDiarioConsolidadoJobs.AddAsync(job, cancellationToken);
}

public class UnitOfWork : BuildingBlocks.Common.Abstractions.IUnitOfWork
{
    private readonly ConsolidacaoDbContext _dbContext;

    public UnitOfWork(ConsolidacaoDbContext dbContext) => _dbContext = dbContext;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => _dbContext.SaveChangesAsync(cancellationToken);
}
