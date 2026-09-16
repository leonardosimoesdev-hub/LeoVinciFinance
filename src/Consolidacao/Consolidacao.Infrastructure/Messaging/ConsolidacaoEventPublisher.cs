using Consolidacao.Application.Events;
using MassTransit;

namespace Consolidacao.Infrastructure.Messaging;

/// <summary>
/// Publica os 3 eventos do fluxo via MassTransit (rider Kafka). Chave de partição =
/// "IdConta|Data" (Ajustes round 1: "chave de partição deve ser IdConta+Data") — garante que
/// todos os eventos de uma mesma consolidação (conta+dia) caiam na mesma partição e sejam
/// processados em ordem, mesmo com múltiplos consumidores.
/// </summary>
public class ConsolidacaoEventPublisher : IConsolidacaoEventPublisher
{
    private readonly ITopicProducerProvider _topicProducerProvider;

    public ConsolidacaoEventPublisher(ITopicProducerProvider topicProducerProvider)
    {
        _topicProducerProvider = topicProducerProvider;
    }

    public async Task PublicarIniciadoAsync(SaldoDiarioConsolidadoIniciadoEvento evento, CancellationToken cancellationToken)
    {
        var producer = _topicProducerProvider.GetProducer<string, SaldoDiarioConsolidadoIniciadoEvento>(
            new Uri($"topic:{KafkaTopics.SaldoDiarioConsolidadoIniciado}"));

        await producer.Produce(ChaveDeParticionamento(evento.IdConta, evento.Data), evento, cancellationToken);
    }

    public async Task PublicarConcluidoAsync(SaldoDiarioConsolidadoConcluidoEvento evento, CancellationToken cancellationToken)
    {
        var producer = _topicProducerProvider.GetProducer<string, SaldoDiarioConsolidadoConcluidoEvento>(
            new Uri($"topic:{KafkaTopics.SaldoDiarioConsolidadoConcluido}"));

        await producer.Produce(ChaveDeParticionamento(evento.IdConta, evento.Data), evento, cancellationToken);
    }

    public async Task PublicarComFalhasAsync(SaldoDiarioConsolidadoComFalhasEvento evento, CancellationToken cancellationToken)
    {
        var producer = _topicProducerProvider.GetProducer<string, SaldoDiarioConsolidadoComFalhasEvento>(
            new Uri($"topic:{KafkaTopics.SaldoDiarioConsolidadoComFalhas}"));

        await producer.Produce(ChaveDeParticionamento(evento.IdConta, evento.Data), evento, cancellationToken);
    }

    private static string ChaveDeParticionamento(Guid idConta, DateOnly data) => $"{idConta}|{data:yyyyMMdd}";
}
