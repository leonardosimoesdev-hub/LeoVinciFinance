# ADR 0010 — Estratégia de Consolidação Diária, Retry e Recuperação de Gaps

## Status

Aceita

## Contexto

A consolidação de saldo é um processo agendado (D-1) que precisa lidar com falhas transitórias e com lacunas históricas (dias não consolidados por indisponibilidade passada).

## Decisão

Três hosted services com responsabilidades distintas:

1. **`SaldoDiarioConsolidadoHostedService`**: roda em intervalo configurável, identifica contas/dias ainda não consolidados (padrão D-1) e publica `SaldoDiarioConsolidadoIniciado` — sem criar duplicidade em re-execuções (checa existência antes de publicar).
2. **`SaldoDiarioConsolidadoComFalhasHostedService`**: localiza execuções com erro ainda elegíveis (dentro do `LimiteTentativas`) e republica `SaldoDiarioConsolidadoIniciado` para reprocessamento.
3. **`SaldoDiarioConsolidadoGapsHostedService`**: varre execuções bem-sucedidas, identifica dias ausentes a partir de uma data inicial configurável, cria as consolidações faltantes e publica os eventos de inicialização correspondentes.

Essa separação evita um único componente monolítico responsável por agendamento normal, retry e reparo histórico — cada hosted service tem uma única responsabilidade (SRP).

## Consequências

* Ganho: cada mecanismo de recuperação é testável e configurável isoladamente (intervalos, limites e data inicial de busca independentes).
* Custo: três processos de fundo coexistindo sobre a mesma tabela de execuções exige cuidado com concorrência — mitigado pelas checagens de idempotência do ADR 0007.

