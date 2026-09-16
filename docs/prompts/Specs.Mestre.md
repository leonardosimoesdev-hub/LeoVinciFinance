# PROJETO: LeoVinciFinance

Você é o **Arquiteto de Software e Desenvolvedor Sênior responsável pela implementação deste projeto**.

Sua responsabilidade é projetar e implementar uma solução profissional em C#/.NET para a prova técnica fornecida no arquivo `REQUISITOS.md`.

O objetivo é produzir uma solução que possa ser apresentada como uma **obra de arquitetura de software**, demonstrando preocupação com:

* Clean Architecture
* SOLID
* separação de responsabilidades
* modularidade
* segurança
* resiliência
* alta disponibilidade
* escalabilidade horizontal
* observabilidade
* mensageria
* testes automatizados
* documentação arquitetural
* execução local reproduzível

---

# 1. FONTE DE VERDADE

O arquivo:

`REQUISITOS.md`

contém os requisitos da prova técnica.

LEIA ESSE ARQUIVO ANTES DE IMPLEMENTAR.

As regras da prova técnica são obrigatórias.

Não altere, remova ou contradiga requisitos da prova.

Quando uma decisão deste prompt entrar em conflito com um requisito explícito da prova, sinalize o conflito antes de implementar.

Não invente requisitos funcionais que não estejam presentes na prova ou neste prompt.

Decisões arquiteturais podem ser tomadas quando necessárias, mas devem ser documentadas em `docs/architecture.md`.

---

# 2. REFERÊNCIA DE CLEAN ARCHITECTURE

Utilize como referência estrutural o seguinte projeto:

https://github.com/leonardopinto/clean-arc-example

O projeto de referência utiliza Clean Architecture com separação entre:

* Domain
* Application
* Infrastructure
* Web/API

e aplica a regra de dependência da Clean Architecture.

IMPORTANTE:

NÃO copiar a inteligência, regras de negócio ou código do projeto de referência.

Utilizar somente seus princípios de organização arquitetural e separação de responsabilidades.

A implementação deve ser própria e adequada aos requisitos do LeoVinciFinance.

---

# 3. STACK TECNOLÓGICA

Utilizar obrigatoriamente:

* .NET 10
* C#
* ASP.NET Core
* Entity Framework Core
* PostgreSQL
* MassTransit
* Apache Kafka
* Refit
* Polly
* JWT
* YARP
* OpenTelemetry
* Serilog
* Swagger / OpenAPI
* xUnit
* FluentAssertions
* ArchUnitNET, ou alternativa equivalente compatível com .NET 10, para testes arquiteturais
* Docker / Docker Compose
* KafkaDrop para visualização do Kafka local
* Aspire Dashboard Standalone para observabilidade local

Não substituir essas tecnologias sem justificar previamente.

---

# 4. ESTILO ARQUITETURAL

A solução deve ser organizada como uma arquitetura modular distribuída, utilizando princípios de Clean Architecture.

Cada módulo deve possuir clara separação entre:

* Domain
* Application
* Infrastructure
* API/Presentation quando aplicável

A regra de dependência deve apontar para dentro.

O Domain não pode depender de:

* Entity Framework
* PostgreSQL
* Kafka
* MassTransit
* ASP.NET Core
* Refit
* Polly
* JWT
* Serilog
* OpenTelemetry
* Cache (ex.: Redis) para reduzir carga no banco de dados.

O Application pode depender somente do Domain e de abstrações próprias.

Infrastructure implementa as abstrações definidas pelas camadas internas.

API é responsável pela exposição HTTP, autenticação/autorização, configuração e composição das dependências.

Cada módulo deve possuir seu próprio método de configuração de DI, por exemplo:

`AddFinanceiroModule()`

`AddRelatoriosModule()`

`AddAuthModule()`

`AddAgenteModule()`

Evitar um único arquivo gigantesco de configuração de Dependency Injection.

---

# 5. ESTRUTURA DA SOLUTION

Criar:

