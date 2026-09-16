# C4 — Nível 4: Deployment (ambiente local Docker Compose)

```mermaid
C4Deployment

&#x20;   title LeoVinciFinance — Deployment local (Docker Compose)



&#x20;   Deployment\_Node(host, "Host Docker (dev)", "Docker Compose") {



&#x20;       Deployment\_Node(gatewayNode, "gateway", "Container") {

&#x20;           Container(gateway, "YARP Gateway")

&#x20;       }



&#x20;       Deployment\_Node(authNode, "auth-api", "Container") {

&#x20;           Container(auth, "Auth.Api")

&#x20;       }



&#x20;       Deployment\_Node(finNode1, "financeiro-api-1", "Container") {

&#x20;           Container(fin1, "Financeiro.Api — réplica 1")

&#x20;       }

&#x20;       Deployment\_Node(finNode2, "financeiro-api-2", "Container") {

&#x20;           Container(fin2, "Financeiro.Api — réplica 2")

&#x20;       }



&#x20;       Deployment\_Node(relNode1, "relatorios-api-1", "Container") {

&#x20;           Container(rel1, "Relatorios.Api — réplica 1")

&#x20;       }

&#x20;       Deployment\_Node(relNode2, "relatorios-api-2", "Container") {

&#x20;           Container(rel2, "Relatorios.Api — réplica 2")

&#x20;       }



&#x20;       Deployment\_Node(workerNode, "consolidacao-worker", "Container") {

&#x20;           Container(worker, "Consolidacao.BackgroundServices")

&#x20;       }



&#x20;       Deployment\_Node(pgNode, "postgres", "Container") {

&#x20;           ContainerDb(pg, "PostgreSQL")

&#x20;       }



&#x20;       Deployment\_Node(kafkaNode, "kafka", "Container") {

&#x20;           ContainerQueue(kafka, "Apache Kafka")

&#x20;       }



&#x20;       Deployment\_Node(kafkadropNode, "kafkadrop", "Container") {

&#x20;           Container(kafkadrop, "KafkaDrop")

&#x20;       }



&#x20;       Deployment\_Node(aspireNode, "aspire-dashboard", "Container") {

&#x20;           Container(aspire, "Aspire Dashboard Standalone")

&#x20;       }

&#x20;   }



&#x20;   Rel(gateway, auth, "HTTP")

&#x20;   Rel(gateway, fin1, "HTTP (load balanced)")

&#x20;   Rel(gateway, fin2, "HTTP (load balanced)")

&#x20;   Rel(gateway, rel1, "HTTP (load balanced)")

&#x20;   Rel(gateway, rel2, "HTTP (load balanced)")



&#x20;   Rel(auth, pg, "EF Core")

&#x20;   Rel(fin1, pg, "EF Core")

&#x20;   Rel(fin2, pg, "EF Core")

&#x20;   Rel(rel1, pg, "EF Core")

&#x20;   Rel(rel2, pg, "EF Core")

&#x20;   Rel(worker, pg, "EF Core")



&#x20;   Rel(worker, gateway, "HTTP/Refit+Polly")

&#x20;   Rel(worker, kafka, "MassTransit")

&#x20;   Rel(kafkadrop, kafka, "Inspeção")



&#x20;   Rel(auth, aspire, "OTel")

&#x20;   Rel(fin1, aspire, "OTel")

&#x20;   Rel(rel1, aspire, "OTel")

&#x20;   Rel(worker, aspire, "OTel")

```

## Racional

* **2 réplicas** para `Financeiro.Api` e `Relatorios.Api`: demonstra escalabilidade horizontal exigida pela prova (seção 6/7 do prompt mestre); ambas as APIs são stateless (estado em PostgreSQL/Kafka), o que torna a replicação segura sem sessão fixa.
* **YARP** faz o load balancing entre as réplicas de cada API e expõe health checks para remover instâncias indisponíveis do pool de roteamento.
* **1 instância** de `Auth.Api` e do worker de `Consolidacao`: nenhum requisito de carga da prova recai sobre login ou sobre o worker; ambos podem ser escalados posteriormente sem mudança estrutural (Auth.Api de forma idêntica às demais APIs; o worker exigiria partição de consumo Kafka por grupo de consumidores, decisão adiada — ver seção 11 de `architecture.md`).
* **PostgreSQL e Kafka** como nós únicos no ambiente local (não há requisito de cluster/HA para a prova técnica; documentado como simplificação intencional do ambiente de desenvolvimento).

