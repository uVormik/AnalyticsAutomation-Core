# ADR 0002 — Client Split

- Status: Accepted
- Date: 2026-04-15

## Context
Команде нужен ранний и стабильный split по клиентам, чтобы backend, web и mobile не начали тянуть проект в разные технологии и разные модели интеграции.

## Decision
1. Desktop client = Blazor WebAssembly PWA.
2. Mobile client = Android APK on .NET MAUI Blazor Hybrid.
3. Shared UI = Razor Class Library.
4. Backend contracts и shared DTO должны быть едины для web и mobile.
5. Финальная desktop UI-реализация остается в слое web/desktop.
6. Финальная Android UI-реализация и device-specific UX остаются в слое mobile.
7. Shared UI не становится местом, куда переносится бизнес-логика клиентов.

## Rationale
- Один стек на C# / .NET 10 уменьшает технологическую распыленность.
- Razor Class Library позволяет переиспользовать UI-части без потери ownership по клиентским shell-ам.
- Четкий split заранее защищает backend contracts от дрейфа под разные UI-подходы.

## Consequences
- Shared contracts должны появляться рано и жить отдельно от UI-слоя.
- Web и mobile могут строить свои shell-и параллельно на одном backend baseline.
- Backend не должен брать на себя client-specific UX решения.
- Любая попытка смены client stack требует отдельного решения.

## Non-goals
- Поддержка iOS на текущем этапе.
- Native desktop shell.
- Детальная дизайн-система в рамках этого ADR.