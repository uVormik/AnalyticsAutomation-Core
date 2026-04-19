# S2-01 Endpoint AuthZ Coverage Map

Status: Draft
Owner: Coder 1 / Platform Owner
Module: Auth / App.Api / Platform
Source: local endpoint inventory after S1-12 Gate

## Purpose

Document current API endpoint authorization intent before changing runtime authorization behavior.

This map is intentionally documentation-first. Runtime code changes must follow in a separate small step after review of this coverage map.

## Current observation

The endpoint inventory found API route mappings across App.Api and Sprint 1 modules.
The same inventory did not show endpoint-level RequireAuthorization or AllowAnonymous markers near the route mappings.

This does not yet prove the absence of global middleware or test-host authorization behavior.
It means endpoint-level intent is not explicit enough and must be hardened.

## Endpoint groups

### Public / anonymous by intent

| Endpoint | Method | Module | Intended authz |
|---|---:|---|---|
| /health/live | GET | App.Api | Anonymous |
| /health/ready | GET | App.Api | Anonymous or ops-safe readiness policy |
| /api/system/version | GET | App.Api | Anonymous or ops-safe system info |
| /api/auth/sign-in | POST | App.Api/Auth | Anonymous |
| /api/auth/refresh | POST | App.Api/Auth | Token/session based, not anonymous business access |

### System diagnostics / platform endpoints

| Endpoint | Method | Module | Intended authz |
|---|---:|---|---|
| /api/system/platform-foundation | GET | App.Api | Authenticated platform/admin policy |
| /api/system/observability | GET | App.Api | Authenticated platform/admin policy |
| /api/system/site-gateway | GET | WebsiteIntegration | Authenticated platform/admin policy |
| /api/system/site-gateway/upload-receipt-preview | POST | WebsiteIntegration | Authenticated platform/admin policy |
| /api/system/site-gateway/download-intent-preview | POST | WebsiteIntegration | Authenticated platform/admin policy |
| /api/system/site-gateway/status/{externalVideoId} | GET | WebsiteIntegration | Authenticated platform/admin policy |
| /api/system/site-gateway/reconcile-preview | POST | WebsiteIntegration | Authenticated platform/admin policy |

### Authenticated business endpoints

| Endpoint | Method | Module | Intended authz |
|---|---:|---|---|
| /api/group-tree/nodes | GET | GroupTree/App.Api | Authenticated |
| /api/group-tree/routing-preview | GET | GroupTree/App.Api | Authenticated admin/platform policy |
| /api/devices/register | POST | Devices/App.Api | Authenticated |
| /api/video/pre-upload-check | POST | VideoUpload | Authenticated uploader policy |
| /api/video/upload-receipt | POST | VideoUpload | Authenticated uploader policy |
| /api/video-download/intents | POST | VideoDownload | Authenticated downloader policy |
| /api/video-download/receipts | POST | VideoDownload | Authenticated downloader policy |
| /api/video-download/intents/{downloadIntentId} | GET | VideoDownload | Authenticated same-scope/admin policy |
| /api/video-download/receipts/{downloadReceiptId} | GET | VideoDownload | Authenticated same-scope/admin policy |
| /api/video-download/status-vocabulary | GET | VideoDownload | Authenticated or explicitly anonymous vocabulary |
| /api/video-duplicates/register-asset | POST | VideoDuplicates | Authenticated system/platform policy |
| /api/video-duplicates/assets/{videoAssetId}/candidates | GET | VideoDuplicates | Authenticated admin/platform policy |
| /api/incidents/duplicates | POST | Incidents | Authenticated system/platform policy |
| /api/incidents/duplicates/assigned/{assignedAdminUserId} | GET | Incidents | Authenticated assigned-admin policy |
| /api/incidents/duplicates/{incidentId}/decision | POST | Incidents | Authenticated assigned-admin policy |
| /api/fraud-signals/evaluate-upload | POST | FraudSignals | Authenticated system/platform policy |
| /api/fraud-signals/incidents/assigned/{assignedAdminUserId} | GET | FraudSignals | Authenticated assigned-admin policy |
| /api/fraud-signals/incidents/{incidentId}/decision | POST | FraudSignals | Authenticated assigned-admin policy |
| /api/worker-pipeline/jobs/analyze-uploaded-video | POST | WorkerPipeline | Authenticated platform/worker policy |
| /api/worker-pipeline/jobs/process-one | POST | WorkerPipeline | Authenticated platform/worker policy |
| /api/worker-pipeline/jobs/{jobId} | GET | WorkerPipeline | Authenticated platform/worker policy |
| /api/worker-pipeline/jobs | GET | WorkerPipeline | Authenticated platform/worker policy |

## First hardening target

The first runtime change should be narrow:

1. Keep health/live and sign-in anonymous.
2. Add explicit authorization intent for at least one protected business endpoint.
3. Add tests proving anonymous access is rejected for that endpoint.
4. Add tests proving health/live stays accessible.

## Non-goals

- No shared DTO changes.
- No route changes.
- No response payload shape changes.
- No database migration.
- No production deploy.
- No role model redesign in this step.

## Open questions before runtime patch

- Is there already global fallback authorization configured in Program.cs?
- Are integration tests currently using auth bypass/test auth handler?
- Which existing policy names already exist and should be reused?
- Should status vocabulary stay authenticated or be explicitly anonymous for client bootstrap?

## Rollback

Documentation-only change.
Rollback by reverting the PR commit.
