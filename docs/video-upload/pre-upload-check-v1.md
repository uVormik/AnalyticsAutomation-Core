# VideoUpload PreUploadCheck v1

## Status
- Stage: Sprint 1 / S1-02
- Endpoint: `POST /api/video/pre-upload-check`
- Module kill switch: `Modules:VideoUpload:PreUploadCheckEnabled`

## Purpose
PreUploadCheck is the online server gate before a client uploads video bytes directly to the site.

## Decisions
- `ALLOW`
- `BLOCK_HARD_DUPLICATE`
- `ALLOW_WITH_REVIEW`
- `BLOCK_POSSIBLE_FALSIFICATION`

## Fast-path duplicate rule
The first exact fingerprint check is allowed.
A later check with the same `ByteSha256 + SizeBytes` is blocked as `BLOCK_HARD_DUPLICATE`.
A repeated attempt by the same user after a hard duplicate block is treated as `BLOCK_POSSIBLE_FALSIFICATION`.

## Offline behavior
When server is offline, this endpoint is unavailable.
Client-side offline mode must use local outbox/cache and cannot guarantee absolute duplicate blocking.

## Rollback
Disable `Modules:VideoUpload:PreUploadCheckEnabled` or revert the PR.