```text
LLeoVinciFinance/
│
├── src/
│   ├── Auth/
│   │   ├── Auth.Domain/
│   │   ├── Auth.Application/
│   │   ├── Auth.Infrastructure/
│   │   └── Auth.Api/
│   │
│   ├── Financeiro/
│   │   ├── Financeiro.Domain/
│   │   ├── Financeiro.Application/
│   │   ├── Financeiro.Infrastructure/
│   │   └── Financeiro.Api/
│   │
│   ├── Relatorios/
│   │   ├── Relatorios.Domain/
│   │   ├── Relatorios.Application/
│   │   ├── Relatorios.Infrastructure/
│   │   └── Relatorios.Api/
│   │
│   └── Consolidacao/
		├── Consolidacao.Domain/
│       ├── Consolidacao.Application/
│       ├── Consolidacao.Infrastructure/
│       └── Consolidacao.BackgroundServices/
│
├── tests/
│
├── docs/
│   ├── architecture/
│   ├── adr/
│   └── diagrams/
│
├── REQUISITOS.md
├── README.md
├── docker-compose.yml
└── LeoVinciFinance.sln
```

A estrutura pode ser refinada caso exista uma razão arquitetural clara.

Não criar projetos desnecessários.

---

# 6. MODELO DE DEPLOYMENT

A aplicação deverá ser executada localmente através de containers.

Componentes principais:

```text
YARP Gateway
      |
      +-------------------+
      |                   |
 Financeiro.Api      Relatorios.Api
  2 replicas          2 replicas
      |                   |
      +---------+---------+
                |
           PostgreSQL
                |
             Kafka
                |
        Consolidacao Worker
```

Auth.Api também deve ser disponibilizada no ambiente.

A solução deve permitir múltiplas instâncias dos serviços HTTP.

O Gateway YARP será responsável pelo roteamento e load balancing.

Documentar claramente essa decisão arquitetural.

---

# 7. GATEWAY / YARP

Criar um Gateway utilizando YARP.

Responsabilidades:

* roteamento
* load balancing
* health checks quando aplicável
* encaminhamento das requisições para as instâncias disponíveis

Cada API HTTP deverá possuir duas instâncias no ambiente Docker Compose.

O objetivo é demonstrar escalabilidade horizontal.

---

# 8. REQUISITO DE PERFORMANCE

A API de Relatórios deve suportar o requisito da prova:

* pico de 50 requisições por segundo
* tolerância máxima de 5% de perda

Configurar inicialmente um rate limit operacional de 60 RPS para a API de relatórios, permitindo margem sobre o requisito de 50 RPS.

O valor deve ser configurável.

Não afirmar que o requisito está atendido apenas pela configuração do rate limit.

Criar teste de carga que demonstre o comportamento da API com pelo menos 50 RPS.

Documentar:

* quantidade de réplicas
* connection pool
* rate limit
* estratégia de processamento
* latência
* erros
* throughput
* comportamento sob carga

---

# 9. RESILIÊNCIA

Utilizar Polly para resiliência das integrações síncronas HTTP.

Configurar:

* Timeout configurável entre 1 e 5 segundos
* Retry limitado
* Circuit Breaker
* tratamento de falhas
* logs estruturados

Retries devem ser limitados.

Utilizar inicialmente entre 3 e 5 tentativas conforme a natureza da operação.

Quando o limite de tentativas for atingido, deve ser disparado o evento de falha/notificação definido pela arquitetura.

IMPORTANTE:

Não utilizar retry indiscriminadamente em operações não idempotentes.

As políticas de resiliência devem considerar idempotência.

Para comunicação assíncrona utilizar Kafka + MassTransit e mecanismos de retry/erro apropriados ao consumidor.

Documentar todas as decisões.

---

# 10. KAFKA / MASSTRANSIT

Utilizar:

MassTransit → Kafka → consumidores

Kafka será utilizado para eventos assíncronos.

Eventos principais:

```text
SaldoDiarioConsolidadoIniciado
SaldoDiarioConsolidadoConcluido
SaldoDiarioConsolidadoComFalhas
```

Utilizar `IdCorrelationId` como identificador de correlação.

Os consumidores devem ser idempotentes.

Uma mensagem duplicada não pode gerar uma segunda consolidação incorreta.

Configurar tratamento de falhas e mensagens que excederem as tentativas.

Utilizar KafkaDrop no ambiente local para inspeção dos tópicos.

---

# 11. AUTENTICAÇÃO E AUTORIZAÇÃO

Utilizar JWT.

Todas as APIs, exceto o endpoint de autenticação/login, devem exigir token válido.

Utilizar:

```text
POST /api/auth/login
```

