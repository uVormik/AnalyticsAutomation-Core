# Site contracts and integration stub

## Status
- Stage: Sprint 0 / S0-13
- Provider mode: Stub
- Real site API: not connected yet

## Goal
Freeze the backend-facing contract expectations for site upload/download integration
without blocking the rest of the solution on a live external dependency.

## Frozen expectations
### Upload receipt binding
Required fields expected from site-facing upload flow:
- provider
- externalVideoId
- storageKey
- siteStatus

Formats:
- externalVideoId: `site-video-<guid>`
- storageKey: `videos/<externalVideoId>.mp4`

Known statuses:
- uploaded
- available
- deleted

### Download intent handoff
Fields expected in future download handoff:
- externalVideoId
- storageKey
- siteStatus
- expiresAtUtc

### Reconcile lookup
Request:
- externalVideoIds

Response:
- items[]
  - externalVideoId
  - storageKey
  - status
  - checkedAtUtc

## Diagnostic endpoints
- GET `/api/system/site-gateway`
- POST `/api/system/site-gateway/reconcile-preview`

## Notes
- backend compiles and runs without a real site
- this is the agreed stub integration point for Sprint 0 freeze
- no secrets or live site credentials are used