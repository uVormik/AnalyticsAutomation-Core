# S2-12 LAN Auth Flow Verification

Status: Completed
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / GroupTree / Korobochka / Web / Android Integration
Type: client integration verification

## Goal

Record that the authenticated client flow works from the laptop over LAN against Korobochka.

This verifies the same path that Coder 2 Web and Coder 3 Android must use for shared integration.

## Environment

Canonical repo:

- `uVormik/AnalyticsAutomation-Core`

Korobochka endpoint:

- API root: `http://192.168.1.66/`
- health: `http://192.168.1.66/health/ready`
- version: `http://192.168.1.66/api/system/version`

Local main during verification:

- `3ebbbb1 S2-11 Integration Account Provisioning Verification`

Runtime version observed on Korobochka:

- `1.0.0+e85d484...`

Note:

- S2-11 provisioning verification was docs-only after the maintenance tool deploy.
- Runtime remained on the last deployed code commit, which is expected for docs-only PRs.

## Account

Login:

- `integration-web-android`

Password:

- supplied out-of-band only;
- not stored in repo;
- not stored in this document;
- not printed in verification output.

## Verified flow

From laptop over LAN:

1. `GET /health/ready` returned healthy.
2. `GET /api/system/version` returned production version payload.
3. `POST /api/auth/sign-in` returned `200`.
4. `accessToken` was present.
5. Authenticated `GET /api/group-tree/nodes` returned `200`.
6. Anonymous `GET /api/group-tree/nodes` returned `401`.

Security evidence:

- `PASSWORD_PRINTED=false`
- `TOKENS_PRINTED=false`

## Client handoff

Coder 2 Web and Coder 3 Android may use:

- API root: `http://192.168.1.66/`
- login: `integration-web-android`
- password: provided out-of-band only

Expected client flow:

1. call `POST /api/auth/sign-in`;
2. read `accessToken`;
3. call `GET /api/group-tree/nodes` with `Authorization: Bearer <accessToken>`;
4. expect `200 OK`;
5. anonymous `GET /api/group-tree/nodes` must remain `401 Unauthorized`.

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

## Rollback / rotation

If integration access must be revoked or rotated:

- reset password with the approved maintenance tool/runbook;
- or disable/reset the account through approved ops procedure;
- do not publish old/new password in repo or chat.

## Production deploy

Not triggered by this documentation PR.