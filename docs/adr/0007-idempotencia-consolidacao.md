# ADR 0007 — Idempotência no Fluxo de Consolidação

## Status

Aceita

## Contexto

Mensagens Kafka podem ser entregues em duplicidade (at-least-once); a prova exige explicitamente que uma mensagem duplicada não gere uma segunda consolidação incorreta.

## Decisão

* Antes de processar `SaldoDiarioConsolidadoIniciado`, o consumidor verifica:

  1. se o Job/Execução já foi concluído para aquela conta/data → loga e finaliza sem reprocessar;
  2. se o saldo já foi consolidado em Relatórios → loga e apenas publica `SaldoDiarioConsolidadoConcluido`, sem recalcular.
* A criação de saldo consolidado em Relatórios é idempotente por construção: constraint única `(IdConta, Data)` no banco + checagem prévia na Application antes do insert; tentativa duplicada não cria novo registro, apenas loga e retorna sucesso.
* O consumidor de `SaldoDiarioConsolidadoConcluido` também verifica se a execução já foi registrada como concluída antes de gravar, evitando duplicidade de registro de execução.
* O consumidor de `SaldoDiarioConsolidadoComFalhas` incrementa um contador de tentativas associado ao Job/Execução (não a cada mensagem recebida isoladamente), respeitando `LimiteTentativas`.

## Consequências

* Ganho: tolerância a entrega duplicada de mensagens sem corromper o saldo consolidado.
* Custo: toda operação de escrita no fluxo de consolidação precisa de uma checagem de estado antes de agir — aceito como custo necessário de correção.

