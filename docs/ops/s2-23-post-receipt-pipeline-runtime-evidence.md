# S2-23 Post-Receipt Pipeline Runtime Evidence

Status: Completed
Owner: Coder 1 / Platform Owner
Module: App.Worker / WorkerPipeline / VideoUpload / VideoDuplicates / Incidents / Korobochka
Type: runtime evidence / deployment verification
Production deploy: already completed before this documentation PR

## Goal

Record that S2-22 Post-Receipt Pipeline Bridge was deployed to Korobochka and runtime smoke evidence confirms the worker bridge is active.

## Source commits

Current deployed main target:

- `51ec912 feat(web): upload integration binding (#74)`

S2-22 implementation is included in current main:

- `9c0b8cd S2-22 Post-Receipt Pipeline Bridge`

## Deployment evidence

Manual deploy workflow:

- workflow: `deploy-korobochka.yml`
- run id: `24769624152`
- result: `success`
- head sha: `51ec91247948e1a5f764304b82ea2cae8ab50191`

Korobochka runtime after deploy:

- `/health/ready`: healthy
- `/api/system/version`: `1.0.0+51ec91247948e1a5f764304b82ea2cae8ab50191`
- release: `20260422-105840`
- `v1-check`: `V1_CHECK_OK`
- `S2_22_BRIDGE_SOURCE_PRESENT`

## Runtime worker evidence

S2-22 bridge runtime logs confirmed:

- `UploadReceiptPipelineBridgeService` started an analysis job;
- command name was `video-upload.deep-analysis`;
- `VideoDuplicateRegistryService` registered a video asset;
- `UploadReceiptPipelineBridgeService` completed the analysis job;
- completion included:
  - `DuplicateCandidateCount=0`;
  - `IncidentCount=0`.

This confirms the exact-hash post-receipt bridge is active on Korobochka for the non-duplicate path.

## What is verified

Verified:

- App.Api deploy at current main version.
- App.Worker service active.
- S2-22 bridge source present in deployed repo.
- Worker bridge processes at least one queued upload receipt analysis job.
- Duplicate registry exact-hash registration path executed.
- No duplicate incident was expected for the observed unique asset path.

## What is not yet verified

Not yet verified in runtime evidence:

- duplicate candidate creation from two accepted receipts with the same exact hash;
- duplicate incident creation on Korobochka runtime;
- fraud signal path;
- ffprobe / ffmpeg / Chromaprint deep media path;
- mobile offline outbox / late sync path.

These remain follow-up scope.

## Architecture notes

The server remains control plane.

Video bytes must not be routed through App.Api as a required proxy.

## Security

No passwords, tokens, private keys, runner tokens, DB secrets, or `/opt/v1-pyton/secrets` contents are included in this document.

## Contracts

No shared DTO changes.

## Migration

No database migration.

## Feature flag

No new feature flag.

S2-22 runtime behavior remains governed by the existing `Modules:WorkerPipeline:DeepAnalysisEnabled` behavior documented in the implementation PR.

## Offline behavior

No offline behavior change in this documentation step.

## Rollback

Rollback for this documentation PR:

- revert this docs PR.

Runtime rollback for S2-22 if needed:

- revert the implementation PR;
- or disable the worker bridge using the existing deep analysis kill switch if operationally required.

## Production deploy

Not triggered by this documentation PR.