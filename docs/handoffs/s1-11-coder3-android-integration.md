# S1-11 handoff for coder 3
Target: Android MAUI Blazor Hybrid integration.
Do not change shared contracts in Android branch.
Use backend contracts as source of truth.
Primary mobile upload flow:
1. collect file metadata locally
2. if online, call POST /api/video-upload/pre-upload-check
3. handle ALLOW, BLOCK_HARD_DUPLICATE, ALLOW_WITH_REVIEW, BLOCK_POSSIBLE_FALSIFICATION
4. upload bytes direct to site when allowed
5. call POST /api/video-upload/receipts
6. store local outbox state until receipt is accepted
Offline upload behavior:
last active account only
limited upload-related functionality only
local outbox and local dedupe cache only
absolute duplicate/fraud block is not guaranteed offline
on reconnect send UploadReceipt/sync payload
Download behavior:
download intent creation is online-only
call POST /api/video-download/intents
download bytes direct from site DownloadUrl
call POST /api/video-download/receipts
do not create DownloadReceipt without server-issued intent
Worker and incidents:
mobile does not start worker jobs directly
server may create AnalyzeUploadedVideo after receipt/sync
mobile should not invent deep media result payload
Health check for stage:
GET /health/live
GET /health/ready
Do not invent:
real site provider payload
new duplicate/fraud statuses
final offline cache policy beyond current baseline
final authz policy behavior
stale download intent reconcile behavior
If an endpoint returns unknown status, display unsupported status and log it.
