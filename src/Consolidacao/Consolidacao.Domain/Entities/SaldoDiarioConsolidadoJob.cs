using Consolidacao.Domain.Exceptions;

namespace Consolidacao.Domain.Entities;

/// <summary>
/// Job concreto que representa o processo de consolidação do saldo diário de UMA conta em
/// UMA data. Um por (IdConta, Data) — a unicidade de fato é garantida por índice único em
/// Infrastructure (mesmo padrão de SaldoDiarioConsolidado em Relatorios.Domain).
/// </summary>
public class SaldoDiarioConsolidadoJob : Job
{
    public Guid IdConta { get; private set; }

    public DateOnly Data { get; private set; }

    /// <summary>
    /// CorrelationId usado em todo o fluxo (log estruturado + propagado nos 3 eventos Kafka),
    /// para permitir rastrear a jornada completa de uma consolidação específica
    /// (Especificação Mestre, seção 10/18).
    /// </summary>
    public Guid CorrelationId { get; private set; }

    private SaldoDiarioConsolidadoJob() { }

    private SaldoDiarioConsolidadoJob(Guid id, Guid idConta, DateOnly data, int limiteTentativas, Guid correlationId)
        : base(id, $"SaldoDiarioConsolidado:{idConta}:{data:yyyy-MM-dd}", limiteTentativas)
    {
        IdConta = idConta;
        Data = data;
        CorrelationId = correlationId;
    }

    public static SaldoDiarioConsolidadoJob Criar(Guid idConta, DateOnly data, int limiteTentativas)
    {
        if (idConta == Guid.Empty)
            throw new ConsolidacaoDomainException("IdConta é obrigatório.");

        return new SaldoDiarioConsolidadoJob(Guid.NewGuid(), idConta, data, limiteTentativas, Guid.NewGuid());
    }
}
