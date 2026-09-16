namespace Consolidacao.Domain.Enums;

/// <summary>
/// Os três eventos do fluxo de consolidação diária (docs/architecture/architecture.md,
/// seção 2.4, e docs/c4/component.md — "Evento: Id, Nome"). Modelado como enum (mesma
/// abordagem já usada para Auth.Domain.Enums.Perfil) em vez de uma entidade de catálogo
/// separada: são valores fixos e fechados, não dados configuráveis em runtime.
/// </summary>
public enum EventoTipo
{
    SaldoDiarioConsolidadoIniciado = 1,
    SaldoDiarioConsolidadoConcluido = 2,
    SaldoDiarioConsolidadoComFalhas = 3
}

/// <summary>Resultado de uma tentativa de execução de uma etapa do Job (seção 18/25).</summary>
public enum StatusExecucao
{
    Sucesso = 1,
    Erro = 2
}
