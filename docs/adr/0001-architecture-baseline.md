# ADR 0001 — Architecture Baseline

- Status: Accepted
- Date: 2026-04-15

## Context
Проекту нужен ранний и однозначный архитектурный baseline.
Без него команда быстро расходится по форме solution, границам модулей, способу поставки и степени связности между частями системы.

## Decision
1. Репозиторий ведется как один monorepo.
2. Архитектурная форма проекта — modular monolith.
3. Core-бизнес реализуется как compiled-in modules.
4. Runtime plugins допустимы только точечно:
   - форматы отчетов
   - рендереры
   - редкие интеграции
   - import/export adapters
5. Серверная часть состоит из:
   - ASP.NET Core API
   - Worker
   - PostgreSQL
   - Nginx
6. Для commodity layers применяется reuse-first подход на стандартных зрелых механизмах .NET.
7. У каждого модуля должны быть:
   - свои contracts
   - свои options
   - свои feature flags
   - свои events
   - свои tests
   - свои observability hooks
8. Миграции БД ведутся по additive-first discipline.
9. Mobile API развивается обратно совместимо, если явно не согласовано иное.
10. Переход к микросервисной архитектуре без отдельного решения запрещен.

## Rationale
- Modular monolith дает понятные границы без раннего операционного усложнения.
- Compiled-in modules подходят под текущий состав команды и Sprint 0 / Sprint 1.
- Reuse-first снижает риск писать нестабильную инфраструктурную основу с нуля.
- Additive-first migrations и backward compatibility защищают mobile/web интеграцию.

## Consequences
- Структура solution и каталогов должна отражать modular monolith.
- Shared contracts и module boundaries фиксируются рано и меняются только контролируемо.
- Любые изменения в архитектурной форме требуют отдельного coordination decision.
- В Sprint 0 приоритет у repo/process/platform foundation, а не у расширения доменной поверхности.

## Non-goals
- Декомпозиция на микросервисы.
- Контейнеризация ради контейнеризации.
- Детальный production hardening в рамках этого ADR.
- Финальная UI-архитектура экранов web/mobile.