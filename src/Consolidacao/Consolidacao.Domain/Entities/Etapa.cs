using BuildingBlocks.Common.Domain;
using Consolidacao.Domain.Enums;

namespace Consolidacao.Domain.Entities;

/// <summary>
/// Uma etapa do fluxo de um Job — registra a transição de um evento para o próximo
/// (docs/c4/component.md: "Etapa: Id, IdJob, Evento, EventoAnterior, Ordem").
/// </summary>
public class Etapa : Entity
{
    public Guid IdJob { get; private set; }

    public EventoTipo Evento { get; private set; }

    public EventoTipo? EventoAnterior { get; private set; }

    public int Ordem { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    private Etapa() { }

    internal Etapa(Guid idJob, EventoTipo evento, EventoTipo? eventoAnterior, int ordem)
        : base(Guid.NewGuid())
    {
        IdJob = idJob;
        Evento = evento;
        EventoAnterior = eventoAnterior;
        Ordem = ordem;
        CriadoEm = DateTimeOffset.UtcNow;
    }
}
