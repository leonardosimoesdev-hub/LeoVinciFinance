# ADR 0002 — Gateway YARP e Estratégia de Escalabilidade Horizontal

## Status

Aceita

## Contexto

A prova exige demonstrar escalabilidade horizontal, com duas réplicas de cada API HTTP (Financeiro e Relatórios) e um componente de roteamento/load balancing (seção 6/7 do prompt mestre).

## Decisão

* Utilizar YARP como Gateway único de entrada, responsável por roteamento, load balancing e health checks por destino.
* `Financeiro.Api` e `Relatorios.Api` rodam com 2 réplicas cada no Docker Compose; `Auth.Api` com 1 réplica nesta fase.
* Nenhuma sessão/estado fixado em uma réplica específica (APIs stateless); estado reside em PostgreSQL/Kafka.

## Consequências

* Ganho: demonstração direta de escalabilidade horizontal sem necessidade de orquestrador externo (Kubernetes) no escopo local da prova.
* Trade-off: balanceamento round-robin simples do YARP não considera carga real das instâncias — aceitável para o escopo de prova técnica local; documentado como simplificação.

