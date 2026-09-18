# LeoVinciFinance

Sistema de controle financeiro modular (Auth, Financeiro, Relatórios, Consolidação) construído
em .NET 10 / Clean Architecture, conforme a Especificação Mestre fornecida.

> \\\\\\\*\\\\\\\*Sobre esta revisão (round 1 de ajustes)\\\\\\\*\\\\\\\*: este código foi escrito por um assistente de IA
> sem acesso a um SDK .NET completo nem à internet no ambiente onde a maior parte dele foi
> gerada. A primeira entrega compilou com uma série de erros reais (relacionados no arquivo
> `docs/prompts/Ajustes.round1.md`), que foram corrigidos nesta revisão — incluindo, mais
> importante, uma correção \\\\\\\*\\\\\\\*arquitetural\\\\\\\*\\\\\\\*: o desenho original do módulo de Consolidação
> estava incorreto (Financeiro publicava um evento Kafka que a Especificação Mestre e a
> ADR 0011 explicitamente proíbem). O desenho atual segue `docs/architecture/architecture.md`
> e `docs/c4/component.md`. Ainda assim, \\\\\\\*\\\\\\\*recomenda-se fortemente rodar `dotnet restore` e
> `dotnet build` localmente antes de considerar o código pronto\\\\\\\*\\\\\\\* — não há garantia de que
> todas as versões de pacote e toda a sintaxe de bibliotecas de terceiros (MassTransit, YARP,
> ArchUnitNET) estejam 100% corretas sem uma compilação real.

## Pré-requisitos

* .NET SDK 10.0
* Docker + Docker Compose
* (Opcional, só para os testes de integração) Docker disponível para Testcontainers

## Estrutura da solution

```
src/
  BuildingBlocks/
    BuildingBlocks.Common        # Entity/DomainException, CQRS (ICommand/IQuery/Result), IUnitOfWork
    BuildingBlocks.WebHost       # JWT bearer, Swagger, Serilog + OpenTelemetry (uso exclusivo das \\\\\\\*.Api)
    BuildingBlocks.ServiceAuth   # Token de conta de serviço + políticas Polly (uso das \\\\\\\*.Infrastructure que chamam outros módulos)
  Auth/                          # Domain, Application, Infrastructure, Api
  Financeiro/                    # Domain, Application, Infrastructure, Api — NÃO usa Kafka (ver ADR 0011)
  Relatorios/                    # Domain, Application, Infrastructure, Api
  Consolidacao/                  # Domain (eventos: SaldoDiarioConsolidadoIniciado/Concluido/ComFalhas), Application, Infrastructure, BackgroundServices (Worker)
  Gateway/                       # Gateway.Api (YARP) — 2 nós para Financeiro e Relatórios
tests/
  UnitTests/                     # \\\\\\\*.Domain.Tests por módulo
  ArchitectureTests/             # ArchUnitNET — valida as regras de dependência entre camadas/módulos
  IntegrationTests/              # Financeiro.Api.IntegrationTests (Testcontainers + Postgres real)
  LoadTests/                     # RelatoriosLoadTest (NBomber)
docker/                          # Dockerfiles de cada serviço + init-schemas.sql
docker-compose.yml
LeoVinciFinance.sln
```

