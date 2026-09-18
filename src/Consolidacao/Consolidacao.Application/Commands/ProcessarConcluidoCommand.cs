using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Consolidacao.Application.Commands;

/// <summary>Consumidor do evento "Concluído" (seção 27): registra a etapa/execução final de sucesso no Job.</summary>
public record ProcessarConcluidoCommand(Guid IdConta, DateOnly Data, decimal Saldo, Guid CorrelationId, string Mensagem) : ICommand<Result<bool>>;

public class ProcessarConcluidoCommandHandler : ICommandHandler<ProcessarConcluidoCommand, Result<bool>>
{
    private readonly IConsolidacaoEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessarConcluidoCommandHandler> _logger;

    public ProcessarConcluidoCommandHandler(IConsolidacaoEventRepository eventRepository, IUnitOfWork unitOfWork, ILogger<ProcessarConcluidoCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ProcessarConcluidoCommand command, CancellationToken cancellationToken)
    {
        // Idempotência: se já existe um evento Concluido para a conta/data, ignora
        var existente = await _eventRepository.ObterConcluidoPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);
        if (existente is not null)
        {
            _logger.LogInformation("Evento Concluído já existe para IdConta={IdConta} Data={Data}; ignorando.", command.IdConta, command.Data);
            return Result<bool>.Success(false);
        }

        var novo = Consolidacao.Domain.Entities.SaldoDiarioConsolidadoConcluidoEventoEntity.Create(command.IdConta, command.Data, command.Saldo, command.CorrelationId, command.Mensagem);
        await _eventRepository.AddConcluidoAsync(novo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Consolidação concluída para IdConta={IdConta} Data={Data}: {Mensagem}", command.IdConta, command.Data, command.Mensagem);

        return Result<bool>.Success(true);
    }
}
