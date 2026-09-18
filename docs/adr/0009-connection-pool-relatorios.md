# ADR 0009 — Dimensionamento do Connection Pool (Relatórios)

## Status

Proposta 

## Contexto

A API de Relatórios precisa suportar 50 RPS de pico com até 5% de perda, rodando em 2 réplicas, compartilhando uma instância PostgreSQL cujo número de conexões é finito.

## Raciocínio (estimativa inicial, a validar com teste de carga)

Variáveis consideradas:

* **Réplicas**: 2 instâncias de `Relatorios.Api`.
* **RPS alvo por réplica**: \~30 RPS (50 RPS distribuídos de forma não perfeitamente uniforme entre 2 réplicas, com margem).
* **Latência média esperada por consulta** (leitura simples com `AsNoTracking()`, endpoint cacheado por 1 dia por conta, o que reduz pressão real no banco após o primeiro acesso): estimada em até \~50ms sob carga.
* **Lei de Little (aplicada como estimativa, não como garantia)**: conexões simultâneas necessárias ≈ RPS × latência média. Para 30 RPS × 0,05s ≈ 1,5 conexão simultânea "em uso" por réplica em regime estável — a isso soma-se margem para picos e para o efeito do cache reduzir, mas não eliminar, o tráfego de escrita/consulta ao criar consolidados.
* **Margem de segurança**: aplicado fator de \~5x sobre a estimativa teórica para absorver variância de latência e picos não perfeitamente distribuídos.

## Decisão (ponto de partida, sujeito a ajuste)

* Pool inicial de conexões por réplica de `Relatorios.Api`: **20** (Npgsql `Maximum Pool Size=20`), totalizando até 40 conexões simultâneas das duas réplicas para o schema `relatorios`.
* Limite de conexões do PostgreSQL (`max\_connections`) configurado com folga suficiente para acomodar os quatro módulos (auth, financeiro, relatorios, consolidacao) sem que Relatórios, sozinho, esgote o limite do servidor.
* Este número **não é definitivo**: será validado (e ajustado se necessário) no teste de carga, com throughput, latência e erros medidos e documentados.

## Consequências

* Ganho: ponto de partida justificado matematicamente, não arbitrário.
* Risco assumido: estimativa de latência é teórica nesta fase (sem código ainda) — tratada explicitamente como hipótese a confirmar, não como conclusão.

