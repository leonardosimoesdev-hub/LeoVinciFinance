using BuildingBlocks.Common.Domain;

namespace Consolidacao.Domain.Entities;

public class SaldoDiarioConsolidadoConcluidoEventoEntity : Entity
{
    public Guid IdConta { get; private set; }

    public DateOnly Data { get; private set; }

    public decimal Saldo { get; private set; }

    public Guid CorrelationId { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; private set; }

    private SaldoDiarioConsolidadoConcluidoEventoEntity() { }

    internal SaldoDiarioConsolidadoConcluidoEventoEntity(Guid idConta, DateOnly data, decimal saldo, Guid correlationId, string mensagem)
        : base(Guid.NewGuid())
    {
        IdConta = idConta;
        Data = data;
        Saldo = saldo;
        CorrelationId = correlationId;
        Mensagem = mensagem;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public static SaldoDiarioConsolidadoConcluidoEventoEntity Create(Guid idConta, DateOnly data, decimal saldo, Guid correlationId, string mensagem) =>
        new(idConta, data, saldo, correlationId, mensagem);
}
