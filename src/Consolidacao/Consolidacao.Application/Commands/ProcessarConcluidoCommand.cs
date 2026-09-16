using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Consolidacao.Application.Commands;

/// <summary>Consumidor do evento "Concluído" (seção 27): registra a etapa/execução final de sucesso no Job.</summary>
public record ProcessarConcluidoCommand(Guid IdJob, string Mensagem) : ICommand<Result<bool>>;

public class ProcessarConcluidoCommandHandler : ICommandHandler<ProcessarConcluidoCommand, Result<bool>>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessarConcluidoCommandHandler> _logger;

    public ProcessarConcluidoCommandHandler(IJobRepository jobRepository, IUnitOfWork unitOfWork, ILogger<ProcessarConcluidoCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ProcessarConcluidoCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.ObterPorIdAsync(command.IdJob, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {IdJob} não encontrado ao processar evento Concluído; ignorando.", command.IdJob);
            return Result<bool>.Success(false);
        }

        if (job.FoiConcluido())
        {
            // Idempotência do consumidor (garantia "pelo menos uma vez" do Kafka) — reprocessar
            // o mesmo evento não deve gerar uma segunda etapa/execução de conclusão.
            _logger.LogInformation("Job {IdJob} já estava concluído; evento duplicado ignorado.", command.IdJob);
            return Result<bool>.Success(false);
        }

        job.RegistrarConclusao(command.Mensagem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Job {IdJob} concluído: {Mensagem}", command.IdJob, command.Mensagem);

        return Result<bool>.Success(true);
    }
}
