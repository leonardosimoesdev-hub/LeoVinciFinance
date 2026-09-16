# ADR 0001 — Clean Architecture + Arquitetura Modular Distribuída

## Status

Aceita

## Contexto

A prova técnica exige uma solução organizada como "obra de arquitetura de software", com Clean Architecture, separação de responsabilidades e modularidade, referenciando estruturalmente `https://github.com/leonardopinto/clean-arc-example` (Domain/Application/Infrastructure/Web-API).

## Decisão

* Adotar Clean Architecture (regra de dependência clássica de Robert C. Martin) dentro de cada módulo de negócio (`Auth`, `Financeiro`, `Relatorios`), com camadas `Domain → Application → Infrastructure/Api`.
* Adotar arquitetura modular distribuída no nível macro: cada módulo é implantável e escalável de forma independente, comunicando-se via HTTP (síncrono) e Kafka/MassTransit (assíncrono), atrás de um Gateway YARP.
* O módulo `Consolidacao` segue o mesmo princípio de dependência, substituindo a camada `Api` por `BackgroundServices` (não expõe HTTP).

## Consequências

* Ganho: baixo acoplamento entre regras de negócio e frameworks; testabilidade alta do Domain/Application.
* Custo: mais projetos/assemblies por módulo (4 por módulo de API, 3 para Consolidação) — aceito conscientemente pela clareza de fronteiras.

