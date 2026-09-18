using BuildingBlocks.Common.Abstractions;
using BuildingBlocks.Common.Application;
using Consolidacao.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Consolidacao.Domain.Entities;

namespace Consolidacao.Application.Commands;

/// <summary>
/// Consumidor do evento "ComFalhas" (seção 25/29): registra a falha no Job e, se o limite de
/// tentativas foi excedido, emite um alerta operacional (log crítico — não há canal de
/// notificação externo definido na Especificação Mestre; ver README).
/// </summary>
public record ProcessarComFalhasCommand(Guid IdConta, DateOnly Data, Guid CorrelationId, string Mensagem) : ICommand<Result<bool>>;

public class ProcessarComFalhasCommandHandler : ICommandHandler<ProcessarComFalhasCommand, Result<bool>>
{
    private readonly IConsolidacaoEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessarComFalhasCommandHandler> _logger;
    private readonly ConsolidacaoOptions _options;

    public ProcessarComFalhasCommandHandler(IConsolidacaoEventRepository eventRepository, IUnitOfWork unitOfWork, IOptions<ConsolidacaoOptions> options, ILogger<ProcessarComFalhasCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<bool>> HandleAsync(ProcessarComFalhasCommand command, CancellationToken cancellationToken)
    {
        // Se já existe um evento Concluido para esta conta/data, ignora (já processado com sucesso)
        var concluido = await _eventRepository.ObterConcluidoPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);
        if (concluido is not null)
        {
            _logger.LogInformation("Evento Concluído já existe para IdConta={IdConta} Data={Data}; ignorando.", command.IdConta, command.Data);
            return Result<bool>.Success(false);
        }

        var falhasExistentes = await _eventRepository.ObterComFalhasPorContaEDataAsync(command.IdConta, command.Data, cancellationToken);
        var tentativas = (falhasExistentes?.Count ?? 0) + 1;

        var novo = Consolidacao.Domain.Entities.SaldoDiarioConsolidadoComFalhasEventoEntity.Create(command.IdConta, command.Data, command.CorrelationId, tentativas, command.Mensagem);
        await _eventRepository.AddComFalhasAsync(novo, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (tentativas >= _options.LimiteTentativas)
        {
            _logger.LogCritical(
                "ALERTA OPERACIONAL: IdConta={IdConta} Data={Data} excedeu o limite de {LimiteTentativas} tentativas e não será mais reprocessado automaticamente.",
                command.IdConta, command.Data, _options.LimiteTentativas);
        }
        else
        {
            _logger.LogWarning("Falha registrada para IdConta={IdConta} Data={Data} ({Tentativas}/{Limite}): {Mensagem}", command.IdConta, command.Data, tentativas, _options.LimiteTentativas, command.Mensagem);
        }

        return Result<bool>.Success(true);
    }
}
