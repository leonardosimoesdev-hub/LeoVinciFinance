# ADR 0004 — PostgreSQL com Schema por Módulo

## Status

Aceita

## Contexto

A prova exige que cada módulo/domínio possua seu próprio schema no PostgreSQL, preservando o isolamento lógico mesmo compartilhando a mesma instância física de banco no ambiente local.

## Decisão

* Um schema por módulo: `auth`, `financeiro`, `relatorios`, `consolidacao`.
* Cada módulo possui seu próprio `DbContext` e suas próprias migrations, aplicadas de forma independente.
* Nenhum módulo acessa diretamente as tabelas de outro schema — toda integração cross-módulo ocorre via API HTTP ou eventos Kafka, nunca via JOIN direto entre schemas.
* Valores monetários: `numeric(18,2)` no banco, `decimal` no Domain; nunca `double`/`float`.

## Consequências

* Ganho: isolamento lógico que simula fronteiras de bounded context mesmo em uma única instância física — facilita uma futura migração para bancos físicos separados por módulo, se necessário.
* Custo: sem transações distribuídas nativas entre módulos — qualquer consistência entre módulos é eventual, via eventos (aceito e coerente com a decisão de mensageria assíncrona do ADR 0003).

