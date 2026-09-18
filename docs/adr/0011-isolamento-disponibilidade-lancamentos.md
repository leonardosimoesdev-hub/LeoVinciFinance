# ADR 0011 — Isolamento de Disponibilidade entre Lançamentos e Consolidação

## Status

Aceita

## Contexto

Há um requisito não funcional explícito e obrigatório: *"O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair."*

## Decisão

* A direção de dependência entre módulos garante isolamento de disponibilidade por construção:

  * `Financeiro.Api` (lançamentos) **não possui nenhuma dependência de tempo de execução** em `Relatorios.Api` ou em `Consolidacao.BackgroundServices` — não faz chamadas HTTP a eles, não publica nem consome eventos Kafka relacionados a eles.
  * A única dependência é no sentido inverso: `Consolidacao` chama `Financeiro.Api` (GET lançamentos) e `Relatorios.Api` (POST saldo consolidado), e `Consolidacao` publica/consome eventos Kafka.
* Consequência direta: se `Relatorios.Api`, `Consolidacao.BackgroundServices` ou o próprio Kafka ficarem indisponíveis, `POST /api/financeiro/lancamentos` continua funcionando normalmente — não há timeout, retry ou circuit breaker no caminho de escrita de lançamentos que dependa desses componentes.
* O que fica temporariamente afetado por uma queda de Relatórios/Consolidação: apenas a consolidação diária (que fica pendente e será recuperada pelos hosted services de retry/gap descritos no ADR 0010) e a consulta de saldo consolidado (não a escrita de lançamentos).
* Reforço arquitetural: `Financeiro.Api` não tem nenhum cliente Refit/Polly apontando para Relatórios ou Consolidação — essa ausência é intencional e deve ser preservada nas fases de implementação (será verificável também via teste de arquitetura, garantindo que o projeto `Financeiro.\*` não referencia clientes HTTP de Relatórios/Consolidação).

## Consequências

* Ganho: atende de forma direta e verificável o requisito não funcional mais crítico da prova técnica.
* Custo: nenhum lançamento é "confirmado como consolidado" em tempo real para o usuário — a consolidação é sempre assíncrona e D-1, o que já era uma decisão e é reforçada, não contradita, por este ADR.

