# S2-01 Platform AuthZ Hardening

Status: Draft
Owner: Coder 1 / Platform Owner
Module: Auth / App.Api / Platform
Type: security hardening

## Goal

Harden API authorization policy coverage after S1-12 Gate without changing shared DTOs, public API payloads, database schema, endpoint routes, or mobile API compatibility.

## Context

S1-12 closed Sprint 1 backend/platform foundation for web/mobile handoff, but production deploy is still not allowed.
Known deferred item: full authz middleware hardening.

## What changes

- backend: audit and harden authorization policy coverage for existing API endpoints.
- web: no change.
- mobile: no change.
- worker: no behavior change.
- db: no migration.
- audit: no schema change.
- flags: no new flag for documentation/audit phase.
- events: no new events.

## Contracts

No shared DTO changes.
No route changes.
No response payload shape changes.
No enum/status changes.

## Migration

Not needed.

## Offline behavior

No offline behavior change.
Offline client behavior remains under existing S1 rules.

## Security

Expected policy direction:
- business endpoints require authenticated access;
- health/live and explicitly safe system endpoints may remain anonymous;
- upload/download/incidents/sync endpoints must have explicit authorization intent;
- no silent widening of access.

## Observability

Expected:
- denied access remains visible through structured logs;
- no secrets or tokens in logs;
- tests prove anonymous vs authenticated behavior for selected endpoints.

## Rollback

Rollback by reverting the PR.
No database rollback.
No shared contract rollback.
No production deploy included.

## Definition of Done

- endpoint authorization coverage map exists in docs/auth;
- tests cover at least one protected business endpoint rejecting anonymous access;
- tests confirm health/live remains accessible as intended;
- existing web/mobile handoff routes keep compatible payloads;
- dotnet restore/build/test pass;
- PR states contracts: none, migration: none, feature flag: none, rollback: revert PR.
