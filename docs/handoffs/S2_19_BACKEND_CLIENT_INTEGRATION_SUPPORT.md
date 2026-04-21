# S2-19 Backend Client Integration Support

Status: Ready for Coder 2 and Coder 3
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / VideoUpload / GroupTree / Web / Android Integration
Type: backend-platform support / integration triage baseline
Production deploy: not included

## Purpose

Support Coder 2 Web/PWA and Coder 3 Android during S2-18 client integration against the frozen backend upload control plane baseline.

Backend remains the control plane.

Video bytes must not go through App.Api as a required proxy.

## Baseline

Canonical repo:

- `uVormik/AnalyticsAutomation-Core`

Main baseline for S2-18 support:

- `8846fde S2-17 Client Upload Control Plane Handoff`

Korobochka endpoints:

- API root: `http://192.168.1.66/`
- health: `http://192.168.1.66/health/ready`
- version: `http://192.168.1.66/api/system/version`

Observed on 2026-04-21 from LAN:

- `/health/ready` returned healthy
- `/api/system/version` returned `1.0.0+c0fc31d688f725e07629f8503a0e99a8034fb307`

Interpretation:

- S2-17 was docs-only and did not deploy runtime
- the frozen client integration contract is documented on top of the existing deployed backend build

Integration account:

- login: `integration-web-android`
- password: out-of-band only

Never publish:

- password
- `accessToken`
- `refreshToken`
- GitHub tokens
- any secret value in chat, docs, PRs, logs, screenshots, or terminal transcripts

## Frozen S2-18 Upload Flow

The flow is frozen for S2-18 client integration:

1. `POST /api/auth/sign-in`
2. `GET /api/group-tree/nodes`
3. `POST /api/video/pre-upload-check`
4. direct client/site upload boundary
5. `POST /api/video/upload-receipt`

Frozen backend endpoints for S2-18:

- `POST /api/auth/sign-in`
- `GET /api/group-tree/nodes`
- `POST /api/video/pre-upload-check`
- `POST /api/video/upload-receipt`

Client-side direct upload boundary:

- client obtains site plan from `pre-upload-check`
- client uploads bytes directly to the site/provider side or a clearly marked local client stub
- App.Api does not become a required upload proxy

## Verified Expected Outcomes

Carry forward the verified baseline from S2-16 and S2-17:

| Check | Expected result |
| --- | --- |
| `POST /api/auth/sign-in` | `200` |
| `GET /api/group-tree/nodes` with bearer | `200` |
| `POST /api/video/pre-upload-check` with bearer | `200`, `decision = ALLOW` |
| `POST /api/video/upload-receipt` with bearer | `200`, `status = ACCEPTED` |
| repeated upload receipt with same logical payload and same `idempotencyKey` | `200`, `status = ALREADY_ACCEPTED` |
| anonymous `POST /api/video/pre-upload-check` | `401` |
| anonymous `POST /api/video/upload-receipt` | `401` |

If an observed result differs from this baseline, use the triage rules below before proposing any backend change.

## Smoke Matrix

Use the same backend expectations for Web/PWA and Android.

| Smoke step | Web/PWA expectation | Android expectation | Expected backend result | Primary triage lead |
| --- | --- | --- | --- | --- |
| health/version reachability | can reach Korobochka over LAN | can reach Korobochka over LAN | healthy + version payload | `ENVIRONMENT_OR_AUTH_ISSUE` if unreachable |
| sign-in | sends login/password/deviceId without logging secrets | sends login/password/deviceId without logging secrets | `POST /api/auth/sign-in -> 200` | `ENVIRONMENT_OR_AUTH_ISSUE` or `CLIENT_BUG` |
| group tree | sends bearer token | sends bearer token | `GET /api/group-tree/nodes -> 200` | `CLIENT_BUG` if auth/header wiring is wrong |
| pre-upload check | sends required metadata only | sends required metadata only | `POST /api/video/pre-upload-check -> 200`, `decision = ALLOW` | `CLIENT_BUG`, `BACKEND_BUG`, or `EXPECTED_DUPLICATE_OR_FRAUD_DECISION` depending on evidence |
| direct upload boundary | keeps bytes out of App.Api | keeps bytes out of App.Api | no App.Api proxy required | `DEFERRED_SCOPE` if provider-side adapter is not ready |
| upload receipt | sends receipt only after allowed flow | sends receipt only after allowed flow | `POST /api/video/upload-receipt -> 200`, `status = ACCEPTED` | `CLIENT_BUG` or `BACKEND_BUG` |
| idempotency repeat | repeats the same logical receipt only | repeats the same logical receipt only | `POST /api/video/upload-receipt -> 200`, `status = ALREADY_ACCEPTED` | `EXPECTED_DUPLICATE_OR_FRAUD_DECISION` if repeat was intentional |
| anonymous guardrail | optional negative smoke without bearer | optional negative smoke without bearer | anonymous pre-upload/receipt stays `401` | `BACKEND_BUG` if anonymous call is accepted |

## Manual Smoke Data Rules

For manual smoke, use a unique logical upload per first-pass attempt:

- unique `businessObjectKey`
- unique `byteSha256`
- matching `sizeBytes`

Recommended convention:

- Web example `businessObjectKey`: `web-smoke-20260421T160000Z-01`
- Android example `businessObjectKey`: `android-smoke-20260421T160000Z-01`

Rules:

- keep `businessObjectKey`, `byteSha256`, and `sizeBytes` aligned to the same logical upload
- generate a fresh trio for each new manual smoke attempt to avoid accidental duplicate/fraud decisions contaminating triage
- do not reuse a previous logical upload when trying to prove first-pass acceptance
- if the goal is idempotency verification, repeat only the same logical `UploadReceipt` with the same `idempotencyKey`
- do not mutate `byteSha256`, `sizeBytes`, `externalVideoId`, `storageKey`, or other receipt fields while claiming it is the same idempotency test

## Triage Rules

### `CLIENT_BUG`

Use when the backend contract is frozen and evidence shows the client likely sent the wrong request shape, wrong header, wrong field mapping, wrong step ordering, or an inconsistent receipt payload.

Examples:

- missing bearer token
- wrong `groupNodeId`
- receipt posted before client/site upload boundary completed
- changed `byteSha256` or `sizeBytes` between pre-check and receipt
- repeated receipt with a different payload but described as the same logical retry

### `BACKEND_BUG`

Use when the client followed the frozen contract, sent sanitized evidence that matches the expected request shape, and backend behavior differs from the verified baseline.

Examples:

- bearer `GET /api/group-tree/nodes` returns non-`200` without an auth/environment explanation
- valid `pre-upload-check` returns an unexpected status or malformed body
- valid first receipt does not return `ACCEPTED`
- anonymous pre-upload or upload-receipt is accepted instead of rejected

### `ENVIRONMENT_OR_AUTH_ISSUE`

Use when the failure is caused by LAN reachability, Korobochka health, runtime availability, bad or expired credentials, wrong base URL, or other environment/auth setup issues.

Examples:

- `/health/ready` is unavailable
- `/api/system/version` is unavailable
- sign-in fails because credentials are wrong or unavailable
- bearer token is missing because sign-in/session bootstrap failed

### `EXPECTED_DUPLICATE_OR_FRAUD_DECISION`

Use when the backend is behaving as designed for duplicate, idempotent, or decision-based control-plane outcomes.

Examples:

- repeated receipt returns `ALREADY_ACCEPTED`
- a manual smoke unintentionally reuses prior logical upload identity
- pre-upload decision is not `ALLOW` because request evidence indicates duplicate/fraud-sensitive reuse rather than a transport or contract bug

### `DEFERRED_SCOPE`

Use when the issue sits outside the S2-18 frozen control plane baseline and should not trigger backend contract changes in this increment.

Examples:

- full authz middleware hardening beyond the frozen baseline
- real site provider work beyond the stub-compatible boundary
- ffprobe/ffmpeg/Chromaprint deep media payload handling
- worker-to-incident fan-out from deep result
- final mobile offline cache policy

## Sanitized Evidence Format

Coder 2 and Coder 3 should report integration failures and suspicious outcomes in this exact format:

```text
endpoint: POST /api/video/pre-upload-check
statusCode: 200
clientPlatform: Web | Android
gitBranch: <branch-name>
gitCommit: <commit-sha>
requestShape:
  authorization: Bearer REDACTED
  body:
    userId: <sanitized-or-stable-placeholder>
    deviceId: <sanitized-or-stable-placeholder>
    groupNodeId: <sanitized-or-stable-placeholder>
    businessObjectKey: <non-secret test key>
    fileName: <sanitized file name>
    sizeBytes: <number>
    byteSha256: <sanitized hash or stable placeholder>
    contentType: <value>
    capturedAtUtc: <timestamp>
responseBody:
  <sanitized JSON body or exact status fields only>
correlationOrId:
  traceId: <value-if-present>
  preUploadCheckId: <value-if-present>
notes:
  no passwords
  no accessToken
  no refreshToken
  no GitHub tokens
```

Evidence rules:

- keep field names and status codes exact
- sanitize secrets and any value that should not leave the device/session
- include correlation/id fields when present
- include enough request shape to detect contract mismatch
- do not post raw tokens, password, cookies, or full sensitive payloads

## Known Deferred

The following remain deferred and are not blockers for the frozen S2-18 control plane baseline:

- full authz middleware hardening
- real site provider beyond stub-compatible baseline
- ffprobe/ffmpeg/Chromaprint deep media payload
- worker-to-incident fan-out from deep result
- final mobile offline cache policy

## NO-GO

Do not do any of the following while supporting S2-18:

- no backend route changes
- no shared DTO changes
- no invented statuses
- no video bytes through App.Api as a required proxy
- no secrets in logs, docs, PRs, screenshots, terminal transcripts, or chat

## Operational Guardrails

- do not run deploy workflow manually for this docs-only support change
- do not SSH to Korobochka unless diagnosis is truly required
- if backend diagnosis is needed, start with `v1-check`, `v1-status`, and `v1-logs`
- keep any diagnostic evidence sanitized and secret-free

## Decision Summary

S2-18 client integration should proceed against the frozen backend control plane contract documented here.

If Web or Android encounters failures, triage against the frozen flow and evidence format first.

Do not change routes, DTOs, statuses, or the upload proxy boundary as part of S2-18 support.
