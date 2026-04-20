# S2-05 Client Integration Readiness

Status: Draft
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / GroupTree / Korobochka Integration
Type: integration readiness / backend-client handoff

## Goal

Prepare a stable backend integration baseline for Coder 2 Web and Coder 3 Android using Korobochka as the shared LAN integration endpoint.

This task starts the transition from platform hardening to applied client/backend integration work.

## Context

S2-01 delivered the first AuthZ hardening increment.
S2-02 hardened CI test failure propagation.
S2-03 updated CI actions to Node 24-compatible versions.
S2-04 added and verified manual Korobochka deployment.

Korobochka is now the runtime/integration endpoint for clients, but GitHub remains the source of truth.

## Current integration endpoint

Use:

- API root: `http://192.168.1.66/`
- health: `http://192.168.1.66/health/ready`
- version: `http://192.168.1.66/api/system/version`

Do not use local Ubuntu/WSL as a shared backend.
Do not use stale repositories.
Do not SSH to Korobochka from web/android work.

## Current API expectations

Public / anonymous:

- `GET /health/live`
- `GET /health/ready`
- `GET /api/system/version`

Authentication:

- `POST /api/auth/sign-in`
- `POST /api/auth/refresh`

Protected business endpoint already enforced:

- `GET /api/group-tree/nodes` requires bearer access token.

## What changes

- backend:
  - document client integration baseline;
  - identify minimum backend smoke checks for web/android;
  - prepare follow-up code tasks for authenticated API smoke coverage.
- web:
  - no code change in this task card.
- mobile:
  - no code change in this task card.
- worker:
  - no behavior change.
- db:
  - no migration.
- audit:
  - no schema change.
- flags:
  - no new feature flag for this docs/task-card step.
- events:
  - no new events.

## Contracts

No shared DTO changes in this task.
No endpoint route changes.
No response payload shape changes.
No enum/status changes.

## Migration

Not needed.

## Offline behavior

No offline behavior change.

Android offline constraints remain:

- last active account only;
- limited video-related functionality;
- no absolute duplicate prevention when server is offline.

## Security

- clients must treat bearer access token as sensitive;
- clients must not log tokens;
- protected endpoints must be called with `Authorization: Bearer <accessToken>`;
- Coder 2 and Coder 3 must not bypass auth locally for shared integration.

## Observability

Minimum checks for integration support:

- health endpoint reachable from laptop/client LAN;
- version endpoint returns current deployed commit/version;
- unauthorized protected endpoint returns 401;
- authenticated protected endpoint returns expected payload after sign-in.

## Rollback

Docs-only task card.
Rollback by reverting PR.

No database rollback.
No shared contract rollback.
No deploy rollback.

## Definition of Done

- Coder 2 and Coder 3 receive the Korobochka integration baseline.
- Backend/client auth expectations are documented.
- Follow-up implementation task is selected:
  - add App.Api integration smoke test for sign-in + authenticated `GET /api/group-tree/nodes`;
  - or add a dedicated integration handoff document with exact request/response examples.
- PR states:
  - module;
  - contracts: none;
  - migration: none;
  - feature flag: none;
  - rollback;
  - production deploy: not included.