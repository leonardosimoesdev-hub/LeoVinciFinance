# ADR 0003 — Kafka + MassTransit para Mensageria Assíncrona

## Status

Aceita

## Contexto

O fluxo de consolidação diária precisa ser assíncrono, desacoplado e resiliente a falhas, com múltiplas etapas (início, conclusão, falha) e possibilidade de reprocessamento.

## Decisão

* Usar Apache Kafka como broker e MassTransit como abstração de mensageria (produção/consumo, retry de consumidor, correlação).
* Três eventos de domínio: `SaldoDiarioConsolidadoIniciado`, `SaldoDiarioConsolidadoConcluido`, `SaldoDiarioConsolidadoComFalhas`, cada um com consumidor dedicado.
* `CorrelationId` propagado em todas as mensagens para rastreabilidade ponta a ponta (log estruturado + OpenTelemetry).
* KafkaDrop disponibilizado no ambiente local para inspeção manual dos tópicos.

## Consequências

* Ganho: desacoplamento entre Financeiro/Relatórios e o processo de consolidação; tolerância a picos e falhas transitórias.
* Custo: complexidade operacional adicional (broker, schemas de mensagem, consumidores idempotentes) — necessária para atender aos requisitos de resiliência e recuperação de falhas/gaps da prova.
* Mecanismos de idempotência e limite de tentativas são detalhados no ADR 0007.

