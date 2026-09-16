# ADR 0008 — Observabilidade com OpenTelemetry, Serilog e Aspire Dashboard

## Status

Aceita

## Contexto

A prova exige rastreabilidade de ponta a ponta do fluxo de consolidação e visibilidade operacional das APIs.

## Decisão

* OpenTelemetry para traces e métricas em todas as APIs e no worker de Consolidação.
* Serilog para logs estruturados, incluindo `CorrelationId`, request id, nome do serviço, duração da operação e eventos de consolidação.
* Aspire Dashboard Standalone como visualizador local unificado (traces + métricas + logs), evitando a necessidade de um stack de observabilidade completo (Prometheus/Grafana/Jaeger) apenas para o escopo da prova.

## Consequências

* Ganho: rastreamento de uma requisição/mensagem através de Gateway → API → evento Kafka → consumidor, correlacionando tudo pelo `CorrelationId`.
* Trade-off: Aspire Dashboard Standalone é adequado para ambiente local de desenvolvimento/demonstração; não substitui uma solução de observabilidade de produção — documentado como escopo intencional da prova técnica.

