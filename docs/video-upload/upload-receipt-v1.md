# VideoUpload UploadReceipt v1

## Status
- Stage: Sprint 1 / S1-03
- Endpoint: `POST /api/video/upload-receipt`
- Module kill switch: `Modules:VideoUpload:UploadReceiptEnabled`

## Purpose
UploadReceipt fixes the server-side fact that a client uploaded video bytes directly to the site.

## Receipt rules
- receipt must reference an existing PreUploadCheck
- PreUploadCheck must allow site upload
- user/device/group context must match
- fingerprint and storage keys must match
- receipt is idempotent by `IdempotencyKey`

## Baseline side effects
- server-side receipt record is stored
- baseline deep-analysis job is queued in `video_upload_receipt_analysis_jobs`
- module-owned receipt audit record is stored in `video_upload_receipt_audit_records`

## Offline behavior
When server is offline, the client stores UploadReceipt in local outbox/PendingSyncItem.
On reconnect, the client sends the receipt to this endpoint.

## Rollback
Disable `Modules:VideoUpload:UploadReceiptEnabled` or revert the PR.
The migration is additive.