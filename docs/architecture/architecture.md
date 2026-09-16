# LeoVinciFinance — Documento de Arquitetura



### Verificação de aderência a `REQUISITOS.md`

* **"Serviço que faça o controle de lançamentos" + "Serviço do consolidado diário"** → mapeados para os módulos `Financeiro` (lançamentos) e `Relatorios`/`Consolidacao` (saldo diário consolidado), respectivamente. Sem conflito.
* **"O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair"** → requisito não funcional explícito da prova, **confirmado como já atendido pelo desenho atual**: `Financeiro.Api` nunca chama `Relatorios.Api` nem o worker de `Consolidacao`; a dependência é sempre no sentido contrário (Consolidação → Financeiro/Relatórios via HTTP, e Consolidação ↔ Kafka). Uma queda de Relatórios/Consolidação não impede a criação de novos lançamentos. Detalhado no ADR 0011.
* **"Em dias de pico, o serviço de consolidado diário recebe 50 RPS, com no máximo 5% de perda"** → mapeado para `Relatorios.Api` (é o serviço de consulta/criação do consolidado diário exposto via HTTP); já tratado nas seções 6 e 9 deste documento e no ADR 0009.
* **"Hospedar em repositório público (GitHub)" / "Todas as documentações de projeto devem estar no repositório"** → requisito operacional (fora do escopo de decisão arquitetural), registrado aqui para não ser esquecido nas fases finais (README + push do repositório público antes da entrega).
* **Escopo de autenticação/perfis (Admin/Comerciante), multi-tenancy (Cliente/Conta), Kafka/MassTransit, YARP, PostgreSQL, .NET 10 etc.** → não são exigidos literalmente por `REQUISITOS.md` (que descreve um problema de negócio simples: "um comerciante controla seu fluxo de caixa"). São decisões minhas, adotadas amparadas pela própria prova, que convida explicitamente a ir além do mínimo ("não se prenda somente a eles... demonstre melhor suas capacidades"). Registrado aqui como **interpretação/ampliação de escopo, não requisito de negócio inventado em contradição com a prova** — a prova não proíbe essa ampliação, apenas não a exige.
* Os princípios de Clean Architecture aplicados seguem a formulação clássica (Robert C. Martin) — ver ADR 0001. (Robert C. Martin — regra de dependência, camadas Domain/Application/Infrastructure/Presentation) com o repositório de referência `https://github.com/leonardopinto/clean-arc-example`.



## 1\. Visão Geral

LeoVinciFinance é um sistema de controle financeiro multi-tenant (por cliente/conta) com consolidação diária de saldos, organizado como uma **arquitetura modular distribuída**: quatro módulos de negócio (Auth, Financeiro, Relatórios, Consolidação), cada um internamente estruturado em Clean Architecture, expostos via um Gateway YARP e integrados de forma síncrona (HTTP/Refit) e assíncrona (Kafka/MassTransit).

Objetivo funcional central: usuários (Admin/Comerciante) lançam movimentações financeiras (`Lancamento`) em contas; um processo assíncrono consolida diariamente o saldo por conta (`SaldoDiarioConsolidado`) e o expõe via API de Relatórios, sob um requisito não funcional de 50 RPS com até 5% de perda.

## 2\. Módulos (Bounded Contexts)

