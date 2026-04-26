# Coder 3 Canonical Mobile Import

## Current step
- MOB-CANON-BACKEND-01 unresolved business-object binding guard.

## IMPORT-01..07 completed in main
- Android media/outbox foundation is complete in `main` through IMPORT-07.
- Latest main SHA reviewed for this checkpoint: `e4dd243`.
- Completed slices:
  - IMPORT-01 shell/navigation
  - IMPORT-02 media picker/cache
  - IMPORT-03 local outbox
  - IMPORT-04 selected media to outbox draft
  - IMPORT-05 local duplicate-precheck
  - IMPORT-06 restart snapshots
  - IMPORT-07 repair/rebind
- Current task is docs-only backend readiness planning.
- PR #106 is merged.
- Approved source search result: no approved Android source found for `businessObjectKey` / report draft / business object binding before `PreUploadCheck`.
- This step adds an Android-local unresolved guard only.
- No production `PreUploadCheck` runtime is included.

## Base and coordination state
- Base main SHA at task start: `e4dd243`
- PR #100 is merged into `main`.
- Coordination log source: GitHub issue `#89`, not a repo file.
- Latest coordination entries reviewed:
  - `#96` feat(mobile): restore local outbox foundation
  - `#97` feat(mobile): connect selected media to local outbox draft
  - `#98` feat(mobile): restore local duplicate precheck
  - `#99` feat(mobile): restore restart-resilient local snapshots
  - `#100` feat(mobile): restore local media draft repair
  - `#101` desktop client form architecture update
  - `#102` replace stale S2-18 Web prompt with desktop-client prompt
  - `#103` desktop UI technology decision task
  - `#104` desktop UI technology owner decision
- Handoff/task cards reviewed:
  - `docs/ops/korobochka-chatgpt-context.md`
  - `docs/handoffs/s1-11-coder3-android-integration.md`
  - `docs/handoffs/s1-11-backend-integration-note.md`
  - `docs/handoffs/s1-12-sprint1-quality-gate.md`
  - `docs/task-cards/MOB-CANON-IMPORT-01.txt`
  - `docs/task-cards/MOB-CANON-IMPORT-02.txt`
  - `docs/task-cards/MOB-CANON-IMPORT-03.txt`
  - `docs/task-cards/MOB-CANON-IMPORT-04.txt`
  - `docs/task-cards/MOB-CANON-IMPORT-05.txt`
  - `docs/task-cards/MOB-CANON-IMPORT-06.txt`
  - `docs/task-cards/MOB-CANON-IMPORT-07.txt`
  - `docs/task-cards/desktop-client-form-architecture-update.txt`
  - `docs/handoffs/S2_18_DESKTOP_CLIENT_UPLOAD_CONTROL_PLANE_PROMPT.md`
  - `docs/task-cards/C2-S2-18_desktop-client-upload-control-plane-integration.txt`

## Scope restored in this PR slice
- Local repair/rebind models.
- `ILocalMediaDraftRepairService`.
- `LocalCurrentSelectionDraftRepairService`.
- `RepairLocalMediaDraftAsync` on local outbox service.
- Queue repair button for restored metadata-only drafts.
- Upload note for restored metadata-only selection.
- Unit tests for repair service and outbox repair behavior.

## Explicitly not included
- report draft
- backend S1 adapters
- PreUploadCheck
- UploadReceipt
- sync
- download
- final offline cache policy
- lookup/filter/profile UX
- incident creation
- direct upload runtime
- App.UI.Shared changes
- shared DTO/contracts changes
- SQLite/local DB
- persistence redesign
- FullPath-based design
- hashes
- ffprobe
- final dedupe logic
- worker/deep-analysis logic

## Source used
- IMPORT-07 source commit: `5c7c11e feat(mobile): restore local media draft repair`
- PR-ready replay branch base: fresh `main` at `b5d0be7`
- canonical current project: `C:\Users\yarad\source\repos\AndroidA_core`

## Validation plan
- `dotnet test .\tests\Unit\App.Mobile.Android.Foundation.Tests\App.Mobile.Android.Foundation.Tests.csproj -c Debug`
- `dotnet build .\src\App.Mobile.Android\App.Mobile.Android.csproj -f net10.0-android -m:1`
- `dotnet format whitespace .\AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`

## Physical Android runtime check
- Result: passed.
- Runtime status: backend01 phone ok.
- Upload page shows unresolved businessObjectKey blocker.
- Local intent can be saved and cleared.
- PreUploadCheck readiness check is blocked.
- No fake businessObjectKey is shown.
- Media/outbox foundation still works.

## Manual steps pending
- none for BACKEND-01.

## Waiting for coder 1
- No immediate blocker for this import slice.
- Backend/S1 integration is not started in this PR slice.

## Waiting for coder 2
- No App.UI.Shared changes are included in this PR slice.

## Next code step
- Blocked pending owner/coder 1 answer for the approved Android `businessObjectKey` source or next approved local UX replay.
- Do not start report, UX, profile, backend, or worker work in this branch.
