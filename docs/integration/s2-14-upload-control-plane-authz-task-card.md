# S2-14 Upload Control Plane AuthZ Task Card

Status: Draft
Owner: Coder 1 / Platform Owner
Module: App.Api / VideoUpload / Auth / Web / Android Integration
Type: backend authz hardening / client integration readiness

## Goal

Prepare upload control plane endpoints for safe Web and Android client integration by requiring authenticated access and adding smoke coverage.

This follows S2-13 Upload Control Plane Client Readiness Audit.

## Context

S2-13 audit confirmed that VideoUpload control plane already contains:

- `POST /api/video/pre-upload-check`;
- `POST /api/video/upload-receipt`;
- `VideoUploadOptions`;
- PreUploadCheck service;
- UploadReceipt service;
- upload receipt audit;
- upload receipt analysis job queueing.

Current architecture requires the server to remain a control plane. Video bytes are uploaded directly client ↔ site, not through the server as a required proxy.

Before Coder 2 Web and Coder 3 Android use these endpoints, they must be protected by bearer authentication and covered by App.Api integration smoke tests.

## What changes

Planned implementation PR:

- backend:
  - require authorization for `POST /api/video/pre-upload-check`;
  - require authorization for `POST /api/video/upload-receipt`;
  - add App.Api integration tests for anonymous rejection and authenticated success path.
- web:
  - no code change in this task card.
- mobile:
  - no code change in this task card.
- worker:
  - no behavior change in this task card.
- db:
  - no migration expected.
- audit:
  - existing upload receipt audit remains.
- flags:
  - no new feature flag expected for endpoint authz.
- events:
  - no new internal event expected in this step.

## Contracts

No shared DTO changes expected.
No API route changes expected.
No response payload shape changes expected.
No enum/status changes expected.

## Migration

No database migration expected.

## Offline behavior

No offline behavior change in this task.

Offline upload behavior remains constrained:

- last active account only;
- limited video-related functionality;
- absolute duplicate prevention is not guaranteed while server is offline;
- late sync must still send UploadReceipt/sync payload when online returns.

## Security

Implementation must ensure:

- anonymous `POST /api/video/pre-upload-check` returns `401`;
- anonymous `POST /api/video/upload-receipt` returns `401`;
- authenticated requests use `Authorization: Bearer <accessToken>`;
- accessToken/refreshToken are not logged;
- password is not logged;
- Web/Android must not bypass auth locally.

## Observability

Minimum test evidence:

- authenticated sign-in works;
- authenticated pre-upload check returns `200`;
- authenticated upload receipt returns expected accepted/idempotent response;
- anonymous upload control plane calls return `401`;
- health/version remain reachable.

## Rollback

Rollback by reverting implementation PR.

No database rollback expected.
No shared contract rollback expected.
No deploy rollback unless implementation is deployed and breaks smoke checks.

## Definition of Done

- task card merged;
- implementation PR selected;
- App.Api integration tests added;
- PR CI passes:
  - restore;
  - format;
  - build;
  - unit-tests;
  - integration-tests;
- no shared DTO / route / migration changes;
- production deploy not triggered by docs PR.