Extraídos do `drawio` de domínio (ver diagrams\\LeoVinciFinance.Domain.drawio:

### 2.1 Auth

* **Perfil**: `Id`, `Nome` (1 = Admin, 2 = Comerciante)
* **User/Usuario**: `Id` (long), `Username`, `PasswordHash`, `Perfil`
* Responsável por `POST /api/auth/login` e `POST /api/auth/token`.

### 2.2 Financeiro

* **Cliente**: `Id` (Guid), `IdUsuario`, `Nome`
* **Conta**: `Id` (Guid), `IdCliente`
* **Lancamento**: `Id` (Guid), `IdConta`, `Valor` (decimal, ≠ 0, positivo ou negativo), `Data`/`DataHora`
* Endpoints: consulta de contas por usuário (Admin), consulta de lançamentos por conta/data (Admin — usado pela Consolidação), criação de lançamento (Admin/Comerciante).

### 2.3 Relatórios

* **SaldoDiarioConsolidado**: `IdConta`, `Data`, `Saldo`, com constraint única `(IdConta, Data)`.
* Endpoints: consulta de saldo consolidado (Admin/Comerciante, com cache de resposta de 1 dia por conta) e criação idempotente do saldo consolidado (somente Admin — usado pela Consolidação).

### 2.4 Consolidação (Agente)

* **Job (abstrato)**: `Id`, `Nome`, `Etapas`, `Execucoes`, `LimiteTentativas`
* **SaldoDiarioConsolidadoJob** (`Job`): `IdConta`
* **Evento**: `Id`, `Nome` — valores: `SaldoDiarioConsolidadoIniciado`, `SaldoDiarioConsolidadoConcluido`, `SaldoDiarioConsolidadoComFalhas`
* **Etapa**: `Id`, `Job`, `Evento`, `EventoAnterior`, `Ordem` — modela a evolução do job
* **Execucao**: `Id`, `IdJob`, `IdEvento`, `DataHora`, `Mensagem`
* Correlação de ponta a ponta via `CorrelationId`.
* Composto por: `SaldoDiarioConsolidadoHostedService` (agendador diário, D‑1), consumidores MassTransit dos três eventos, `SaldoDiarioConsolidadoComFalhasHostedService` (retry de falhas) e `SaldoDiarioConsolidadoGapsHostedService` (recuperação de gaps históricos).

## 3\. Regra de dependência (Clean Architecture)

Cada módulo (`Auth`, `Financeiro`, `Relatorios`) segue:

```
Domain  <—  Application  <—  Infrastructure
                 ^
                 |
               Api (composition root)
```

* **Domain**: entidades, value objects, regras intrínsecas (ex.: `Lancamento.Valor != 0`), sem dependência de EF Core, ASP.NET, Kafka, MassTransit, Refit, Polly, JWT, Serilog, OpenTelemetry ou cache.
* **Application**: casos de uso (Commands/Queries/Handlers), depende apenas de Domain e de abstrações próprias (`IRepository`, `IUnitOfWork`, `IEventPublisher`, etc.).
* **Infrastructure**: implementa as abstrações (EF Core + PostgreSQL, MassTransit + Kafka, Refit + Polly, cache).
* **Api**: HTTP, autenticação/autorização, Swagger, composição de DI (`AddXxxModule()`), validação de entrada.

O módulo `Consolidacao` substitui `Api` por `BackgroundServices` (hosted services + consumers), pois não expõe HTTP — apenas processa eventos e agenda jobs.

Essa regra será validada automaticamente na Fase 3+ via testes de arquitetura (ArchUnitNET ou equivalente) — ver seção 34 do prompt mestre.

## 4\. Comunicação entre módulos

### 4.1 Síncrona (HTTP)

* Consolidação → Financeiro: `GET /api/financeiro/contas/{idConta}/lancamentos/{data}` (obter lançamentos do dia).
* Consolidação → Relatórios: `POST /api/relatorios/saldo-diario-consolidado` (persistir saldo).
* Implementada via **Refit** (clientes tipados) + **Polly** (timeout 1–5s configurável, retry limitado 3–5 tentativas, circuit breaker). Operações não idempotentes por natureza não recebem retry automático indiscriminado — ver ADR de resiliência.

### 4.2 Assíncrona (Kafka + MassTransit)

* Eventos `SaldoDiarioConsolidadoIniciado/Concluido/ComFalhas`, publicados/consumidos via MassTransit sobre Kafka.
* `CorrelationId` propagado em todas as mensagens.
* Consumidores idempotentes (mensagem duplicada não gera segunda consolidação) — verificação de estado do Job/Execução antes de processar.

## 5\. Modelo de deployment (ambiente local)

```
                         Clientes / Swagger
                                |
                          YARP Gateway
                                |
        +-----------+-----------+-----------+
        |           |           |           |
   Auth.Api   Financeiro.Api Financeiro.Api Relatorios.Api (x2)
   (1 inst.)   (réplica 1)    (réplica 2)
        |           |           |           |
        +-----------+-----------+-----------+
                                |
                          PostgreSQL
                       (schema por módulo)
                                |
                              Kafka  <---> KafkaDrop (inspeção)
                                |
                  Consolidacao.BackgroundServices
                                |
                       Aspire Dashboard (OTel)
```

* Gateway YARP: roteamento, load balancing (round-robin nativo do YARP entre destinos do cluster) e health checks por destino.
* `Financeiro.Api` e `Relatorios.Api`: 2 réplicas cada no Docker Compose, para demonstrar escalabilidade horizontal stateless (estado fica no PostgreSQL/Kafka).
* `Auth.Api`: 1 instância nesta fase (não há requisito de carga sobre login); pode ser escalada da mesma forma se necessário.
* `Consolidacao.BackgroundServices`: processo(s) worker, sem exposição HTTP.

Detalhamento completo em `docs/c4/deployment.md`.

## 6\. Requisito de performance (Relatórios)

* Requisito da prova: pico de 50 RPS, tolerância ≤ 5% de perda.
* Configuração operacional inicial: rate limit de 60 RPS na API de Relatórios (margem de 20% sobre o requisito), configurável via `appsettings`/variável de ambiente.
* O atendimento ao requisito só será considerado validado após o teste de carga da Fase 8 — a configuração do rate limit, por si só, não comprova o requisito (ver seção 8 do prompt mestre).
* Dimensionamento do connection pool do PostgreSQL para o módulo de Relatórios será calculado e documentado (com o raciocínio, não apenas o número) nas Fases 4/8, considerando: 2 réplicas × conexões por réplica, tempo médio esperado de consulta, e o limite de conexões do PostgreSQL. Placeholder registrado em `docs/adr/0009-connection-pool-relatorios.md` — valor final será fechado quando houver medição real.

## 7\. Segurança

* JWT emitido por `Auth.Api`, contendo no mínimo `IdUsuario`, `IdConta` (quando aplicável), `Username`, `Perfil`.
* Perfis: `Admin`, `Comerciante`, com autorização baseada em roles/policies.
* Autorização validada em cada serviço responsável pela operação (não apenas no Gateway).
* Senhas: hash com algoritmo moderno (Identity/BCrypt/Argon2 — decisão final registrada em ADR na Fase 4), nunca texto puro ou criptografia reversível.
* Segredos de desenvolvimento fora do código-fonte (user-secrets/variáveis de ambiente), nunca hardcoded em build de produção.

## 8\. Observabilidade

* OpenTelemetry para traces e métricas; Serilog para logs estruturados (correlation id, request id, duração, eventos de consolidação).
* Aspire Dashboard Standalone como visualizador local (substitui um APM completo neste escopo de prova técnica).

## 9\. Persistência

* PostgreSQL, um schema por módulo/domínio (`auth`, `financeiro`, `relatorios`, `consolidacao`).
* `numeric(18,2)` para colunas monetárias; `decimal` no Domain — nunca `double`/`float`.
* Separação entre leitura (`AsNoTracking()`) e escrita nos repositórios; sem repository genérico que esconda funcionalidades do EF Core sem necessidade.

## 10\. Testes (estratégia, detalhamento na Fase 3+)

* Unitários: regras de negócio (lançamentos, autorização de conta, consolidação, idempotência, recuperação de falhas, gaps, limite de tentativas).
* Arquiteturais: regra de dependência entre camadas (ArchUnitNET ou equivalente para .NET 10).
* Integração: APIs + PostgreSQL + Kafka + autenticação + fluxo de consolidação.
* E2E: login → lançamento → evento Kafka → consolidação → persistência → consulta de saldo.
* Carga: ≥ 50 RPS na API de Relatórios, com resultados documentados.

## 11\. Trade-offs e pontos em aberto para as próximas fases

* Algoritmo exato de hashing de senha: decisão adiada para a Fase 4 (ADR dedicado).
* Estratégia exata de particionamento/chaveamento dos tópicos Kafka: decisão adiada para a Fase 4/6.
* Números finais de connection pool/rate limit: dependem de medição em Fase 8; valores desta fase são estimativas de partida, não conclusões.
* Estrutura de pastas da solution pode ser levemente ajustada na Fase 2 caso surja razão arquitetural concreta (a estrutura-base já está definida na seção 5 do prompt mestre e será respeitada).
* Publicação em repositório GitHub público e conferência de que toda a documentação (`docs/`, README) está versionada: pendência operacional a resolver antes da entrega final (Fase 9), conforme `REQUISITOS.md`.

