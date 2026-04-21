# S2-11 Integration Account Provisioning Verification

Status: Completed
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / Korobochka / Ops
Production deploy: already completed before provisioning verification

## Goal

Record that the dedicated Korobochka integration account was provisioned safely and verified without exposing password, access token, refresh token, token hashes, password hash, private keys, runner token, or secret files.

## Account

Login:

- `integration-web-android`

Purpose:

- shared LAN integration for Coder 2 Web and Coder 3 Android;
- authenticated API smoke path;
- Web/Android integration against Korobochka.

Password handling:

- password was supplied out-of-band;
- password is not stored in repo;
- password is not written to this document;
- password must not be posted to GitHub, ChatGPT, PRs, logs, screenshots, or terminal transcripts.

## Provisioning result

Safe provisioning evidence:

- account status: created;
- assigned role: `platform_owner`;
- assigned group node: `root`;
- `app.auth_users` count changed from `0` to `1`;
- `POST /api/auth/sign-in` returned `200`;
- authenticated `GET /api/group-tree/nodes` returned `200`;
- anonymous `GET /api/group-tree/nodes` returned `401`;
- report confirmed `PASSWORD_PRINTED=false`;
- report confirmed `TOKENS_PRINTED=false`.

## Client handoff

Coder 2 Web and Coder 3 Android may use:

- API root: `http://192.168.1.66/`
- health: `http://192.168.1.66/health/ready`
- version: `http://192.168.1.66/api/system/version`
- login: `integration-web-android`
- password: provided out-of-band only

Expected flow:

1. call `POST /api/auth/sign-in`;
2. read `accessToken`;
3. call `GET /api/group-tree/nodes` with `Authorization: Bearer <accessToken>`;
4. expect `200 OK`;
5. anonymous `GET /api/group-tree/nodes` must remain `401 Unauthorized`.

## Security

Hard requirements:

- do not log `accessToken`;
- do not log `refreshToken`;
- do not log password;
- do not store the password in repo;
- do not store the password in appsettings;
- do not commit secret files;
- do not use DevelopmentBootstrap as shared integration auth;
- do not create hidden auth bypass;
- do not SSH to Korobochka from Web/Android work.

## Rollback / rotation

If access must be revoked or rotated:

- reset password using the approved maintenance tool/runbook;
- or disable/reset the account through an approved ops step;
- do not delete auth schema;
- do not remove seed roles or permissions;
- do not publish old/new password in repo or chat.

## Contracts

No shared DTO changes.

## Migration

No database migration.

## Feature flag

No application feature flag.

## Offline behavior

No offline behavior change.

## Production deploy

Not triggered by this documentation PR.