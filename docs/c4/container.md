# C4 — Nível 2: Containers

```mermaid
C4Container

title LeoVinciFinance — Diagrama de Containers



Person(admin, "Administrador")
Person(comerciante, "Comerciante")



&#x20;   System\_Boundary(leovinci, "LeoVinciFinance") {

&#x20;       Container(gateway, "Gateway", "YARP / ASP.NET Core", "Roteamento, load balancing e health checks")

&#x20;       Container(auth, "Auth.Api", "ASP.NET Core", "Login e emissão/validação de JWT")

&#x20;       Container(financeiro, "Financeiro.Api", "ASP.NET Core (2 réplicas)", "Clientes, contas e lançamentos")

&#x20;       Container(relatorios, "Relatorios.Api", "ASP.NET Core (2 réplicas)", "Consulta e criação de saldo diário consolidado")

&#x20;       Container(worker, "Consolidacao.BackgroundServices", ".NET Worker", "Agendamento, consumo de eventos e recuperação de falhas/gaps")

&#x20;       ContainerDb(postgres, "PostgreSQL", "PostgreSQL", "Um schema por módulo (auth, financeiro, relatorios, consolidacao)")

&#x20;       ContainerQueue(kafka, "Kafka", "Apache Kafka", "Eventos de consolidação")

&#x20;       Container(kafkadrop, "KafkaDrop", "Ferramenta local", "Inspeção de tópicos Kafka")

&#x20;       Container(aspire, "Aspire Dashboard", "Ferramenta local", "Traces, métricas e logs (OpenTelemetry)")

&#x20;   }



&#x20;   Rel(admin, gateway, "HTTPS/JWT")

&#x20;   Rel(comerciante, gateway, "HTTPS/JWT")



&#x20;   Rel(gateway, auth, "Roteia /api/auth/\*")

&#x20;   Rel(gateway, financeiro, "Roteia /api/financeiro/\*", "Load balanced")

&#x20;   Rel(gateway, relatorios, "Roteia /api/relatorios/\*", "Load balanced")



&#x20;   Rel(financeiro, postgres, "Lê/escreve", "EF Core, schema financeiro")

&#x20;   Rel(relatorios, postgres, "Lê/escreve", "EF Core, schema relatorios")

&#x20;   Rel(auth, postgres, "Lê/escreve", "EF Core, schema auth")

&#x20;   Rel(worker, postgres, "Lê/escreve", "EF Core, schema consolidacao")



&#x20;   Rel(worker, financeiro, "GET lançamentos do dia", "HTTP/Refit+Polly")

&#x20;   Rel(worker, relatorios, "POST saldo consolidado", "HTTP/Refit+Polly")



&#x20;   Rel(worker, kafka, "Publica/consome eventos SaldoDiarioConsolidado\*", "MassTransit")

&#x20;   Rel(kafkadrop, kafka, "Inspeciona tópicos")



&#x20;   Rel(auth, aspire, "Exporta traces/métricas/logs")

&#x20;   Rel(financeiro, aspire, "Exporta traces/métricas/logs")

&#x20;   Rel(relatorios, aspire, "Exporta traces/métricas/logs")

&#x20;   Rel(worker, aspire, "Exporta traces/métricas/logs")

```

## Notas

* `Financeiro.Api` e `Relatorios.Api` aparecem como containers únicos com anotação "(2 réplicas)" — a topologia física detalhada está em `deployment.md`.
* O worker de Consolidação é o único container que fala tanto com Kafka quanto, via HTTP, com Financeiro e Relatórios — ele é o orquestrador do fluxo de consolidação.

