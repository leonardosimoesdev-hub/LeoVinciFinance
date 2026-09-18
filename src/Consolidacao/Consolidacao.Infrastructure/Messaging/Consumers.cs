using BuildingBlocks.Common.Application;
using Consolidacao.Application.Commands;
using Consolidacao.Application.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Consolidacao.Infrastructure.Messaging;

/// <summary>
/// Consumidor do evento "Iniciado" — dispara o trabalho de fato (buscar lançamentos, somar,
/// publicar em Relatorios). "Pelo menos uma vez": uma exceção não tratada aqui faz o
/// MassTransit não avançar o offset, reprocessando a mensagem — por isso o handler de
/// Application (ProcessarIniciadoCommandHandler) é idempotente ponta a ponta.
/// </summary>
public class SaldoDiarioConsolidadoIniciadoConsumer : IConsumer<SaldoDiarioConsolidadoIniciadoEvento>
{
    private readonly ICommandHandler<ProcessarIniciadoCommand, Result<bool>> _handler;
    private readonly ILogger<SaldoDiarioConsolidadoIniciadoConsumer> _logger;

    public SaldoDiarioConsolidadoIniciadoConsumer(ICommandHandler<ProcessarIniciadoCommand, Result<bool>> handler, ILogger<SaldoDiarioConsolidadoIniciadoConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaldoDiarioConsolidadoIniciadoEvento> context)
    {
        var evento = context.Message;

        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = evento.CorrelationId, ["IdJob"] = evento.IdJob });
        _logger.LogInformation("Evento Iniciado recebido para IdConta={IdConta} Data={Data}.", evento.IdConta, evento.Data);

        await _handler.HandleAsync(new ProcessarIniciadoCommand(evento.IdJob, evento.IdConta, evento.Data, evento.CorrelationId), context.CancellationToken);
    }
}

/// <summary>Consumidor do evento "Concluído" — registra a etapa/execução final de sucesso no Job.</summary>
public class SaldoDiarioConsolidadoConcluidoConsumer : IConsumer<SaldoDiarioConsolidadoConcluidoEvento>
{
    private readonly ICommandHandler<ProcessarConcluidoCommand, Result<bool>> _handler;
    private readonly ILogger<SaldoDiarioConsolidadoConcluidoConsumer> _logger;

    public SaldoDiarioConsolidadoConcluidoConsumer(ICommandHandler<ProcessarConcluidoCommand, Result<bool>> handler, ILogger<SaldoDiarioConsolidadoConcluidoConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaldoDiarioConsolidadoConcluidoEvento> context)
    {
        var evento = context.Message;

        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = evento.CorrelationId, ["IdJob"] = evento.IdJob });
        _logger.LogInformation("Evento Concluído recebido para IdConta={IdConta} Data={Data} Saldo={Saldo}.", evento.IdConta, evento.Data, evento.Saldo);

        await _handler.HandleAsync(new ProcessarConcluidoCommand(evento.IdConta, evento.Data, evento.Saldo, evento.CorrelationId, evento.Mensagem), context.CancellationToken);
    }
}

/// <summary>Consumidor do evento "ComFalhas" — registra a falha no Job e avalia o limite de tentativas.</summary>
public class SaldoDiarioConsolidadoComFalhasConsumer : IConsumer<SaldoDiarioConsolidadoComFalhasEvento>
{
    private readonly ICommandHandler<ProcessarComFalhasCommand, Result<bool>> _handler;
    private readonly ILogger<SaldoDiarioConsolidadoComFalhasConsumer> _logger;

    public SaldoDiarioConsolidadoComFalhasConsumer(ICommandHandler<ProcessarComFalhasCommand, Result<bool>> handler, ILogger<SaldoDiarioConsolidadoComFalhasConsumer> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaldoDiarioConsolidadoComFalhasEvento> context)
    {
        var evento = context.Message;

        using var _ = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = evento.CorrelationId, ["IdJob"] = evento.IdJob });
        _logger.LogWarning("Evento ComFalhas recebido para IdConta={IdConta} Data={Data}: {Mensagem}", evento.IdConta, evento.Data, evento.Mensagem);

        await _handler.HandleAsync(new ProcessarComFalhasCommand(evento.IdConta, evento.Data, evento.CorrelationId, evento.Mensagem), context.CancellationToken);
    }
}
