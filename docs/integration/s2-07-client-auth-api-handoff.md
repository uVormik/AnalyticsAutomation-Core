# S2-07 Client Auth API Handoff

Status: Ready for team consumption
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / GroupTree / Web / Android Integration
Type: task card + handoff
Goal: give Coder 2 and Coder 3 an exact baseline for authenticated API usage.
Contracts: no shared DTO changes
Migration: none
Feature flag: none
Offline behavior: no change
Rollback: revert docs PR
Production deploy: not included

## Task Card

### Context

S2-06 confirmed the shared authenticated smoke path on `main`:

1. sign in;
2. read `accessToken`;
3. send `Authorization: Bearer <accessToken>`;
4. call `GET /api/group-tree/nodes`;
5. receive `200 OK`.

This handoff gives Coder 2 Web and Coder 3 Android the exact integration baseline they should use next.

Canonical repo: `uVormik/AnalyticsAutomation-Core`.
Korobochka is the shared LAN integration endpoint, not the Git source of truth.

### Scope and guardrails

- docs-only handoff for authenticated API usage;
- no production code changes;
- no shared contract or DTO changes;
- no route changes;
- no migration;
- no package changes;
- no deploy workflow activity;
- no secrets, tokens, passwords, or SSH keys added to the repo.

## Handoff

### Integration Endpoint

Use Korobochka LAN integration endpoints:

- API root: `http://192.168.1.66/`
- health ready URL: `http://192.168.1.66/health/ready`
- version URL: `http://192.168.1.66/api/system/version`

Do not use Korobochka as a replacement for Git history, PR review, or source control truth.

### API Baseline

Public endpoints:

- `GET /health/live`
- `GET /health/ready`
- `GET /api/system/version`

Auth endpoints:

- `POST /api/auth/sign-in`
- `POST /api/auth/refresh`

Protected endpoint:

- `GET /api/group-tree/nodes` requires `Authorization: Bearer <accessToken>`.

### Client Sequence

Use the existing shared contracts and integration-tested flow:

1. Call `POST /api/auth/sign-in` with `login`, `password`, and `deviceId`.
2. Read `accessToken` from the sign-in response.
3. Send `Authorization: Bearer <accessToken>` when calling `GET /api/group-tree/nodes`.
4. Expect `200 OK` for the authenticated group tree request.
5. Expect `401 Unauthorized` when `GET /api/group-tree/nodes` is called anonymously.

Do not invent new DTO fields in Web or Android branches. When exact response shape is needed, use the existing shared contracts and App.Api integration tests:

- [shared-contracts-v0.md](../contracts/shared-contracts-v0.md)
- [s2-06-authenticated-api-smoke-coverage.md](s2-06-authenticated-api-smoke-coverage.md)
- [AuthenticatedEndpointTests.cs](../../tests/Integration/App.Api/AuthenticatedEndpointTests.cs)
- [AnonymousEndpointTests.cs](../../tests/Integration/App.Api/AnonymousEndpointTests.cs)

### Security Notes

- do not log `accessToken` or `refreshToken`;
- do not store tokens in plain logs;
- do not bypass auth locally for shared integration;
- do not SSH to Korobochka from web/android work;
- when reporting failures, include exact status and body with token values redacted.

### Coder 2 Web Handoff

- Web/PWA should call the Korobochka LAN API at `http://192.168.1.66/`.
- Use bearer token authentication for `GET /api/group-tree/nodes`.
- If integration fails, report the exact HTTP status and response body with tokens redacted.
- Do not add or assume new auth or group tree DTO fields beyond the existing shared contracts.

### Coder 3 Android Handoff

- Android should call the Korobochka LAN API at `http://192.168.1.66/`.
- Mobile API compatibility is preserved.
- Offline behavior is unchanged.
- Bearer token is required for online `GET /api/group-tree/nodes` calls.
- Do not add or assume new auth or group tree DTO fields beyond the existing shared contracts.

### Validation Evidence

- S2-06 PR [#61](https://github.com/uVormik/AnalyticsAutomation-Core/pull/61) merged on 2026-04-20.
- `main` is at `e62d44d` (`S2-06 Authenticated API Smoke Coverage`).
- `main` CI succeeded for `restore`, `format`, `build`, `unit-tests`, and `integration-tests`.
- App.Api integration smoke covers authenticated group tree access and anonymous rejection:
  - `GET /api/group-tree/nodes` returns `200 OK` with bearer auth;
  - `GET /api/group-tree/nodes` returns `401 Unauthorized` when anonymous.

## Rollback

Rollback is docs-only: revert the docs PR.
No database rollback.
No shared contract rollback.
Production deploy is not included.
