---
name: Task card
about: Planned work item for a controlled increment
title: "[Task] "
labels: ["feature"]
assignees: []
---

## Goal
Кратко опишите ценность и ожидаемый результат.

## Module
В каком модуле живет изменение? Кто владелец?

## Type
- новый модуль
- расширение модуля
- новый adapter
- bugfix
- refactor
- infra/process

## What changes
- backend:
- web:
- mobile:
- worker:
- db:
- audit:
- flags:
- events:

## New or changed contracts
- incoming:
- outgoing:
- shared DTO:
- internal events:

## Migration
- [ ] no migration
- [ ] additive-first migration required

Details:

## Offline behavior
Что происходит offline?
Что запрещено offline?
Нужны ли queue / outbox / receipt / late sync?

## Security
- auth/authz impact:
- device/user scope:
- audit requirements:

## Observability
- logs:
- health checks / metrics / counters:
- incident scenario:

## Rollback
Как выключить / откатить безопасно?

## Definition of done
- [ ] code
- [ ] tests
- [ ] logging
- [ ] audit
- [ ] docs / handoff note
- [ ] dependencies on other coders described