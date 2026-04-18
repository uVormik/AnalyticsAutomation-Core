# VideoDownload control plane v1

## Status
- Stage: Sprint 1 / S1-08
- Module: VideoDownload
- Kill switch: `Modules:VideoDownload:Enabled`

## Flow
1. Client calls `POST /api/video-download/intents`.
2. Server validates user/device/group context and creates a download intent.
3. Client downloads video bytes directly from the site URL returned in the intent.
4. Client calls `POST /api/video-download/receipts`.
5. Server fixes the download fact and writes audit records.

## Endpoints
- `POST /api/video-download/intents`
- `POST /api/video-download/receipts`
- `GET /api/video-download/intents/{downloadIntentId}`
- `GET /api/video-download/receipts/{downloadReceiptId}`
- `GET /api/video-download/status-vocabulary`

## Status vocabulary
Intent:
- `created`
- `consumed`
- `expired`
- `rejected`

Receipt:
- `accepted`
- `duplicate_ignored`

## Tables
- `video_download_intents`
- `video_download_receipts`
- `video_download_audit_records`

## Site integration
This v1 uses a stub-compatible site download URL generated from:
`Modules:VideoDownload:StubDownloadBaseUrl`.

The actual direct bytes flow remains client ↔ site.
The server remains a control plane.

## Offline behavior
Download intent creation is online-only.
Offline clients must not create new download intents.
DownloadReceipt without a server-issued intent is rejected.

## Rollback
Disable `Modules:VideoDownload:Enabled` or revert the PR.
Migration is additive and keeps intent/receipt history.