# S1-11 Backend integration note
Status: Sprint 1 backend integration handoff.
Scope: web and Android integration against backend control-plane contracts.
Runtime code changes: none.
DB migration: none.
Auth note: current auth foundation is baseline. Full authz policy middleware hardening is deferred.
Stage note: use stage API only after stage URL/env is provided by owner.
Core rule: video bytes go direct client <-> site. API is control plane.
Health endpoints:
GET /health/live
GET /health/ready
WebsiteIntegration endpoints:
GET /api/website-integration/contracts
POST /api/website-integration/upload-receipts/preview
POST /api/website-integration/download-intents/preview
POST /api/website-integration/reconcile/preview
Upload pre-check endpoint:
POST /api/video-upload/pre-upload-check
PreUploadCheck decisions:
ALLOW
BLOCK_HARD_DUPLICATE
ALLOW_WITH_REVIEW
BLOCK_POSSIBLE_FALSIFICATION
PreUploadCheck minimal request fields:
userId, deviceId, groupNodeId, businessObjectKey, fileName, contentType, sizeBytes, byteSha256
Upload receipt endpoint:
POST /api/video-upload/receipts
UploadReceipt statuses:
accepted
duplicate_ignored
rejected
UploadReceipt minimal request fields:
clientReceiptKey, userId, deviceId, groupNodeId, businessObjectKey, externalVideoId, fileName, contentType, sizeBytes, byteSha256, uploadedAtUtc
Duplicate registry endpoints:
POST /api/video-duplicates/assets/register
POST /api/video-duplicates/candidates/evaluate
GET /api/video-duplicates/candidates/{candidateId}
Duplicate match classes:
HARD_DUPLICATE
SUSPECT_DUPLICATE
POSSIBLE_FALSIFICATION
Duplicate incident endpoints:
POST /api/incidents/duplicates
GET /api/incidents/duplicates/assigned/{assignedAdminUserId}
POST /api/incidents/duplicates/{incidentId}/decision
Duplicate incident statuses:
ASSIGNED
RESOLVED
IGNORED_BELOW_THRESHOLD
Duplicate decisions:
CONFIRM_DUPLICATE
DISMISS_DUPLICATE
ESCALATE
Fraud signal endpoints:
POST /api/fraud-signals/evaluate-upload
GET /api/fraud-signals/incidents/assigned/{assignedAdminUserId}
POST /api/fraud-signals/incidents/{incidentId}/decision
Fraud severities:
LOW
MEDIUM
HIGH
Fraud decisions:
CONFIRM_SUSPICION
DISMISS_SUSPICION
ESCALATE
Worker pipeline endpoints:
POST /api/worker-pipeline/jobs/analyze-uploaded-video
POST /api/worker-pipeline/jobs/process-one
GET /api/worker-pipeline/jobs/{jobId}
GET /api/worker-pipeline/jobs?take=25
Worker job type:
AnalyzeUploadedVideo
Worker statuses:
queued
running
completed
failed
Download endpoints:
POST /api/video-download/intents
POST /api/video-download/receipts
GET /api/video-download/intents/{downloadIntentId}
GET /api/video-download/receipts/{downloadReceiptId}
GET /api/video-download/status-vocabulary
Download intent statuses:
created
consumed
expired
rejected
Download receipt statuses:
accepted
duplicate_ignored
Online upload flow:
1. Client calculates local file metadata.
2. Client calls PreUploadCheck.
3. If allowed, client uploads bytes direct to site.
4. Client sends UploadReceipt to API.
5. Backend writes audit and can queue AnalyzeUploadedVideo.
6. Duplicate or fraud incidents can be assigned to admins.
Offline upload flow:
1. Client uses last active account only.
2. Client performs local outbox/dedupe-cache check.
3. Client uploads to site if allowed by offline UX.
4. Client stores PendingSyncItem/outbox item.
5. On reconnect, client sends UploadReceipt/sync payload.
6. Server performs late duplicate/fraud detection.
Online download flow:
1. Client calls CreateDownloadIntent.
2. API returns DownloadUrl.
3. Client downloads bytes direct from site.
4. Client sends DownloadReceipt.
Known limitations:
Real site provider beyond stub-compatible URL is deferred.
Deep media ffprobe/ffmpeg/Chromaprint payload is deferred.
Full authz policy middleware hardening is deferred.
Worker-to-incident fan-out from deep result is deferred.
Offline absolute duplicate/fraud block is not guaranteed.
Generic error shape:
Use HTTP 400 for invalid request, 409 for rejected business operation, 404 for missing read model, 5xx for unexpected server error.
Do not invent new statuses in clients. Unknown statuses must be displayed as unknown/unsupported and logged.
