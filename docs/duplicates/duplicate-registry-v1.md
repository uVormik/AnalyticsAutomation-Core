# VideoDuplicates Duplicate Registry v1

## Status
- Stage: Sprint 1 / S1-04
- Module: VideoDuplicates
- Endpoints:
  - `POST /api/video-duplicates/register-asset`
  - `GET /api/video-duplicates/assets/{videoAssetId}/candidates`
- Kill switch: `Modules:VideoDuplicates:RegistryEnabled`

## Scope
This step adds exact fingerprint registry baseline after UploadReceipt.

## Exact duplicate rule
`ByteSha256 + SizeBytes` match creates `HARD_DUPLICATE` candidate.

## Ownership
VideoDuplicates owns:
- `video_duplicate_assets`
- `video_duplicate_fingerprints`
- `video_duplicate_candidates`
- `video_duplicate_registry_audit_records`

## Deferred
Incident creation and admin routing are deferred to S1-05.
Deep media fingerprinting is deferred to deep dedupe phase.

## Rollback
Disable `Modules:VideoDuplicates:RegistryEnabled` or revert the PR.
The migration is additive.