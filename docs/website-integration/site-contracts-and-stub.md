# Site contracts and integration stub

## Status
- Stage: Sprint 1 / S1-01
- Provider mode: Stub
- Real site API: not connected yet

## Goal
Freeze backend-facing site gateway expectations and provide a stable stub path
for future PreUploadCheck, UploadReceipt and DownloadIntent work.

## Current diagnostic endpoints
- GET `/api/system/site-gateway`
- POST `/api/system/site-gateway/upload-receipt-preview`
- POST `/api/system/site-gateway/download-intent-preview`
- GET `/api/system/site-gateway/status/{externalVideoId}`
- POST `/api/system/site-gateway/reconcile-preview`

## Frozen expectations
### Upload receipt preview
Required fields:
- provider
- externalVideoId
- storageKey
- siteStatus

### Download intent preview
Response fields:
- externalVideoId
- storageKey
- siteStatus
- downloadUrl
- expiresAtUtc

### Status lookup
- route: `/api/system/site-gateway/status/{externalVideoId}`
- response field: `siteStatus`

### Reconcile lookup
- request: `externalVideoIds`
- response: `items[]`

## Known stub statuses
- uploaded
- available
- deleted

## Notes
- backend remains independent from a live site
- this module is the agreed adapter boundary for Sprint 1 upload/download work
- no secrets or live credentials are used