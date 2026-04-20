# S2-06 Authenticated API Smoke Coverage

Status: Draft
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / GroupTree / Integration Tests
Type: backend integration hardening

## Goal

Add backend smoke coverage for the authenticated client integration path:

1. sign in;
2. receive access token;
3. call protected `GET /api/group-tree/nodes` with `Authorization: Bearer <accessToken>`;
4. verify successful response.

This is the first implementation step after S2-05 Client Integration Readiness.

## Context

S2-05 documented Korobochka as the shared LAN integration endpoint for Web and Android clients.

Current known API behavior:

- public health/version endpoints remain anonymous;
- `GET /api/group-tree/nodes` requires bearer authentication;
- clients must not bypass auth for shared integration.

Coder 2 and Coder 3 need a stable backend smoke path before deeper UI/mobile integration.

## What changes

- backend:
  - add App.Api integration test coverage for authenticated access;
  - verify sign-in token can access protected group tree endpoint.
- web:
  - no code change in this task.
- mobile:
  - no code change in this task.
- worker:
  - no behavior change.
- db:
  - no migration expected.
- audit:
  - no schema change.
- flags:
  - no new feature flag expected for test coverage.
- events:
  - no new internal events.

## Contracts

No shared DTO changes expected.
No endpoint route changes.
No response payload shape changes.
No enum/status changes.

## Migration

Not needed.

## Offline behavior

No offline behavior change.

## Security

- bearer access token must be used only in test scope;
- no secrets committed;
- no test token logged;
- protected endpoint must reject anonymous and accept authenticated request.

## Observability

Test evidence must cover:

- anonymous `GET /api/group-tree/nodes` returns 401;
- authenticated `GET /api/group-tree/nodes` returns 200;
- `/health/live` remains anonymous.

## Rollback

Rollback by reverting the PR.
No database rollback.
No shared contract rollback.
No deploy rollback.

## Definition of Done

- App.Api integration tests cover authenticated group tree access;
- tests remain CI-safe and isolated from external PostgreSQL;
- dotnet format passes;
- PR CI passes:
  - restore;
  - format;
  - build;
  - unit-tests;
  - integration-tests;
- PR states:
  - module;
  - contracts: none;
  - migration: none;
  - feature flag: none;
  - rollback;
  - production deploy: not included.