# AnalyticsAutomation-Core

## Назначение
AnalyticsAutomation-Core — кроссплатформенная система, в которой:
- desktop client = Blazor WebAssembly PWA
- mobile client = Android APK on .NET MAUI Blazor Hybrid
- shared UI = Razor Class Library
- server = ASP.NET Core API + Worker + PostgreSQL + Nginx

Видео-байты не идут через сервер как обязательный прокси.
Сервер работает как control plane / sync plane / audit plane.

## Зафиксированный технологический baseline
- Language/platform: C# / .NET 10
- Desktop: Blazor WebAssembly PWA
- Mobile: .NET MAUI Blazor Hybrid (Android)
- Shared UI: Razor Class Library
- Server: ASP.NET Core API + Worker
- Database: PostgreSQL
- Reverse proxy: Nginx
- XLSX: Open XML SDK
- Background processing: hosted services / worker

## Неизменяемые архитектурные решения
- Architecture form = modular monolith
- Core business = compiled-in modules
- Runtime plugins are allowed only for report formats, renderers, rare integrations, and import/export adapters
- Video upload/download = direct client <-> site
- Server = control plane
- Feature flags are mandatory
- Per-module options are mandatory
- Internal events are mandatory
- DB migrations are additive-first
- Mobile API evolves backward-compatibly unless explicitly approved otherwise
- Module data ownership is mandatory
- New work starts as a task card, not as “just code”

## Что делает сервер
- PreUploadCheck
- CreateDownloadIntent
- UploadReceipt / DownloadReceipt
- sync and reconciliation
- duplicate registry
- fraud suspicion
- incidents and escalation
- audit trail
- reports foundation
- future 1C foundation

## Что сервер не делает
- не является обязательным медиапрокси для upload/download
- не превращается в microservice fleet
- не держит финальную desktop/mobile UI-логику
- не допускает прямые cross-module SQL зависимости

## Основной рабочий vertical slice
desktop upload -> precheck -> direct site upload -> upload receipt -> server sync -> duplicate/fraud incident -> admin review

## Repo workflow summary
- default branch: `main`
- direct push to `main` is forbidden
- merge to `main` only through PR
- short-lived branches only
- squash merge is the default merge strategy
- `main` must stay releasable
- shared/contracts/infra changes require controlled review
- every new task starts as a short task card

See:
- `docs/repo-workflow.md`
- `docs/main-protection-checklist.md`

## ADR index
- `docs/adr/0001-architecture-baseline.md`
- `docs/adr/0002-client-split.md`
- `docs/adr/0003-direct-video-upload-and-sync.md`
- `docs/adr/0004-dedup-and-fraud-policy.md`
- `docs/adr/0005-branching-and-release-policy.md`

## Current project phase
Sprint 0:
- repo/process/platform foundation
- ADR baseline
- GitHub governance
- protected `main`

Sprint 1:
- first working backend vertical slice for upload control plane

## Source of truth
- repository code and docs
- ADR in `docs/adr`
- approved project documents
- GitHub branch / PR / release rules

## Not allowed without explicit coordination
- changing architecture form
- changing shared contracts silently
- breaking mobile API compatibility
- breaking migration discipline
- bypassing feature flags / options / audit expectations