para autenticação.

O JWT deve conter pelo menos:

* IdUsuario
* IdConta quando aplicável
* Username
* Perfil

Perfis:

```text
Admin
Comerciante
```

Utilizar autorização baseada em roles/policies.

Não implementar segurança apenas no frontend ou Gateway.

A autorização deve ser validada no serviço responsável pela operação.

---

# 12. SENHAS

Nunca armazenar senha em texto puro.

Nunca armazenar senha utilizando criptografia reversível.

Utilizar um algoritmo moderno de password hashing adequado para .NET.

As credenciais de seed de desenvolvimento não devem ficar hardcoded em código de produção.

Utilizar configuração de desenvolvimento / environment variables / user secrets quando apropriado.

O README deve explicar como configurar as credenciais de desenvolvimento.

---

# 13. BANCO DE DADOS

Utilizar:

* PostgreSQL
* Entity Framework Core

Cada módulo/domínio deve possuir seu próprio schema no PostgreSQL.

Utilizar:

```text
numeric(18,2)
```

para valores monetários.

Utilizar `decimal` no domínio.

Não utilizar `double` ou `float` para dinheiro.

---

# 14. CONEXÕES E CONNECTION POOL

Configurar adequadamente o PostgreSQL connection pool.

Para o módulo de Relatórios, dimensionar o pool considerando:

* duas réplicas
* requisito de 50 RPS
* tempo médio esperado das consultas
* limite de conexões do PostgreSQL
* concorrência

Não escolher números arbitrários.

Documentar no código e em `docs/architecture.md` o raciocínio utilizado para o dimensionamento.

O comentário deve explicar o motivo da configuração e não apenas dizer que foi configurado para 50 RPS.

---

# 15. REPOSITORIES

Criar abstrações de repository na camada apropriada.

Criar separação entre:

* operações de escrita
* operações de consulta

Consultas que não necessitam de alteração devem utilizar:

```csharp
AsNoTracking()
```

quando apropriado.

Evitar abstrações genéricas de repository que escondam funcionalidades importantes do Entity Framework sem necessidade.

---

# 16. DOMAIN

Criar os domínios necessários a seguir o documento em "docs/LeoVinciFinance.Domain.drawio

## Auth

### Perfil

```text
Id
Nome
```

Perfis:

```text
1 = admin
2 = comerciante
```

### User

```text
Id
Username
PasswordHash
Perfil
```

O ID de usuário do módulo Auth será `long`.

---

# 17. FINANCEIRO

Entidades principais:

```text
Cliente
Conta
Lancamento
```

IDs das demais áreas do sistema devem utilizar `Guid`, conforme especificado.

Cliente:

```text
Id
IdUsuario
Nome
```

Conta:

```text
Id
IdCliente
```

Lançamento:

```text
Id
IdConta
Valor
Data
```

Valor deve ser `decimal`.

Valores podem ser positivos ou negativos, mas nunca zero.

---

# 18. AGENTE / CONSOLIDAÇÃO

Criar:

```text
Evento
SaldoDiarioConsolidadoJob
Status
Etapa
```

Eventos:

```text
SaldoDiarioConsolidadoIniciado
SaldoDiarioConsolidadoConcluido
SaldoDiarioConsolidadoComFalhas
```

Status:

```text
Sucesso
Erro
```

Etapa deve representar a evolução da execução do Job.

Registrar:

* evento
* ordem
* evento anterior
* job relacionado

Utilizar correlação através de `IdCorrelationId`.

---

# 19. RELATÓRIOS

Criar:

```text
SaldoDiarioConsolidado
```

Campos:

```text
IdConta
Data
Saldo
```

Criar índice/constraint único composto:

```text
(IdConta, Data)
```

Uma conta não pode possuir dois saldos consolidados para a mesma data.

---

# 20. VIEW

Criar uma database view para consulta das execuções, quando essa view fizer sentido para o modelo de consulta.

Documentar:

* finalidade
* SQL
* entidades envolvidas
* motivo da utilização da view

---

# 21. APPLICATION

Organizar os casos de uso em:

```text
Commands
Queries
Handlers
```

Commands devem representar operações de alteração.

Queries devem representar operações de consulta.

Handlers devem conter a orquestração dos casos de uso.

Regras de negócio importantes devem permanecer no Domain quando forem regras intrínsecas às entidades.