**Removido nesta revisão**: o projeto `BuildingBlocks.IntegrationEvents` (existia só para o
evento `LancamentoCriadoIntegrationEvent`, que não deveria existir — ver seção "O que mudou
arquiteturalmente" abaixo).

## O que mudou arquiteturalmente no round 1

1. **Financeiro não publica mais nenhum evento.** A primeira versão fazia `Financeiro.Api`
publicar `LancamentoCriadoIntegrationEvent` no Kafka após cada lançamento, e a Consolidação
consumia esse evento. Isso violava a ADR 0011 ("a disponibilidade de Financeiro nunca pode
depender de outro módulo ou de infraestrutura de mensageria"). Agora Financeiro é uma API
puramente request/response, sem MassTransit/Kafka — mais simples e mais alinhada à
Especificação Mestre.
2. **Consolidação foi redesenhada para persistir eventos por tabela (uma tabela por evento)**
(`docs/c4/component.md`): agora o domínio possui três tabelas de evento internas ao próprio
módulo — `SaldoDiarioConsolidadoIniciado`, `SaldoDiarioConsolidadoConcluido` e
`SaldoDiarioConsolidadoComFalhas` — em vez do modelo anterior baseado em agregados `Job`/
`Etapa`/`Execução`. Cada evento tem um CorrelationId para rastreamento ponta a ponta; os
produtor e consumidores desses eventos são o mesmo módulo (`Consolidacao.Infrastructure`),
portanto o contrato de serialização (DTOs em `Consolidacao.Application.Events`) fica local ao
módulo.
3. **4 workers dentro de `Consolidacao.BackgroundServices`**:

   * `SaldoDiarioConsolidadoAgendadorHostedService` — todo dia, inicia a consolidação de D-1
para todas as contas.
   * `SaldoDiarioConsolidadoRetryHostedService` — periodicamente, reenvia Jobs que falharam e
ainda não excederam `Consolidacao:LimiteTentativas`.
   * `SaldoDiarioConsolidadoLacunasHostedService` — periodicamente, detecta datas sem
consolidação bem-sucedida (ex.: o worker ficou fora do ar num dia) e as reprocessa.
   * O barramento MassTransit (3 consumidores Kafka) roda como 4º "worker" do processo,
iniciado automaticamente pelo `AddMassTransit`/`AddRider`.

   Chave de partição Kafka = `"{IdConta}|{Data:yyyyMMdd}"` — garante que os 3 eventos de uma
mesma consolidação caiam sempre na mesma partição e sejam processados em ordem.

4. **Eliminada a duplicação de código entre Relatorios.Infrastructure e
Consolidacao.Infrastructure** (apontada nos Ajustes round 1): `IServiceTokenProvider`,
cliente Refit de `Auth.Api` e as políticas Polly (retry/circuit breaker/timeout) agora vivem
uma única vez em `BuildingBlocks.ServiceAuth`.
5. **Conta de serviço não faz mais login a cada chamada.** `ServiceAccount:Token` guarda um
token de longa duração; o provider só o *valida* via `POST /api/auth/token` a cada uso
(cacheado por `ServiceAccount:CacheMinutos`). Login com `ServiceAccount:Senha` só acontece
como fallback, se o token estiver ausente/expirado.
6. **`Conta.CriarAvulsa` foi removida** do domínio de Financeiro — uma Conta só pode nascer de
`Cliente.AbrirConta()`, reforçando de fato a invariante "uma conta só existe se tiver um
cliente".
7. **Gateway com 2 nós para Financeiro e Relatórios** (Especificação Mestre, seção 7/38: "cada
API HTTP deverá possuir duas instâncias"). Auth.Api permanece com 1 instância nesta fase.
Relatórios e Consolidação agora chamam Financeiro/Relatórios **através do Gateway**
(`http://gateway-api:8080`), não direto num nó específico — assim o round-robin do YARP
também vale para o tráfego interno entre serviços.
8. **Nenhum "magic number"/string solta**: limites de tentativas, intervalos dos workers,
parâmetros de rate limiting, timeouts/retries do Polly e mensagens de validação do
Financeiro agora vêm de `appsettings` ou de classes de constantes (`FinanceiroMensagens`,
`ConsolidacaoOptions`, `ResilienceOptions` etc.).

## Correções de pacotes NuGet (causavam falha de `dotnet restore`/`dotnet build`)

|Pacote|Antes|Depois|Motivo|
|-|-|-|-|
|`ArchUnitNET.xUnit`|0.11.0|**`TngTech.ArchUnitNET.xUnit` 0.13.1**|O nome do pacote no NuGet está errado na v1 — o ID correto tem o prefixo `TngTech.`|
|`Npgsql.EntityFrameworkCore.PostgreSQL`|9.0.4|**10.0.3**|9.0.4 exige EF Core `<10.0.0`, conflitando com `Microsoft.EntityFrameworkCore 10.0.0`. A 10.0.3 já suporta EF Core 10/.NET 10|
|`NBomber` / `NBomber.Http`|5.8.0 / 5.8.0|**6.2.0 / 6.1.0**|Versões antigas resolviam `NBomber.Contracts` em versões incompatíveis entre si|
|`Refit` / `Refit.HttpClientFactory`|7.2.1|**8.0.0**|Corrige vulnerabilidade conhecida (GHSA-3hxg-fxwm-8gf7)|
|`OpenTelemetry.\\\\\\\*`|1.10.x|**1.11.x**|Corrige vulnerabilidades conhecidas em `OpenTelemetry.Api`/`Exporter.OpenTelemetryProtocol`|
|`System.Security.Cryptography.Xml`|(transitivo, 9.0.0)|**override direto para 10.0.0**|Dependência transitiva vulnerável trazida por `System.IdentityModel.Tokens.Jwt`|
|`SSH.NET`|(transitivo)|**override direto para 2026.0.0**|Dependência transitiva vulnerável trazida por `Testcontainers`|



## Primeira execução (local, sem Docker)

1. Suba um PostgreSQL local (ou `docker compose up postgres -d`). No diretório do projeto.
2. Gere e aplique as migrations de cada módulo:

```bash
   dotnet tool install --global dotnet-ef   # se ainda não tiver

  foreach ($m in "Auth","Financeiro","Relatorios","Consolidacao") { $infra="src/$m/$m.Infrastructure"; dotnet ef migrations add InicialCreate --project "$infra/$m.Infrastructure.csproj" --startup-project "$infra/$m.Infrastructure.csproj"; if ($LASTEXITCODE -ne 0) { break }; dotnet ef database update --project "$infra/$m.Infrastructure.csproj" --startup-project "$infra/$m.Infrastructure.csproj"; if ($LASTEXITCODE -ne 0) { break } } 

   ```

Nota: Durante a execução das migrations, pode ser exibida uma mensagem de erro semelhante à apresentada abaixo:



```bash

Failed executing DbCommand (28ms) \[Parameters=\[], CommandType='Text', CommandTimeout='30']
SELECT "MigrationId", "ProductVersion"
FROM relatorios.\_\_ef\_migrations\_history
ORDER BY "MigrationId";

   ```

Esse comportamento está relacionado a um bug conhecido do Entity Framework (EF) ao processar migrations em múltiplos schemas. A mensagem ocorre durante a etapa de verificação do histórico de migrations e não impede a criação ou execução normal da migration.

Portanto, caso essa mensagem seja exibida nesse contexto, ela pode ser desconsiderada, desde que a migration seja posteriormente criada e aplicada normalmente.



3. `appsettings.Development.json` de cada `\\\\\\\*.Api`/Worker já vem com uma chave JWT e credenciais
de desenvolvimento — **nunca usar esses valores em produção**.
4. Rode cada serviço em terminais separados:

```bash
   dotnet run --project src/Auth/Auth.Api                    # porta 5001
   dotnet run --project src/Financeiro/Financeiro.Api         # porta 5002
   dotnet run --project src/Financeiro/Financeiro.Api --urls http://localhost:5012  # 2o no (opcional)
   dotnet run --project src/Relatorios/Relatorios.Api         # porta 5003
   dotnet run --project src/Relatorios/Relatorios.Api --urls http://localhost:5013  # 2o no (opcional)
   dotnet run --project src/Consolidacao/Consolidacao.BackgroundServices
   dotnet run --project src/Gateway/Gateway.Api                # porta 5000 (entrada unica)
   ```

## Execução via Docker Compose

```bash
cp .env.example .env   # preencha JWT\\\\\\\_KEY e SERVICE\\\\\\\_ACCOUNT\\\\\\\_SENHA (SERVICE\\\\\\\_ACCOUNT\\\\\\\_TOKEN pode ficar vazio)
docker compose up --build
```

Serviços expostos:

|Serviço|Porta(s)|Descrição|
|-|-|-|
|gateway-api|5000|**Ponto único de entrada** (YARP, round-robin para Financeiro/Relatórios)|
|auth-api|5001|1 instância|
|financeiro-api-1 / -2|5002 / 5012|2 instâncias atrás do Gateway|
|relatorios-api-1 / -2|5003 / 5013|2 instâncias atrás do Gateway|
|aspire-dashboard|18888|UI de observabilidade (traces OTLP)|
|kafdrop|9000|Inspeção visual dos tópicos/mensagens Kafka|
|postgres|5432|—|
|kafka|9092|—|
|redis|6379|—|

As migrations **não** rodam automaticamente no `docker compose up` — aplique-as manualmente
antes do primeiro uso (aponte a connection string para `localhost:5432`).



```bash
   dotnet tool install --global dotnet-ef   # se ainda não tiver

  foreach ($m in "Auth","Financeiro","Relatorios","Consolidacao") { $infra="src/$m/$m.Infrastructure"; dotnet ef migrations add InicialCreate --project "$infra/$m.Infrastructure.csproj" --startup-project "$infra/$m.Infrastructure.csproj"; if ($LASTEXITCODE -ne 0) { break }; dotnet ef database update --project "$infra/$m.Infrastructure.csproj" --startup-project "$infra/$m.Infrastructure.csproj"; if ($LASTEXITCODE -ne 0) { break } }

   ```

Nota: Durante a execução das migrations, pode ser exibida uma mensagem de erro semelhante à apresentada abaixo:



```bash

Failed executing DbCommand (28ms) \[Parameters=\[], CommandType='Text', CommandTimeout='30']
SELECT "MigrationId", "ProductVersion"
FROM relatorios.\_\_ef\_migrations\_history
ORDER BY "MigrationId";

   ```

Esse comportamento está relacionado a um bug conhecido do Entity Framework (EF) ao processar migrations em múltiplos schemas. A mensagem ocorre durante a etapa de verificação do histórico de migrations e não impede a criação ou execução normal da migration.

Portanto, caso essa mensagem seja exibida nesse contexto, ela pode ser desconsiderada, desde que a migration seja posteriormente criada e aplicada normalmente.



## Conta de serviço (Relatorios.Api e Consolidacao.BackgroundServices)

Essas duas partes do sistema chamam outros módulos internamente como o usuário
`service-consolidacao` (perfil Admin). Em vez de fazer login a cada chamada, configure um
token de longa duração:

```bash
# 1. Suba ao menos Auth.Api e Postgres, com as migrations aplicadas
curl -X POST http://localhost:5001/api/auth/login -H "Content-Type: application/json" -d '{"username":"service-consolidacao","senha":"ServiceConsolidacao@123"}'

# 2. Copie o "token" da resposta para SERVICE\\\\\\\_ACCOUNT\\\\\\\_TOKEN no .env (ou appsettings.Development.json)
# 3. Reinicie relatorios-api-\\\\\\\*/consolidacao-worker
```

Se você não configurar `SERVICE\\\\\\\_ACCOUNT\\\\\\\_TOKEN`, o sistema funciona do mesmo jeito: cada
chamada valida o token vazio (falha), cai no fallback de login com `ServiceAccount:Senha`, e
loga um aviso sugerindo configurar o token. Funcionalmente idêntico, só um pouco mais lento.

## Usuários de desenvolvimento (seed)

|Username|Senha|Perfil|Uso|
|-|-|-|-|
|`admin`|`Admin@123`|Admin|Acesso administrativo completo|
|`comerciante`|`Comerciante@123`|Comerciante|Fluxo normal de lançamentos|
|`service-consolidacao`|`ServiceConsolidacao@123`|Admin|Conta de serviço (Relatorios.Api e Consolidacao.BackgroundServices)|

## Clientes e contas de desenvolvimento (HasData)

O `FinanceiroDbContext` utiliza `HasData` para gerar, por meio da migration do Entity
Framework, 3 clientes com 2 contas cada. Cada cliente possui um `IdUsuario` definido:

|Cliente|IdUsuario|Contas|
|-|-|-|
|Cliente Seed 1|`1`|2 contas|
|Cliente Seed 2|`2`|2 contas|
|Cliente Seed 3|`3`|2 contas|

Os IDs dos clientes e das contas são determinísticos. Portanto, as contas podem ser
consultadas após a aplicação da migration e utilizadas nos testes manuais. Esses dados não
são criados por um seed executado em runtime.

## Fluxo básico de teste manual

```bash
# 1. Login
curl -X POST http://localhost:5000/api/auth/login \\\\\\\\
  -H "Content-Type: application/json" \\\\\\\\
  -d '{"username":"comerciante","senha":"Comerciante@123"}'

# 2. Criar um lançamento (substitua TOKEN e IDCONTA)
curl -X POST http://localhost:5000/api/financeiro/lancamentos \\\\\\\\
  -H "Content-Type: application/json" \\\\\\\\
  -H "Authorization: Bearer TOKEN" \\\\\\\\
  -d '{"idConta":"IDCONTA","valor":150.75}'

# 3. A consolidacao de HOJE so roda no proximo ciclo do agendador diario (ou do worker de
#    lacunas). Em desenvolvimento, appsettings.Development.json do worker usa intervalos bem
#    mais curtos (5-10 min) para nao precisar esperar 24h. Depois, consultar o saldo:
curl "http://localhost:5000/api/relatorios/saldo-diario-consolidado?idConta=IDCONTA\\\\\\\&data=2026-09-14" \\\\\\\\
  -H "Authorization: Bearer TOKEN"
```

> Não há endpoint para criar `Cliente`/`Conta` na Especificação Mestre original. Para os testes
> manuais, utilize as contas geradas pelo `HasData` após aplicar as migrations.

## Testes

```bash
dotnet test tests/UnitTests/Auth.Domain.Tests
dotnet test tests/UnitTests/Financeiro.Domain.Tests
dotnet test tests/UnitTests/Relatorios.Domain.Tests
dotnet test tests/UnitTests/Consolidacao.Domain.Tests     # agora cobre Job/Etapa/Execucao
dotnet test tests/ArchitectureTests                       # inclui a regra da ADR 0011 (Financeiro x MassTransit)
dotnet test tests/IntegrationTests/Financeiro.Api.IntegrationTests   # exige Docker (Testcontainers) - nao depende mais de Kafka
```

Teste de carga (requer o ambiente completo rodando e um token válido):

```bash
export LOAD\\\\\\\_TEST\\\\\\\_TOKEN="<token>"
export LOAD\\\\\\\_TEST\\\\\\\_ID\\\\\\\_CONTA="<guid>"
dotnet run -c Release --project tests/LoadTests/RelatoriosLoadTest
```

## Decisões e trade-offs registrados (não literais na Especificação Mestre)

1. **IdConta no JWT**: o claim `idConta` é nulo no login (Auth não conhece o schema
Financeiro); autorização por conta é feita consultando titularidade nos módulos
Financeiro/Relatórios.
2. **`GET /api/financeiro/contas`** (sem filtro de usuário): usado pelos hosted services de
Consolidação para listar todas as contas do sistema (agendador diário e detector de
lacunas).
3. **`GET /api/relatorios/saldo-diario-consolidado` consultado antes de recalcular**: usado por
`ProcessarIniciadoCommandHandler` para checar se aquela conta/data já foi consolidada
(idempotência de sistema, além da idempotência do próprio consumidor Kafka).
4. **Saldo consolidado igual a zero**: rejeitado pelo Domain (mesma regra do Lançamento). A
Consolidação trata esse caso como "concluído sem publicar", nunca como falha.
5. **Alerta operacional ao esgotar tentativas**: sem canal de notificação externo definido na
Especificação Mestre — implementado como `LogCritical` estruturado, capturável por qualquer
ferramenta de observabilidade ligada ao Aspire Dashboard.
6. **Relatórios/Consolidação chamam os outros módulos através do Gateway**, não direto num nó
específico — evita que cada chamador precise conhecer/balancear as 2 instâncias de
Financeiro/Relatórios individualmente.

## Limitações conhecidas / próximos passos

* Nenhuma migration EF Core foi gerada (requer SDK real) — ver seção "Primeira execução".
* Não há outbox transacional em nenhum publish de evento; a defesa contra perda de mensagem é
o worker de retry + o worker de detecção de lacunas (idempotentes ponta a ponta).
* Versões de pacotes NuGet foram corrigidas com base em pesquisa (não em um `dotnet restore`
real) — confirme disponibilidade/versões mais recentes ao rodar pela primeira vez.
* Endpoint de criação de `Cliente`/`Conta` não existe (fora do escopo literal da Especificação
Mestre) — necessário para popular dados de teste manualmente via banco.
* `BuildingBlocks.ServiceAuth` reaproveita o mesmo mecanismo de login de usuário para a conta
de serviço; uma implementação de client-credentials/OAuth2 real seria mais robusta, mas
ficaria fora do desenho de Auth já definido (usuário + senha + perfil).

