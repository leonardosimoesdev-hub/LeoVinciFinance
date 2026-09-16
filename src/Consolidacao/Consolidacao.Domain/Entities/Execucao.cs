using BuildingBlocks.Common.Domain;
using Consolidacao.Domain.Enums;

namespace Consolidacao.Domain.Entities;

/// <summary>
/// Registro de uma tentativa concreta de processar um evento do Job — histórico de auditoria
/// e base para a política de retry/limite de tentativas (docs/c4/component.md: "Execucao:
/// Id, IdJob, IdEvento, DataHora, Mensagem").
/// </summary>
public class Execucao : Entity
{
    public Guid IdJob { get; private set; }

    public EventoTipo Evento { get; private set; }

    public StatusExecucao Status { get; private set; }

    public DateTimeOffset DataHora { get; private set; }

    public string Mensagem { get; private set; } = string.Empty;

    private Execucao() { }

    internal Execucao(Guid idJob, EventoTipo evento, StatusExecucao status, string mensagem)
        : base(Guid.NewGuid())
    {
        IdJob = idJob;
        Evento = evento;
        Status = status;
        Mensagem = mensagem;
        DataHora = DateTimeOffset.UtcNow;
    }
}
