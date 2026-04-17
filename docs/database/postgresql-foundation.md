# PostgreSQL foundation

## Status
- Stage: Sprint 0 / S0-07
- Scope: PostgreSQL foundation and migrations discipline
- Data access strategy: EF Core 10 + Npgsql provider

## Baseline decision
- Primary relational database: PostgreSQL
- ORM / migrations: Entity Framework Core
- PostgreSQL provider: Npgsql.EntityFrameworkCore.PostgreSQL
- Migrations live in BuildingBlocks.Infrastructure
- Startup project for local EF commands: App.Api

## Default local development database
- host: localhost
- port: 5432
- database: analytics_automation_dev
- app role: analytics_automation_app_dev
- password file: %LOCALAPPDATA%\AnalyticsAutomation-Core\local-dev\postgres-app-password.txt

## Schema and naming rules
- default schema: app
- tables: snake_case plural nouns where practical
- primary key column: id
- foreign key columns: <entity>_id
- indexes: ix_<table>_<column_list>
- unique indexes: ux_<table>_<column_list>
- migration history table: app.__ef_migrations_history

## Migration discipline
- additive-first only
- destructive cleanup is always delayed to a later explicit step
- one migration must have a clear purpose and a documented rollback thought process
- no silent rename/removal of columns or tables in the same step that introduces new behavior

## First migration
- name: InitialPostgresqlFoundation
- purpose:
  - create schema app
  - create table app.database_bootstrap_markers
  - seed marker code initial_postgresql_foundation