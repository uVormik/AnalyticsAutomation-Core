# C2 S1-002 Upload integration handoff

Branch:
feature/web-upload-integration-binding

GeneratedAt:
2026-04-20 19:11:18

## Status

This bounded web increment is complete at adapter/UI-boundary level.

The current branch implements the web-side upload path scaffolding for:

1. PreUploadCheck
2. disabled direct site upload adapter boundary
3. UploadReceipt request/UI boundary

This does not claim production direct site upload runtime is configured.

## Completed in App.Web

### Upload API adapter surface

Files:
- src/App.Web/Features/Upload/Api/UploadApiEndpoints.cs
- src/App.Web/Features/Upload/Api/IVideoUploadApi.cs
- src/App.Web/Features/Upload/Api/HttpVideoUploadApi.cs

Endpoints used:
- POST /api/video/pre-upload-check
- POST /api/video/upload-receipt

The adapter uses frozen BuildingBlocks.Contracts.VideoUpload DTOs.
No shared DTOs were changed.

### PreUploadCheck UI binding

Files:
- src/App.Web/Features/Upload/Models/UploadPreCheckFormModel.cs
- src/App.Web/Features/Upload/Models/UploadPreCheckRequestFactory.cs
- src/App.Web/Features/Upload/Pages/UploadPage.razor
- src/App.Web/Features/Upload/Pages/UploadPage.razor.cs

Frozen request shape mapped:
- UserId
- DeviceId
- GroupNodeId
- BusinessObjectKey
- FileName
- SizeBytes
- ByteSha256
- ContentType
- CapturedAtUtc

CapturedAtUtc is mapped as DateTimeOffset from UTC ISO-8601 string.

Frozen decision vocabulary handled:
- ALLOW
- BLOCK_HARD_DUPLICATE
- ALLOW_WITH_REVIEW
- BLOCK_POSSIBLE_FALSIFICATION

Unsupported decisions are not mapped to fake states.
They are shown as unsupported and logged.

### Direct site upload adapter boundary

Files:
- src/App.Web/Features/Upload/SiteGateway/DirectSiteVideoUploadDraft.cs
- src/App.Web/Features/Upload/SiteGateway/DirectSiteVideoUploadResult.cs
- src/App.Web/Features/Upload/SiteGateway/IDirectSiteVideoUploadAdapter.cs
- src/App.Web/Features/Upload/SiteGateway/DisabledDirectSiteVideoUploadAdapter.cs

Current implementation:
- DisabledDirectSiteVideoUploadAdapter

Purpose:
- keep the adapter boundary explicit
- avoid pretending production site upload runtime is configured
- allow the UI flow to stop honestly between PreUploadCheck and UploadReceipt

### UploadReceipt UI boundary

Files:
- src/App.Web/Features/Upload/Models/UploadReceiptFormModel.cs
- src/App.Web/Features/Upload/Models/UploadReceiptRequestFactory.cs
- src/App.Web/Features/Upload/Pages/UploadPage.razor
- src/App.Web/Features/Upload/Pages/UploadPage.razor.cs

Frozen request shape mapped:
- PreUploadCheckId
- UserId
- DeviceId
- GroupNodeId
- ExternalVideoId
- StorageKey
- SiteStatus
- SizeBytes
- ByteSha256
- IdempotencyKey
- UploadedAtUtc

Receipt status vocabulary handled:
- ACCEPTED
- ALREADY_ACCEPTED
- REJECTED

Unsupported receipt statuses are not mapped to fake states.
They are shown as unsupported and logged.

## Tests added or updated

Project:
- tests/Unit/App.Web.Tests/App.Web.Tests.csproj

Test files:
- UploadApiEndpointsTests.cs
- UploadDecisionPresentationTests.cs
- UploadReceiptPresentationTests.cs
- UploadPreCheckRequestFactoryTests.cs
- DirectSiteVideoUploadAdapterTests.cs
- UploadReceiptRequestFactoryTests.cs

Current test count observed:
- 14 tests passed after Step 36

## Current guardrails

Do not change shared DTOs in this web branch.

Do not invent:
- new PreUploadCheck decisions
- new UploadReceipt statuses
- duplicate/fraud incident payloads
- production site provider payload
- download control plane behavior

Do not treat DisabledDirectSiteVideoUploadAdapter as production upload runtime.

## Remaining work

Next bounded step should be one of:

1. Wire real site upload adapter only when site provider contract/runtime is explicitly available.
2. Add manual verification note / PR checklist for current upload integration branch.
3. Start admin duplicate/fraud review UI only under a separate bounded task and using frozen incident contracts.

## Recent commits

08e9cbc feat(web): add upload receipt UI boundary
c63291f feat(web): add direct site upload adapter boundary
587503c feat(web): bind upload screen to pre-upload check
8ef0e49 feat(web): add upload API adapter surface
891eca7 docs(web): add task card for upload integration binding
0405419 S2-04 Manual Korobochka Deploy Verification
3687c3b S2-04 Manual Korobochka Deploy Workflow
7b010e8 S2-04 Korobochka Deploy Workflow Task Card
aae841c Docs/korobochka chatgpt context (#55)
5516e9d S2-03 CI Actions Node 24 Readiness