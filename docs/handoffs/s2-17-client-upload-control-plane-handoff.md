# S2-17 Client Upload Control Plane Handoff

Status: Ready for Coder 2 and Coder 3
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / VideoUpload / GroupTree / Web / Android Integration
Type: handoff / client integration baseline
Production deploy: not included

## Goal

Give Coder 2 Web and Coder 3 Android a precise backend baseline for integrating the first upload control plane flow against Korobochka.

## Current backend state

Main is expected to be at:

- `8854531 S2-16 Upload Control Plane LAN Verification`

Korobochka runtime was verified after S2-15 deploy:

- API root: `http://192.168.1.66/`
- health: `http://192.168.1.66/health/ready`
- version: `http://192.168.1.66/api/system/version`

Verified runtime version during S2-16:

- `1.0.0+c0fc31d...`

S2-16 docs verification was merged after runtime deploy and does not require another deploy.

## Account

Use:

- login: `integration-web-android`
- password: provided out-of-band only

Never put the password into:

- GitHub;
- ChatGPT;
- PRs;
- logs;
- screenshots;
- terminal transcripts;
- appsettings;
- source code.

## Required client flow

### 1. Sign in

Call:

- `POST /api/auth/sign-in`

Body shape:

- `login`
- `password`
- `deviceId`

Expected result:

- HTTP `200`
- response contains `accessToken`

Client rule:

- store/use `accessToken` only as needed;
- do not log `accessToken`;
- do not log `refreshToken`.

### 2. Load group tree

Call:

- `GET /api/group-tree/nodes`

Header:

- `Authorization: Bearer <accessToken>`

Expected result:

- HTTP `200`

Anonymous behavior:

- anonymous request must return `401`.

Client rule:

- select a valid group node before upload;
- initial shared smoke can use `root` if no UI selection is ready yet.

### 3. Pre-upload check

Call:

- `POST /api/video/pre-upload-check`

Header:

- `Authorization: Bearer <accessToken>`

Required request fields:

- `userId`
- `deviceId`
- `groupNodeId`
- `businessObjectKey`
- `fileName`
- `sizeBytes`
- `byteSha256`
- `contentType`
- `capturedAtUtc`

Expected S2-16 verified result:

- HTTP `200`
- `decision = ALLOW`
- `canUploadToSite = true`
- response contains `preUploadCheckId`
- response contains `sitePlan.externalVideoId`
- response contains `sitePlan.storageKey`
- response contains `sitePlan.requiredReceiptEndpoint = /api/video/upload-receipt`

Client rule:

- do not upload video bytes to App.Api;
- video bytes go directly client ↔ site according to site plan;
- server remains control plane.

### 4. Direct site upload

Current site provider is still a stub/foundation integration.

Client integration may treat this step as:

- call direct site upload if the site adapter is ready;
- otherwise simulate direct upload only in a clearly marked local/client stub;
- do not send video bytes to App.Api as a workaround.

### 5. Upload receipt

Call:

- `POST /api/video/upload-receipt`

Header:

- `Authorization: Bearer <accessToken>`

Required request fields:

- `preUploadCheckId`
- `userId`
- `deviceId`
- `groupNodeId`
- `externalVideoId`
- `storageKey`
- `siteStatus`
- `sizeBytes`
- `byteSha256`
- `idempotencyKey`
- `uploadedAtUtc`

Expected S2-16 verified result:

- first receipt:
  - HTTP `200`
  - status field: `ACCEPTED`
- repeated receipt with the same idempotency key:
  - HTTP `200`
  - status field: `ALREADY_ACCEPTED`

Anonymous behavior:

- anonymous `POST /api/video/pre-upload-check` returns `401`;
- anonymous `POST /api/video/upload-receipt` returns `401`.

## Coder 2 Web instructions

Scope:

- implement Web/PWA client-side API usage for the upload control plane baseline;
- do not change backend routes or DTOs;
- do not SSH to Korobochka from Web tasks;
- do not bypass auth locally.

Suggested first increment:

1. sign-in using integration account;
2. store bearer token in a safe client dev/session path;
3. load group tree;
4. create a minimal pre-upload request from selected/local file metadata;
5. show pre-upload decision and site plan;
6. submit upload receipt after direct-site upload stub/adapter step.

Do not implement admin incident UI in this increment.

## Coder 3 Android instructions

Scope:

- implement Android/.NET MAUI client-side API usage for the upload control plane baseline;
- do not change backend routes or DTOs;
- do not SSH to Korobochka from Android tasks;
- do not bypass auth locally.

Suggested first increment:

1. sign-in using integration account;
2. store bearer token only in approved app/session storage;
3. load group tree;
4. collect local file metadata:
   - file name;
   - size bytes;
   - content type;
   - SHA-256;
   - captured timestamp if available;
5. call pre-upload check;
6. only proceed to site upload when decision allows it;
7. submit upload receipt.

Offline upload queue remains a later task.

## Security requirements

Both clients must follow:

- do not log password;
- do not log `accessToken`;
- do not log `refreshToken`;
- do not store tokens in plain logs;
- do not store integration password in repo;
- do not send video bytes through App.Api as a required proxy;
- do not fake backend authorization in shared integration;
- report exact HTTP status and sanitized body when integration fails.

## Contracts

No shared DTO changes in this handoff.

If a client needs contract change:

- stop;
- propose exact additive DTO change;
- coordinate with Coder 1;
- do not silently change shared contracts.

## Migration

No database migration.

## Feature flag

No new feature flag in this handoff.

## Offline behavior

Offline upload behavior is not implemented by this handoff.

Reminder:

- offline server mode cannot guarantee absolute duplicate prevention;
- late sync must still send receipts/sync data when online returns;
- offline access is limited to the last active account and video-required actions.

## Rollback

Revert this docs PR if the handoff wording is wrong.

Runtime rollback is not needed because this PR does not deploy code.

## Validation evidence

S2-16 LAN verification confirmed:

- sign-in returned `200`;
- authenticated group tree returned `200`;
- authenticated pre-upload check returned `200`;
- pre-upload decision was `ALLOW`;
- upload receipt returned `ACCEPTED`;
- repeated receipt returned `ALREADY_ACCEPTED`;
- anonymous upload endpoints returned `401`;
- password and tokens were not printed.