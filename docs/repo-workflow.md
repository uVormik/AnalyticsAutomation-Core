# Repository Workflow

## Назначение
Этот документ фиксирует рабочий процесс репозитория на Sprint 0 и Sprint 1.
Цель — держать `main` в управляемом состоянии и не допускать скрытых breaking changes.

## Базовый принцип
- одна задача = один понятный working increment
- короткие ветки
- merge только через PR
- `main` всегда должна быть потенциально пригодна к релизу

## Ветки
### Default branch
- `main`

### Разрешенные short-lived branches
- `feature/<scope>-<short-name>`
- `fix/<scope>-<short-name>`
- `hotfix/<short-name>`
- `chore/infra-<short-name>`
- `docs/<short-name>`

### Что запрещено
- direct push в `main`
- long-lived `develop`
- unrelated refactoring внутри task PR
- смешивание нескольких крупных задач в одном PR

## Как начинается новая работа
Любая новая задача сначала получает короткую карточку.
Минимум:
- цель
- модуль
- владелец
- contracts impact
- migration
- feature flag
- events
- offline behavior
- observability
- rollback

## Pull Request policy
Каждый PR обязан содержать:
- цель изменения
- модуль / область
- затронутые contracts
- есть ли миграция
- есть ли feature flag
- offline behavior, если он меняется
- audit / observability impact
- как проверить вручную
- какие тесты добавлены или обновлены
- как откатить
- есть ли breaking change и нужен ли ADR update

## Review policy
### Обязательный минимум
- минимум один approval обязателен
- shared/contracts/infra changes нельзя мерджить без контролируемого review

### Ownership по областям
- Platform owner:
  - `App.Api`
  - `App.Worker`
  - `BuildingBlocks/*`
  - `Modules/*`
  - `infra/*`
  - `.github/workflows/*`
  - migrations
  - auth / group tree / devices / sync / duplicate / fraud / incidents
  - `docs/adr/*`
- Web owner:
  - `App.Web`
  - `App.UI.Shared`
- Mobile owner:
  - `App.Mobile.Android`

### Отдельное правило
Любое изменение shared DTO, sync states, duplicate statuses, auth/session contracts или group tree contracts:
- сначала формулируется явно
- затем идет через PR
- затем сообщается другим кодерам как shared change

## Merge policy
- merge в `main` только через PR
- squash merge — default
- PR должен быть узким и завершенным
- hidden TODO не считаются завершением
- breaking change должен быть явно помечен
- после появления CI в S0-04 красный pipeline блокирует merge

## Labels
Минимальный набор labels:
- `backend`
- `web`
- `android`
- `infra`
- `bug`
- `feature`
- `tech-debt`
- `blocked`
- `incident`

## Release note baseline
- релизные точки отмечаются tag-ами
- production deploy не выполняется автоматически без явного решения владельца проекта
- rollback path должен быть задокументирован заранее

## Что считается готовым к merge
Изменение готово к merge, когда:
- scope понятен
- PR оформлен полностью
- review выполнен
- нет скрытого breaking impact
- docs/ADR обновлены, если это нужно
- после S0-04 required checks зеленые