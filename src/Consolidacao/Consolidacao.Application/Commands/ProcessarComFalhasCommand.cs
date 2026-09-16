using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Consolidacao.Application.Commands;

/// <summary>
/// Consumidor do evento "ComFalhas" (seção 25/29): registra a falha no Job e, se o limite de
/// tentativas foi excedido, emite um alerta operacional (log crítico — não há canal de
/// notificação externo definido na Especificação Mestre; ver README).
/// </summary>
public record ProcessarComFalhasCommand(Guid IdJob, string Mensagem) : ICommand<Result<bool>>;

public class ProcessarComFalhasCommandHandler : ICommandHandler<ProcessarComFalhasCommand, Result<bool>>
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessarComFalhasCommandHandler> _logger;

    public ProcessarComFalhasCommandHandler(IJobRepository jobRepository, IUnitOfWork unitOfWork, ILogger<ProcessarComFalhasCommandHandler> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ProcessarComFalhasCommand command, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.ObterPorIdAsync(command.IdJob, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {IdJob} não encontrado ao processar evento ComFalhas; ignorando.", command.IdJob);
            return Result<bool>.Success(false);
        }

        if (job.FoiConcluido())
        {
            _logger.LogInformation("Job {IdJob} já havia concluído antes de registrar esta falha (corrida com retry); ignorando.", command.IdJob);
            return Result<bool>.Success(false);
        }

        job.RegistrarFalha(command.Mensagem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (job.ExcedeuLimiteTentativas())
        {
            // Alerta operacional definitivo: nenhum canal externo de notificação foi definido
            // na Especificação Mestre — registrado como log crítico estruturado, para ser
            // capturado por qualquer ferramenta de observabilidade ligada ao Aspire Dashboard.
            _logger.LogCritical(
                "ALERTA OPERACIONAL: Job {IdJob} (IdConta={IdConta}, Data={Data}) excedeu o limite de {LimiteTentativas} tentativas e não será mais reprocessado automaticamente.",
                job.Id, job.IdConta, job.Data, job.LimiteTentativas);
        }
        else
        {
            _logger.LogWarning(
                "Job {IdJob} falhou ({QuantidadeFalhas}/{LimiteTentativas} tentativas): {Mensagem}",
                job.Id, job.QuantidadeFalhas(), job.LimiteTentativas, command.Mensagem);
        }

        return Result<bool>.Success(true);
    }
}
