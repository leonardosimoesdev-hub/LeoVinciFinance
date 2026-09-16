# ADR 0006 — Resiliência de Integrações Síncronas com Polly

## Status

Aceita

## Contexto

As chamadas HTTP entre Consolidação → Financeiro e Consolidação → Relatórios precisam tolerar falhas transitórias sem gerar efeitos colaterais incorretos.

## Decisão

* Timeout configurável entre 1 e 5 segundos por chamada.
* Retry limitado (3 a 5 tentativas conforme a natureza da operação), aplicado apenas a operações idempotentes:

  * `GET lançamentos por conta/data` (Financeiro): idempotente por natureza → elegível a retry.
  * `POST saldo-diario-consolidado` (Relatórios): tornado idempotente pela regra de negócio (unicidade IdConta+Data e verificação prévia de existência) → elegível a retry.
* Circuit breaker configurado para evitar sobrecarga em cascata quando um serviço downstream está degradado.
* Ao esgotar as tentativas, o consumidor publica `SaldoDiarioConsolidadoComFalhas`, preservando o `IdCorrelationId` — nunca falha silenciosamente.
* Logs estruturados em cada tentativa/falha (Serilog + OpenTelemetry).

## Consequências

* Ganho: previsibilidade de comportamento sob falha parcial; nenhuma operação não idempotente sofre retry indiscriminado.
* Custo: necessidade de desenhar cada endpoint chamado pela Consolidação já pensando em idempotência (documentado no ADR 0007).

