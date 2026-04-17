# Audit / logging / health / observability foundation

## Status
- Stage: Sprint 0 / S0-11
- Scope: audit baseline, correlation id propagation, health policy expansion

## Audit baseline
- AuditRecord table
- audit categories:
  - authentication
  - devices
  - group_tree
- database-backed audit service
- request correlation id is stored in audit records

## Correlation baseline
- request header: X-Correlation-Id
- if absent, server generates one
- response echoes X-Correlation-Id
- logging scope includes correlation id and request path

## Health policy
Readiness includes:
- self
- platform_runtime
- audit_foundation
- postgresql

## First audited actions
- sign_in_failed
- sign_in_succeeded
- device_registration_upserted
- device_registration_blocked_by_flag