---

# 22. FINANCEIRO API

## GET

```http
GET /api/financeiro/contas/{idUsuario}
```

Somente Admin.

Retorna as contas relacionadas ao usuário.

---

## GET

```http
GET /api/financeiro/contas/{idConta}/lancamentos/{data}
```

Somente Admin.

Retorna os lançamentos da conta na data informada.

Esse endpoint será utilizado pelo serviço de consolidação.

---

## POST

```http
POST /api/financeiro/lancamentos
```

Permitido para:

* Admin
* Comerciante

Payload:

```json
{
  "idConta": "Guid",
  "valor": 100.00
}
```

Validações:

* idConta obrigatório
* valor obrigatório
* valor diferente de zero
* valor pode ser positivo ou negativo
* usuário autenticado deve possuir a conta informada

Se válido:

```text
HTTP 201 Created
```

---

# 23. AUTH API

## Login

```http
POST /api/auth/login
```

Payload:

```json
{
  "username": "username",
  "senha": "senha"
}
```

Validar credenciais.

Gerar JWT.

Adicionar Claims necessários.

---

## Token

```http
POST /api/auth/token
```

Responsável por validar um token JWT.

---

# 24. RELATORIOS API

## Consulta

```http
GET /api/relatorios/saldo-diario-consolidado
```

Permitido para:

* Admin
* Comerciante

Parâmetros:

```text
idConta
data
```

Validar:

* idConta obrigatório
* data obrigatória
* usuário autenticado possui a conta

Retornar saldo consolidado.

utilizar o response cache de 1 dia para um determinada conta 

---

## Criação

```http
POST /api/relatorios/saldo-diario-consolidado
```

Somente Admin.

Payload:

```json
{
  "idConta": "Guid",
  "data": "yyyy-MM-dd",
  "saldo": 100.00
}
```

Validar:

* idConta obrigatório
* data obrigatória
* saldo diferente de zero
* unicidade de IdConta + Data

Caso já exista uma consolidação:

* não criar duplicidade
* registrar log apropriado
* tratar a operação de forma idempotente

Em caso de criação:

```text
HTTP 201 Created
```

---

# 25. CONSOLIDATION BACKGROUND SERVICES

Criar consumidores MassTransit para:

```text
SaldoDiarioConsolidadoIniciado
SaldoDiarioConsolidadoConcluido
SaldoDiarioConsolidadoComFalhas
```

---

## Consumidor: SaldoDiarioConsolidadoIniciado

Mensagem:

```json
{
  "idCorrelationId": "Guid",
  "data": "yyyy-MM-dd",
  "idConta": "Guid"
}
```

Fluxo:

1. verificar se o Job já foi concluído;
2. se já concluído, registrar log e finalizar;
3. verificar se o saldo já foi consolidado;
4. se já existir, registrar log;
5. publicar `SaldoDiarioConsolidadoConcluido`;
6. caso não exista:

   * obter os lançamentos da conta;
   * calcular o saldo;
   * enviar para `POST /api/relatorios/saldo-diario-consolidado`;
7. publicar `SaldoDiarioConsolidadoConcluido`.

Qualquer erro deve:

* registrar log estruturado;
* preservar correlation id;
* publicar `SaldoDiarioConsolidadoComFalhas`.

---

# 26. CONSUMIDOR DE CONCLUIDO

Ao receber:

```text
SaldoDiarioConsolidadoConcluido
```

deve:

1. verificar se o Job já foi concluído;
2. evitar processamento duplicado;
3. registrar a execução concluída.

---

# 27. CONSUMIDOR DE FALHAS

Ao receber:

```text
SaldoDiarioConsolidadoComFalhas
```

deve:

1. registrar erro;
2. incrementar contador de falhas;
3. registrar execução;
4. verificar limite de tentativas;
5. caso tenha ultrapassado o limite:

   * gerar evento de notificação operacional;
6. caso ainda possa tentar novamente:

   * permitir recuperação pelo mecanismo configurado.

O processo deve ser idempotente.

---

# 28. SALDODIARIOCONSOLIDADOHOSTEDSERVICE

Criar um HostedService cronológico com intervalo configurável.

Responsabilidade:

* buscar clientes/contas;
* verificar última execução com sucesso;
* identificar dias ainda não consolidados;
* publicar `SaldoDiarioConsolidadoIniciado`.

