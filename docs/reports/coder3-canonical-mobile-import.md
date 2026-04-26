# Coder 3 Canonical Mobile Import

## Current step
- MOB-CANON-REPORT-05 local validation summary and ready-to-queue gating.

## Main baseline
- IMPORT-01..07 are in main.
- BACKEND-01 unresolved business-object binding guard is in main.
- REPORT-00..03 local report-first baseline is in main.
- REPORT-04 local field editing and selector prototype is in main.
- Latest main SHA reviewed for this replay: `5e17f51`.
- TEAM COORDINATION LOG #89 records the Android `businessObjectKey` decision:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298

## Completed Android media/outbox/report foundation
- IMPORT-01 shell/navigation
- IMPORT-02 media picker/cache
- IMPORT-03 local outbox
- IMPORT-04 selected media to outbox draft
- IMPORT-05 local duplicate-precheck
- IMPORT-06 restart snapshots
- IMPORT-07 repair/rebind
- BACKEND-01 unresolved business-object binding guard before production `PreUploadCheck`
- REPORT-00..03 local report-first baseline
- REPORT-04 local field editing and selector prototype

## Scope restored in this PR slice
- Android-local validation models.
- Android-local validation service.
- Validation summary in report draft.
- Local ready-to-queue gating.
- Outbox protection against invalid local report drafts.

## Explicitly not included
- production `PreUploadCheck`
- `UploadReceipt`
- backend create-report
- approved backend endpoint/source for `businessObjectKey`
- auth/session/device binding
- direct upload runtime
- download runtime
- production reports engine
- backend validation rules
- report persistence
- incident creation
- App.UI.Shared changes
- shared DTO/contracts changes
- App.Api/backend changes
- Modules changes
- DB migrations
- workflows/deploy changes

## BusinessObject decision status
- Production direction is Option 1: Android must obtain `businessObjectKey` from a backend-controlled report/business-object binding source before `PreUploadCheck`.
- The concrete backend source/endpoint/contract remains undocumented.
- Local report draft ids are not `businessObjectKey`.
- Local queue action is not backend report save.
- Production `PreUploadCheck` runtime remains blocked.
- This slice is Android-local validation/gating only.
- This local validation is not backend validation.

## Validation plan
- `dotnet test .\tests\Unit\App.Mobile.Android.Foundation.Tests\App.Mobile.Android.Foundation.Tests.csproj -c Debug`
- `dotnet build .\src\App.Mobile.Android\App.Mobile.Android.csproj -f net10.0-android -m:1`
- `dotnet format whitespace .\AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- `git diff --check`

## Physical Android runtime check
- Result: passed after runtime localization fixes.
- Runtime localization check: passed.
- Runtime status: report05 ru ok.
- Confirmed:
  - validation summary renders in report draft.
  - missing required fields are shown in Russian.
  - missing video attachment is shown in Russian.
  - incomplete draft is blocked from local queue.
  - completed draft can be placed into local queue.
  - field editing and selector behavior still work.
  - media attachments still work.
  - no fake `businessObjectKey` is shown.
  - production `PreUploadCheck` remains blocked.
- Runtime issue found after PR #112 opened:
  - physical Android check found Russian localization mojibake in visible UI.
  - physical Android check found user-facing English terms in report validation/media/field text.
- Fix scope:
  - ReportDraft visible text and `MobileUiText` Russian localization only.
  - User-facing backend/upload/save/validation wording replaced with Russian wording where applicable.
  - Empty report field placeholders render current Russian localization even for existing local placeholder field state.
  - Technical identifiers remain unchanged where needed.

## Manual steps pending
- none for REPORT-05-R1.

## Next code step
- REPORT-06 local report draft snapshot persistence, or wait for backend concrete `businessObjectKey` source.
