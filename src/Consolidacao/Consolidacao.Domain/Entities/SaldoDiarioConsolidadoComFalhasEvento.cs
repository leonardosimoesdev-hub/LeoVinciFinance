using BuildingBlocks.Common.Domain;

namespace Consolidacao.Domain.Entities;

public class SaldoDiarioConsolidadoComFalhasEventoEntity : Entity
{
    public Guid IdConta { get; private set; }

    public DateOnly Data { get; private set; }

    public Guid CorrelationId { get; private set; }

    public int Tentativas { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    public DateTimeOffset CriadoEm { get; private set; }

    private SaldoDiarioConsolidadoComFalhasEventoEntity() { }

    internal SaldoDiarioConsolidadoComFalhasEventoEntity(Guid idConta, DateOnly data, Guid correlationId, int tentativas, string mensagem)
        : base(Guid.NewGuid())
    {
        IdConta = idConta;
        Data = data;
        CorrelationId = correlationId;
        Tentativas = tentativas;
        Mensagem = mensagem;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    public static SaldoDiarioConsolidadoComFalhasEventoEntity Create(Guid idConta, DateOnly data, Guid correlationId, int tentativas, string mensagem) =>
        new(idConta, data, correlationId, tentativas, mensagem);

    public SaldoDiarioConsolidadoComFalhasEventoEntity IncrementTentativas()
    {
        Tentativas++;
        return this;
    }
}
