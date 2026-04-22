# S2-27 UploadReceiptSync Runtime Evidence

Status: Completed
Owner: Coder 1 / Platform Owner
Module: App.Api / VideoUpload / Auth / WorkerPipeline / Korobochka
Type: runtime evidence / deployment verification
Production deploy: already completed before this documentation PR

## Goal

Record that S2-26 Late Sync Upload Receipt Intake was deployed to Korobochka and smoke-tested through the new late/offline sync endpoint.

## Source commit

Deployed target:

- `007c59b S2-26 Late Sync Upload Receipt Intake`

Included previous runtime foundation:

- `9c0b8cd S2-22 Post-Receipt Pipeline Bridge`

## Deployment evidence

Manual deploy workflow:

- workflow: `deploy-korobochka.yml`
- run id: `24776602588`
- result: `success`
- head sha: `007c59b9403cb792a8d229075023456405735651`

Korobochka runtime after deploy:

- `/health/ready`: healthy
- `/api/system/version`: `1.0.0+007c59b9403cb792a8d229075023456405735651`
- release: `20260422-134902`
- `v1-check`: `V1_CHECK_OK`

## Endpoint evidence

New endpoint:

- `POST /api/video/upload-receipt-sync`

Anonymous smoke:

- anonymous `POST /api/video/upload-receipt-sync` returned `401`.

Authenticated smoke:

- `POST /api/auth/sign-in` returned `200`;
- `accessToken` was present but not printed;
- authenticated `GET /api/group-tree/nodes` returned `200`;
- root group node was resolved;
- authenticated `POST /api/video/upload-receipt-sync` returned `200`;
- first sync receipt status was `ACCEPTED`;
- repeated sync receipt with the same idempotency key returned `200`;
- repeated sync receipt status was `ALREADY_ACCEPTED`.

## DTO shape observed

`VideoUploadReceiptSyncRequestDto` fields used by smoke:

- `userId`
- `deviceId`
- `groupNodeId`
- `businessObjectKey`
- `fileName`
- `contentType`
- `externalVideoId`
- `storageKey`
- `siteStatus`
- `sizeBytes`
- `byteSha256`
- `idempotencyKey`
- `capturedAtUtc`
- `uploadedAtUtc`

## Security evidence

- `PASSWORD_PRINTED=false`
- `TOKENS_PRINTED=false`
- request values were not printed;
- no password, token, private key, runner token, DB secret, or `/opt/v1-pyton/secrets` content is included.

## Architecture notes

The server remains control plane.

Video bytes must not be routed through App.Api as a required proxy.

S2-26 adds late/offline receipt sync intake. It does not weaken the existing online `POST /api/video/upload-receipt` flow.

## Contracts

Additive public API contract already merged in S2-26:

- new route `POST /api/video/upload-receipt-sync`;
- new request DTO `VideoUploadReceiptSyncRequestDto`.

No existing DTO or route was removed.

## Migration

No database migration.

## Feature flag

S2-26 introduced:

- `Modules:VideoUpload:UploadReceiptSyncEnabled`

Purpose:

- kill switch for the new late/offline sync intake endpoint.

## Offline behavior

This endpoint is the approved backend intake foundation for late/offline upload receipt sync.

Final mobile offline cache/outbox policy remains separate client scope.

## Rollback

Runtime rollback if needed:

- set `Modules:VideoUpload:UploadReceiptSyncEnabled=false`;
- or revert S2-26 implementation PR;
- existing online upload receipt flow remains available through `POST /api/video/upload-receipt`.

Documentation rollback:

- revert this docs PR.

## Production deploy

Not triggered by this documentation PR.