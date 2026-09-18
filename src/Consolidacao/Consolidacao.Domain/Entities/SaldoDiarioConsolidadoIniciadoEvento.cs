using BuildingBlocks.Common.Domain;

namespace Consolidacao.Domain.Entities;

public class SaldoDiarioConsolidadoIniciadoEventoEntity : Entity
{
    public Guid IdConta { get; private set; }

    public DateOnly Data { get; private set; }

    public Guid CorrelationId { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    private SaldoDiarioConsolidadoIniciadoEventoEntity() { }

    internal SaldoDiarioConsolidadoIniciadoEventoEntity(Guid idConta, DateOnly data, Guid correlationId)
        : base(Guid.NewGuid())
    {
        IdConta = idConta;
        Data = data;
        CorrelationId = correlationId;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public static SaldoDiarioConsolidadoIniciadoEventoEntity Create(Guid idConta, DateOnly data, Guid correlationId) =>
        new(idConta, data, correlationId);
}
