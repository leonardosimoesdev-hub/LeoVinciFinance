-- Cria os 4 schemas (um por módulo) antes de qualquer migration rodar — seção 13 da
-- Especificação Mestre ("cada módulo deve possuir seu próprio schema").
-- Redundante com o que o EF Core Npgsql provider já gera automaticamente na migration
-- (CREATE SCHEMA IF NOT EXISTS) quando se usa HasDefaultSchema, mas mantido aqui como
-- segunda camada de garantia, já que este script roda no primeiro boot do container Postgres.

CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS financeiro;
CREATE SCHEMA IF NOT EXISTS relatorios;
CREATE SCHEMA IF NOT EXISTS consolidacao;
