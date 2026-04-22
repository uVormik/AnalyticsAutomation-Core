# S2-33 Duplicate Incident Runtime Evidence

Status: Completed
Owner: Coder 1 / Platform Owner
Module: VideoUpload / WorkerPipeline / VideoDuplicates / Incidents / GroupTree / Korobochka
Type: runtime evidence / deployment verification
Production deploy: already completed before this documentation PR

## Goal

Record that the duplicate incident runtime path is now proven on Korobochka after S2-31 incident routing admin provisioning and S2-32 duplicate runtime smoke.

## Runtime baseline

Korobochka runtime target used for the smoke:

- current main includes `965c591 add S2-18 upload control plane baseline (#83)`;
- S2-31 implementation is included through `c89af3a S2-31 Incident Routing Admin Assignment Provisioning`;
- App.Api version observed during smoke: `1.0.0+965c591f1aad1218667a0ea50d7051bfb4a1f6e9`;
- `/health/ready`: healthy.

## Prerequisite evidence from S2-31

S2-31 provisioning created a dedicated routing admin baseline:

- user: `incident-routing-admin`;
- role: `platform_owner`;
- group admin assignment: `root -> incident-routing-admin`;
- routing-preview for `root + integration-web-android` resolved at least one admin;
- `v1-check`: `V1_CHECK_OK`;
- no password, token, or password hash was printed.

## S2-32 smoke evidence

S2-32 executed duplicate runtime smoke through the approved late/offline receipt sync endpoint:

- anonymous `POST /api/video/upload-receipt-sync` returned `401`;
- authenticated sign-in for `integration-web-android` returned `200`;
- authenticated `GET /api/group-tree/nodes` returned `200`;
- routing preview endpoint returned `200`;
- two late-sync receipts were submitted with the same exact fingerprint:
  - same `byteSha256`;
  - same `sizeBytes`;
  - different idempotency keys;
- second receipt idempotency repeat was executed;
- request values were not printed.

## Worker / incident evidence

Worker logs confirmed the full exact-hash duplicate path:

- duplicate registry registered a video asset;
- duplicate candidate count was `1`;
- duplicate incident routing created a `DuplicateIncident`;
- upload receipt analysis job completed with:
  - `DuplicateCandidateCount=1`;
  - `IncidentCount=1`.

This proves that the first backend upload vertical slice now reaches:

`late/offline upload receipt sync -> worker bridge -> duplicate registry -> duplicate candidate -> duplicate incident routing`.

## Architecture notes

The server remains the control plane.

Video bytes must not be routed through App.Api as a required proxy.

This evidence covers exact-hash duplicate detection. It does not claim deep media dedupe through ffprobe / ffmpeg / Chromaprint.

## What is verified

Verified:

- S2-31 admin routing provisioning works.
- Routing baseline no longer returns empty admin routing for the root uploader scenario.
- S2-26 `upload-receipt-sync` endpoint remains bearer-protected.
- S2-22 worker bridge processes late-sync receipts.
- Duplicate registry creates candidate for exact `byteSha256 + sizeBytes`.
- Duplicate incident is created after routing baseline exists.
- No password/token/request values were printed in smoke evidence.

## What remains follow-up scope

Not yet verified as final product scope:

- admin review UI;
- incident list/read model for Web;
- mobile offline outbox integration;
- deep media fingerprints:
  - ffprobe;
  - ffmpeg;
  - Chromaprint;
- fraud suspicion incident path;
- final reporting/export path;
- incident decision workflow.

## Security

No passwords, password hashes, access tokens, refresh tokens, private keys, runner tokens, DB secrets, or `/opt/v1-pyton/secrets` content are included in this document.

## Contracts

No new shared DTO changes in this documentation PR.

No public API route changes in this documentation PR.

## Migration

No database migration.

## Feature flags

No new feature flag in this documentation PR.

Runtime behavior remains controlled by previously implemented module flags and operational provisioning.

## Rollback

Documentation rollback:

- revert this docs PR.

Runtime rollback if required:

- disable/revoke `incident-routing-admin` assignment by approved operator procedure;
- rotate or disable the dedicated admin account;
- use existing feature flags / kill switches for runtime paths where applicable.

## Production deploy

Not triggered by this documentation PR.