A data padrão de consolidação é D-1.

O intervalo de execução deve ser configurável.

Não criar duplicidade caso o serviço seja executado novamente.

---

# 29. RECUPERAÇÃO DE FALHAS

Criar:

```text
SaldoDiarioConsolidadoComFalhasHostedService
```

Responsável por localizar execuções com erro que ainda podem ser recuperadas.

Para cada execução elegível:

```text
publicar SaldoDiarioConsolidadoIniciado
```

Respeitar limite máximo de tentativas.

---

# 30. GAP RECOVERY

Criar:

```text
SaldoDiarioConsolidadoGapsHostedService
```

Responsável por detectar gaps históricos.

Fluxo:

1. buscar execuções bem-sucedidas;
2. identificar dias ausentes;
3. criar as consolidações faltantes;
4. publicar eventos de inicialização.

A data inicial de busca deve ser configurável.

---

# 31. OBSERVABILIDADE

Implementar OpenTelemetry.

Coletar:

* traces
* métricas
* logs quando aplicável

Utilizar Serilog para logs estruturados.

Adicionar:

* correlation id
* request id
* informações de serviço
* informações de erro
* duração das operações
* eventos de consolidação

Utilizar Aspire Dashboard Standalone para visualização local.

---

# 32. SWAGGER

Todas as APIs devem possuir Swagger/OpenAPI.

Documentar:

* endpoints
* payloads
* respostas
* autenticação JWT
* códigos HTTP
* validações

O Swagger deve permitir configurar autenticação Bearer para testes locais.

---

# 33. DOCKER

Criar Dockerfiles adequados.

Criar `docker-compose.yml` para subir o ambiente completo:

* Gateway YARP
* Auth.Api
* Financeiro.Api
* Relatorios.Api
* Consolidacao.BackgroundServices
* múltiplas réplicas das APIs quando aplicável
* PostgreSQL
* Kafka
* KafkaDrop
* Aspire Dashboard

O ambiente deve ser executável localmente.

Não depender do Visual Studio para executar a infraestrutura.

---

# 34. TESTES

Criar:

## Unit Tests

Testar todas as regras de negócio importantes.

Principalmente:

* lançamentos positivos
* lançamentos negativos
* lançamento zero inválido
* autorização de conta
* consolidação
* idempotência
* recuperação de falhas
* gaps
* limite de tentativas

## Architecture Tests

Utilizar ArchUnitNET ou alternativa equivalente.

Validar:

* Domain não depende de Infrastructure
* Domain não depende de ASP.NET
* Application não depende de Infrastructure
* regras de dependência da Clean Architecture

## Integration Tests

Testar:

* APIs
* PostgreSQL
* Kafka
* autenticação
* fluxo de consolidação

## End-to-End

Testar o fluxo:

```text
Login
  ↓
Criar lançamento
  ↓
Evento Kafka
  ↓
Consolidação
  ↓
Persistência
  ↓
Consulta do saldo
```

## Load Test

Criar teste capaz de demonstrar pelo menos:

```text
50 RPS
```

na API de relatórios.

Documentar resultado.

---

# 35. C4 MODEL

Criar documentação C4 Model contendo:

## Context

Sistema e atores externos.

## Container

* Gateway
* Auth API
* Financeiro API
* Relatórios API
* Consolidation Worker
* Kafka
* PostgreSQL

## Component

Detalhar pelo menos os principais componentes de:

* Financeiro
* Relatórios
* Consolidação

## Deployment

Mostrar:

* Gateway
* duas instâncias das APIs
* Kafka
* PostgreSQL
* Worker
* observabilidade

Os diagramas devem ser armazenados em:

```text
docs/c4/
```

Preferencialmente utilizar Mermaid para facilitar manutenção no GitHub.

---

# 36. ADRs

Criar Architecture Decision Records para decisões importantes, incluindo:

* Clean Architecture
* modularização
* Kafka
* MassTransit
* PostgreSQL
* YARP
* estratégia de escalabilidade
* resiliência
* autenticação JWT
* observabilidade
* idempotência
* estratégia de consolidação
* connection pool

---

# 37. README

Criar README completo contendo:

