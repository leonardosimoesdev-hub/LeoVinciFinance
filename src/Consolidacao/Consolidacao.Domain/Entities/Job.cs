using BuildingBlocks.Common.Domain;
using Consolidacao.Domain.Enums;
using Consolidacao.Domain.Exceptions;

namespace Consolidacao.Domain.Entities;

/// <summary>
/// Job: agregado raiz que acompanha o ciclo de vida de um processo de consolidação — suas
/// Etapas (transições evento -> evento) e Execuções (tentativas concretas, com sucesso/erro).
/// Base abstrata para os diferentes tipos de job do sistema (hoje só existe
/// <see cref="SaldoDiarioConsolidadoJob"/>, mas o desenho já comporta outros no futuro — ver
/// docs/architecture/architecture.md, seção 2.4).
/// </summary>
public abstract class Job : Entity
{
    public string Nome { get; private set; } = string.Empty;

    public int LimiteTentativas { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    private readonly List<Etapa> _etapas = new();
    public IReadOnlyCollection<Etapa> Etapas => _etapas.AsReadOnly();

    private readonly List<Execucao> _execucoes = new();
    public IReadOnlyCollection<Execucao> Execucoes => _execucoes.AsReadOnly();

    protected Job() { }

    protected Job(Guid id, string nome, int limiteTentativas) : base(id)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ConsolidacaoDomainException("O nome do Job é obrigatório.");

        if (limiteTentativas <= 0)
            throw new ConsolidacaoDomainException("O limite de tentativas deve ser maior que zero.");

        Nome = nome;
        LimiteTentativas = limiteTentativas;
        CriadoEm = DateTimeOffset.UtcNow;
    }

    /// <summary>Registra a etapa inicial do fluxo e a execução correspondente (evento "Iniciado").</summary>
    public void Iniciar()
    {
        if (_etapas.Count > 0)
            throw new ConsolidacaoDomainException("O Job já foi iniciado anteriormente.");

        _etapas.Add(new Etapa(Id, EventoTipo.SaldoDiarioConsolidadoIniciado, eventoAnterior: null, ordem: 1));
        _execucoes.Add(new Execucao(Id, EventoTipo.SaldoDiarioConsolidadoIniciado, StatusExecucao.Sucesso, "Job iniciado."));
    }

    /// <summary>Registra a conclusão com sucesso do fluxo (evento "Concluído").</summary>
    public void RegistrarConclusao(string mensagem)
    {
        if (FoiConcluido())
            throw new ConsolidacaoDomainException("O Job já foi concluído anteriormente.");

        _etapas.Add(new Etapa(Id, EventoTipo.SaldoDiarioConsolidadoConcluido, EventoTipo.SaldoDiarioConsolidadoIniciado, ordem: _etapas.Count + 1));
        _execucoes.Add(new Execucao(Id, EventoTipo.SaldoDiarioConsolidadoConcluido, StatusExecucao.Sucesso, mensagem));
    }

    /// <summary>Registra uma falha na tentativa atual (evento "Com falhas").</summary>
    public void RegistrarFalha(string mensagem)
    {
        var eventoAnterior = _etapas.Count > 0 ? _etapas[^1].Evento : (EventoTipo?)null;

        _etapas.Add(new Etapa(Id, EventoTipo.SaldoDiarioConsolidadoComFalhas, eventoAnterior, ordem: _etapas.Count + 1));
        _execucoes.Add(new Execucao(Id, EventoTipo.SaldoDiarioConsolidadoComFalhas, StatusExecucao.Erro, mensagem));
    }

    public bool FoiConcluido() =>
        _execucoes.Any(e => e.Evento == EventoTipo.SaldoDiarioConsolidadoConcluido && e.Status == StatusExecucao.Sucesso);

    public int QuantidadeFalhas() => _execucoes.Count(e => e.Status == StatusExecucao.Erro);

    /// <summary>
    /// Verifica se o Job já esgotou as tentativas permitidas — usado pelo
    /// SaldoDiarioConsolidadoComFalhasHostedService (seção 29) para decidir entre reprocessar
    /// ou registrar um alerta operacional definitivo.
    /// </summary>
    public bool ExcedeuLimiteTentativas() => QuantidadeFalhas() >= LimiteTentativas;
}
