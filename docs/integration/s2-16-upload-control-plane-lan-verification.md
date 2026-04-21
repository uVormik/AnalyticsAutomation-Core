# S2-16 Upload Control Plane LAN Verification

Status: Completed
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / VideoUpload / GroupTree / Korobochka / Web / Android Integration
Type: authenticated LAN smoke verification

## Goal

Record that the authenticated upload control plane flow works from the laptop over LAN against Korobochka.

This verifies the backend path that Coder 2 Web and Coder 3 Android must use for the first upload vertical slice.

## Environment

Canonical repo:

- `uVormik/AnalyticsAutomation-Core`

Korobochka endpoint:

- API root: `http://192.168.1.66/`
- health: `http://192.168.1.66/health/ready`
- version: `http://192.168.1.66/api/system/version`

Local main during verification:

- `c0fc31d S2-15 Upload Control Plane AuthZ`

Runtime version observed on Korobochka:

- `1.0.0+c0fc31d...`

## Account

Login:

- `integration-web-android`

Password:

- supplied out-of-band only;
- not stored in repo;
- not written to this document;
- not printed in verification output.

## Verified authenticated flow

From laptop over LAN:

1. `GET /health/ready` returned healthy.
2. `GET /api/system/version` returned production version payload.
3. `POST /api/auth/sign-in` returned `200`.
4. `accessToken` was present.
5. Authenticated `GET /api/group-tree/nodes` returned `200`.
6. Root group node was resolved.
7. Authenticated `POST /api/video/pre-upload-check` returned `200`.
8. Pre-upload decision was `ALLOW`.
9. Pre-upload response contained:
   - `preUploadCheckId`;
   - site plan external video id;
   - site plan storage key;
   - receipt endpoint `/api/video/upload-receipt`.
10. Authenticated `POST /api/video/upload-receipt` returned `200`.
11. Upload receipt status was `ACCEPTED`.
12. Repeating the same upload receipt returned `200`.
13. Repeated upload receipt status was `ALREADY_ACCEPTED`.

## Verified anonymous behavior

Anonymous calls stayed rejected:

- anonymous `POST /api/video/pre-upload-check` returned `401`;
- anonymous `POST /api/video/upload-receipt` returned `401`.

## Security evidence

- `PASSWORD_PRINTED=false`
- `TOKENS_PRINTED=false`

## Client handoff

Coder 2 Web and Coder 3 Android may use the following upload control plane sequence:

1. call `POST /api/auth/sign-in`;
2. read `accessToken`;
3. call `GET /api/group-tree/nodes` with `Authorization: Bearer <accessToken>`;
4. select the applicable group node;
5. call `POST /api/video/pre-upload-check` with bearer token;
6. if decision is `ALLOW`, upload video bytes directly client to site;
7. call `POST /api/video/upload-receipt` with bearer token;
8. treat repeated receipt with same idempotency key as idempotent.

## Architecture notes

The server remains the control plane.

Video bytes must not be routed through App.Api as a required proxy.

## Security requirements

- do not log password;
- do not log `accessToken`;
- do not log `refreshToken`;
- do not store tokens in plain logs;
- do not store integration password in repo;
- do not paste password into GitHub, ChatGPT, PRs, screenshots, or terminal transcripts;
- do not SSH to Korobochka from Web/Android tasks;
- do not bypass auth locally.

## Contracts

No shared DTO changes.

## Migration

No database migration.

## Feature flag

No application feature flag.

## Offline behavior

No offline behavior change.

Offline upload behavior remains a separate later task.

## Rollback / rotation

If integration access must be revoked or rotated:

- reset password with the approved maintenance tool/runbook;
- or disable/reset the account through approved ops procedure;
- do not publish old/new password in repo or chat.

## Production deploy

Not triggered by this documentation PR.