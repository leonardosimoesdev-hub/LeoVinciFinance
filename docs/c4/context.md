# C4 — Nível 1: Contexto

```mermaid
C4Context

&#x20;   title LeoVinciFinance — Diagrama de Contexto



&#x20;   Person(admin, "Administrador", "Gerencia clientes, contas e consolidações")

&#x20;   Person(comerciante, "Comerciante", "Registra lançamentos e consulta saldos da própria conta")



&#x20;   System(leovinci, "LeoVinciFinance", "Plataforma de controle financeiro com consolidação diária de saldos")



&#x20;   System\_Ext(consumidorApi, "Cliente HTTP externo", "Qualquer client que consome as APIs via Gateway (ex.: Swagger, front-end futuro)")



&#x20;   Rel(admin, leovinci, "Autentica-se, gerencia contas, consulta lançamentos e relatórios")

&#x20;   Rel(comerciante, leovinci, "Autentica-se, registra lançamentos, consulta saldo consolidado")

&#x20;   Rel(consumidorApi, leovinci, "Requisições HTTP/JSON via Gateway", "HTTPS/JWT")

```

## Notas

* Não há integrações com sistemas externos de terceiros nesta fase (ex.: bancos, ERPs)
* Todo acesso externo passa pelo Gateway YARP (ver `container.md`).