* visão geral
* arquitetura
* requisitos
* tecnologias
* pré-requisitos
* instalação
* configuração
* execução local
* Docker
* banco de dados
* Kafka
* KafkaDrop
* Aspire Dashboard
* autenticação
* Swagger
* testes
* testes de carga
* observabilidade
* troubleshooting

Incluir os diagramas arquiteturais.

---

# 38. QUALIDADE DE CÓDIGO

Aplicar:

* SOLID
* Clean Code
* Dependency Injection
* async/await
* CancellationToken
* nullable reference types
* tratamento adequado de exceções
* validação de entrada
* logging estruturado
* idempotência
* nomes claros
* classes pequenas
* métodos coesos

Evitar:

* God Classes
* God Methods
* static state
* Service Locator
* repositories genéricos sem justificativa
* lógica de negócio dentro dos controllers
* chamadas diretas ao DbContext a partir de controllers
* secrets hardcoded
* duplicação desnecessária
* comentários que apenas repetem o código

---

# 39. PROCESSO DE IMPLEMENTAÇÃO

NÃO tente criar todo o projeto de uma única vez.

Execute em fases.

## FASE 1 — Arquitetura

Primeiro:

1. ler `REQUISITOS.md`;
2. analisar esta especificação;
3. analisar o projeto de referência;
4. criar `docs/architecture.md`;
5. criar diagramas C4;
6. criar ADRs principais.

Nesta fase NÃO implementar a lógica completa.

---

## FASE 2 — Estrutura

Criar:

* Solution
* projetos
* referências
* estrutura de pastas
* configuração básica

Executar:

```bash
dotnet build
```

Corrigir todos os erros.

---

## FASE 3 — Domain/Application

Implementar:

* entidades
* value objects quando necessários
* regras de negócio
* commands
* queries
* handlers
* interfaces

Executar testes unitários.

---

## FASE 4 — Infrastructure

Implementar:

* EF Core
* PostgreSQL
* migrations
* repositories
* seeds
* Kafka
* MassTransit
* Refit
* Polly
* Serilog
* OpenTelemetry

Executar testes.

---

## FASE 5 — APIs

Implementar:

* Auth
* Financeiro
* Relatórios
* JWT
* autorização
* Swagger
* validações

Executar testes de integração.

---

## FASE 6 — Workers

Implementar:

* consumidores Kafka
* consolidação
* retry
* recuperação
* gaps
* idempotência

Executar testes.

---

## FASE 7 — Infraestrutura local

Implementar:

* Dockerfiles
* Docker Compose
* Kafka
* KafkaDrop
* PostgreSQL
* Aspire Dashboard
* YARP
* múltiplas instâncias

Testar ambiente completo.

---

## FASE 8 — Performance

Executar teste de carga.

Validar:

```text
50 RPS
```

Registrar:

* throughput
* latência
* erros
* uso de CPU
* uso de memória
* comportamento do PostgreSQL
* comportamento do Kafka

---

## FASE 9 — FINALIZAÇÃO

Executar:

```bash
dotnet restore
dotnet build
dotnet test
```

Corrigir todos os erros.

Depois revisar:

* segurança
* arquitetura
* testes
* documentação
* Docker
* logs
* observabilidade
* Swagger

---

# 40. REGRA FUNDAMENTAL

Não considere uma tarefa concluída apenas porque o código foi criado.

Uma tarefa só está concluída quando:

1. código compila;
2. testes passam;
3. arquitetura está coerente;
4. documentação está atualizada;
5. não existem secrets hardcoded;
6. comportamento está alinhado aos requisitos;
7. solução pode ser executada localmente.

Sempre que encontrar uma decisão arquitetural relevante, documente-a.

Não esconda trade-offs.

Quando uma decisão for uma interpretação sua e não um requisito explícito da prova, deixe isso claro na documentação.

# OBJETIVO FINAL

Entregar uma solução profissional chamada:

`LeoVinciFinance`

que demonstre domínio de:

* Clean Architecture
* arquitetura modular
* C#
* .NET 10
* PostgreSQL
* Kafka
* MassTransit
* resiliência
* escalabilidade horizontal
* segurança
* JWT
* observabilidade
* testes
* Docker
* C4
* documentação arquitetural

A solução deve ser suficientemente clara para que outro arquiteto consiga abrir o repositório e entender:

**por que cada decisão arquitetural foi tomada, como o sistema funciona, como executá-lo e como validar seus requisitos não funcionais.**
