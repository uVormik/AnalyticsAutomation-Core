# S1-11 handoff for coder 2
Target: Web/Desktop PWA integration.
Do not change shared contracts in web branch.
Use backend contracts as source of truth.
Primary upload UI flow:
1. collect file metadata
2. call POST /api/video-upload/pre-upload-check
3. handle ALLOW, BLOCK_HARD_DUPLICATE, ALLOW_WITH_REVIEW, BLOCK_POSSIBLE_FALSIFICATION
4. upload bytes direct to site when allowed
5. call POST /api/video-upload/receipts
6. show receipt status accepted, duplicate_ignored, or rejected
Admin review UI can use:
GET /api/incidents/duplicates/assigned/{assignedAdminUserId}
POST /api/incidents/duplicates/{incidentId}/decision
GET /api/fraud-signals/incidents/assigned/{assignedAdminUserId}
POST /api/fraud-signals/incidents/{incidentId}/decision
Worker status UI can use:
GET /api/worker-pipeline/jobs/{jobId}
GET /api/worker-pipeline/jobs?take=25
Download UI flow:
1. call POST /api/video-download/intents
2. open DownloadUrl or stream direct from site in browser context
3. call POST /api/video-download/receipts
Health check for stage:
GET /health/live
GET /health/ready
Do not invent:
real site provider payload
new duplicate/fraud statuses
deep media analysis payload
final authz policy behavior
stale download intent reconcile behavior
If an endpoint returns unknown status, display unsupported status and log it.
