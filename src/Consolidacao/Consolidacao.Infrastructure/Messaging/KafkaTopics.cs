namespace Consolidacao.Infrastructure.Messaging;

/// <summary>
/// Nomes dos tópicos Kafka do fluxo de consolidação — constantes (nunca strings soltas pelo
/// código, Ajustes round 1). Como os 3 eventos são produzidos E consumidos inteiramente
/// dentro de Consolidacao.Infrastructure, não há necessidade de compartilhar este arquivo com
/// nenhum outro módulo.
/// </summary>
public static class KafkaTopics
{
    public const string SaldoDiarioConsolidadoIniciado = "leovincifinance.consolidacao.saldo-diario-consolidado-iniciado";
    public const string SaldoDiarioConsolidadoConcluido = "leovincifinance.consolidacao.saldo-diario-consolidado-concluido";
    public const string SaldoDiarioConsolidadoComFalhas = "leovincifinance.consolidacao.saldo-diario-consolidado-com-falhas";

    /// <summary>Nome dos consumer groups — um por consumidor, para permitir escalar cada etapa do fluxo de forma independente.</summary>
    public static class GruposDeConsumidores
    {
        public const string Iniciado = "consolidacao-iniciado-consumer-group";
        public const string Concluido = "consolidacao-concluido-consumer-group";
        public const string ComFalhas = "consolidacao-com-falhas-consumer-group";
    }
}
