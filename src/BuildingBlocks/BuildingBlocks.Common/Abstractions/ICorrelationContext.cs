namespace BuildingBlocks.Common.Abstractions;

/// <summary>
/// Abstração para propagação do IdCorrelationId de ponta a ponta (HTTP -> Kafka -> HTTP),
/// conforme Especificação Mestre seção 10 e 18. Implementada em Infrastructure de cada
/// módulo (middleware HTTP / filtro do MassTransit); Application apenas consome.
/// </summary>
public interface ICorrelationContextAccessor
{
    Guid CorrelationId { get; }
}

/// <summary>
/// Abstração de publicação de eventos de integração (implementada via MassTransit/Kafka
/// em Infrastructure). Application/Domain nunca referenciam MassTransit diretamente.
/// </summary>
public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class;
}
