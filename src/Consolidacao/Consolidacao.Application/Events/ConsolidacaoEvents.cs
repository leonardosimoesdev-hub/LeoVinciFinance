namespace Consolidacao.Application.Events;

/// <summary>
/// Os 3 eventos do fluxo (docs/c4/component.md), modelados como tipos de Application — o
/// formato de fio (serialização Kafka) é decisão de Infrastructure. Como produtor e
/// consumidor destes eventos são o MESMO módulo (Consolidacao), não há necessidade de um
/// contrato compartilhado entre módulos como BuildingBlocks.IntegrationEvents; por isso esse
/// projeto foi removido nesta revisão (Ajustes round 1 + ADR 0011: Financeiro não publica
/// mais nenhum evento).
/// </summary>
public record SaldoDiarioConsolidadoIniciadoEvento(Guid IdJob, Guid IdConta, DateOnly Data, Guid CorrelationId);

public record SaldoDiarioConsolidadoConcluidoEvento(Guid IdJob, Guid IdConta, DateOnly Data, decimal Saldo, Guid CorrelationId, string Mensagem);

public record SaldoDiarioConsolidadoComFalhasEvento(Guid IdJob, Guid IdConta, DateOnly Data, Guid CorrelationId, string Mensagem);

/// <summary>
/// Publicação dos 3 eventos via Kafka (implementado em Infrastructure com MassTransit).
/// Interface explícita (não genérica) porque, ao contrário do que se tentou no round
/// anterior, aqui produtor e consumidor sempre vivem no mesmo assembly — não há problema de
/// "URN de tipo" entre módulos a resolver.
/// </summary>
public interface IConsolidacaoEventPublisher
{
    Task PublicarIniciadoAsync(SaldoDiarioConsolidadoIniciadoEvento evento, CancellationToken cancellationToken);

    Task PublicarConcluidoAsync(SaldoDiarioConsolidadoConcluidoEvento evento, CancellationToken cancellationToken);

    Task PublicarComFalhasAsync(SaldoDiarioConsolidadoComFalhasEvento evento, CancellationToken cancellationToken);
}
