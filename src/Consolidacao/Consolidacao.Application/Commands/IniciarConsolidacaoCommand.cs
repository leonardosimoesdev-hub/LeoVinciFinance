using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Consolidacao.Application.Events;
using Consolidacao.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Consolidacao.Application.Commands;

/// <summary>
/// Cria (se ainda não existir) o Job de consolidação para (IdConta, Data) e publica o evento
/// "Iniciado". Chamado pelos hosted services agendador, de gaps e de retry (seções 25/28/29/30)
/// — todos convergem para este único ponto de entrada, o que garante a mesma checagem de
/// idempotência (seção 28: "não criar duplicidade") em qualquer origem da chamada.
/// </summary>
public record IniciarConsolidacaoCommand(Guid IdConta, DateOnly Data) : ICommand<Result<bool>>;

public class IniciarConsolidacaoCommandHandler : ICommandHandler<IniciarConsolidacaoCommand, Result<bool>>
{
    private readonly IConsolidacaoEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConsolidacaoEventPublisher _eventPublisher;
    private readonly ConsolidacaoOptions _options;
    private readonly ILogger<IniciarConsolidacaoCommandHandler> _logger;

    public IniciarConsolidacaoCommandHandler(
        IConsolidacaoEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IConsolidacaoEventPublisher eventPublisher,
        IOptions<ConsolidacaoOptions> options,
        ILogger<IniciarConsolidacaoCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(IniciarConsolidacaoCommand command, CancellationToken cancellationToken)
    {
        var existente = await _eventRepository.ObterIniciadoPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);

        if (existente is not null)
        {
            // Idempotência (seção 28): já existe um Job para esta conta/data — não cria outro.
            // Se ele já foi concluído, não há nada a fazer; se está pendente/com falha, quem
            // decide reprocessar é o SaldoDiarioConsolidadoComFalhasHostedService, não este comando.
            _logger.LogInformation(
                "Job de consolidação já existe para IdConta={IdConta} Data={Data} (IdJob={IdJob}); nada a fazer.",
                command.IdConta, command.Data, existente.Id);

            return Result<bool>.Success(false);
        }

        var correlationId = Guid.NewGuid();
        var iniciado = Consolidacao.Domain.Entities.SaldoDiarioConsolidadoIniciadoEventoEntity.Create(command.IdConta, command.Data, correlationId);

        await _eventRepository.AddIniciadoAsync(iniciado, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublicarIniciadoAsync(
            new Consolidacao.Application.Events.SaldoDiarioConsolidadoIniciadoEvento(Guid.Empty, command.IdConta, command.Data, correlationId),
            cancellationToken);

        _logger.LogInformation(
            "Evento Iniciado criado: IdConta={IdConta} Data={Data} CorrelationId={CorrelationId}.",
            command.IdConta, command.Data, correlationId);

        return Result<bool>.Success(true);
    }
}
