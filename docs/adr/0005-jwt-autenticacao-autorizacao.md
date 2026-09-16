# ADR 0005 — Autenticação JWT e Autorização por Perfil

## Status

Aceita

## Contexto

Todas as APIs, exceto login, exigem autenticação; a autorização deve considerar os perfis `Admin` e `Comerciante`, validada no serviço responsável pela operação (não apenas no Gateway) — seção 11 do prompt mestre.

## Decisão

* `Auth.Api` emite JWT via `POST /api/auth/login`, contendo `IdUsuario`, `IdConta` (quando aplicável), `Username` e `Perfil` como claims.
* `POST /api/auth/token` valida um token existente.
* Autorização baseada em roles/policies do ASP.NET Core, aplicada em cada API (Financeiro, Relatórios) — o Gateway YARP apenas encaminha a requisição, sem decidir autorização de negócio.
* Regra de posse de conta (usuário só acessa/lança em contas às quais tem relação) validada na camada Application de cada módulo, não confiada ao token isoladamente.

## Consequências

* Ganho: segurança em profundidade — mesmo que o Gateway seja contornado internamente, cada API reforça a própria autorização.
* Custo: alguma duplicação de configuração de autenticação JWT entre APIs — aceito por reforçar o princípio "não implementar segurança apenas no frontend ou Gateway".

