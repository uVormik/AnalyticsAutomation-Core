# ADR 0004 — Dedup and Fraud Policy

- Status: Accepted
- Date: 2026-04-15

## Context
Проект должен не только ловить точные дубли, но и выявлять подозрительные и возможные фальсифицированные попытки загрузки.
Политика должна одинаково трактоваться backend, web и mobile.

## Decision
1. Duplicate/fraud модель многослойная:
   - exact fingerprint = ByteSha256 + Size
   - technical media fingerprint = ffprobe metadata
   - normalized fingerprint = streamhash / framehash
   - content fingerprint = video signature + audio fingerprint
2. Классы совпадений:
   - Hard duplicate
   - Suspect duplicate
   - Possible falsification
3. При online-сервере PreUploadCheck обязателен до загрузки видео.
4. PreUploadCheck возвращает один из результатов:
   - ALLOW
   - BLOCK_HARD_DUPLICATE
   - ALLOW_WITH_REVIEW
   - BLOCK_POSSIBLE_FALSIFICATION
5. При offline-сервере абсолютная блокировка дублей не гарантируется.
6. После UploadReceipt или отложенной синхронизации сервер обязан быстро выполнить анализ и при необходимости создать incident.
7. Правило маршрутизации incident:
   - обычный пользователь -> админ своей ветки
   - админ ветки -> админ уровнем выше
   - если на нужной ноде несколько админов, получают все ответственные этой ноды
   - если достигнут верх дерева, получает root/supervisory admin
8. Критичные решения и переходы по этой политике аудируются.

## Rationale
- Exact duplicate check нужен для быстрого hard block.
- Более глубокие слои нужны для detect/review сценариев и возможной фальсификации.
- Правило эскалации по дереву групп — часть бизнес-политики, а не UI-решение.

## Consequences
- Worker-based deep analysis должен быть предусмотрен уже в первой серверной архитектуре.
- UploadReceipt, duplicate registry, incidents и routing нельзя считать второстепенными.
- Web и mobile не должны придумывать собственные названия duplicate/fraud статусов.
- Первая реализация может стартовать с exact/technical слоя и расширяться без ломки контракта.

## Non-goals
- Полная deep-media science реализация в Sprint 0.
- Финальная настройка scoring thresholds в этом ADR.
- Полный UI lifecycle для incident review.