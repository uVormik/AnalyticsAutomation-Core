# ADR 0005 — Branching and Release Policy

- Status: Accepted
- Date: 2026-04-15

## Context
Проекту нужен контролируемый основной поток разработки с первого коммита.
Без этого shared contracts, infra, migrations и backend foundation быстро станут хаотичными.

## Decision
1. Основная и защищенная ветка — `main`.
2. Long-lived `develop` branch не используется.
3. Разрешены только short-lived branches:
   - `feature/<scope>-<short-name>`
   - `fix/<scope>-<short-name>`
   - `hotfix/<short-name>`
   - `chore/infra-<short-name>`
   - `docs/<short-name>`
4. Merge в `main` выполняется только через Pull Request.
5. Squash merge — базовая стратегия merge.
6. `main` должна оставаться в releasable state.
7. Минимум один review обязателен.
8. Изменения в shared contracts, infra, deployment, migrations, auth, group tree, sync logic, App.Api, App.Worker, BuildingBlocks, GitHub Actions и module boundaries требуют review platform owner.
9. Любой breaking change должен быть явно отмечен в PR и, если это baseline decision, в ADR.
10. Релизы отмечаются tag-ами.
11. CI checks становятся required для merge, как только минимальный pipeline будет поднят в S0-04.

## Rationale
- Один защищенный поток проще удерживать в рабочем состоянии.
- Short-lived branches уменьшают конфликтность и скрытую дрейфующую работу.
- Squash merge поддерживает чистую историю и понятный rollback reasoning.
- Явная пометка breaking changes защищает web/mobile интеграцию.

## Consequences
- Нельзя делать direct push в `main`.
- Нельзя прятать архитектурные изменения в случайных PR.
- Любой PR должен быть узким, понятным и проверяемым.
- После появления CI красный pipeline должен блокировать merge.

## Non-goals
- Сложные release trains.
- Автоматический production deploy без явного решения владельца проекта.
- Нефиксируемые в docs ручные правила работы.