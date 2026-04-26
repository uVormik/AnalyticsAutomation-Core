# Coder 3 Canonical Mobile Import

## Current step
- MOB-CANON-REPORT-04 local report field editing and selector prototype.

## Main baseline
- IMPORT-01..07 are in main.
- BACKEND-01 unresolved business-object binding guard is in main.
- REPORT-00..03 local report-first baseline is in main.
- Latest main SHA reviewed for this replay: `0c98e62`.
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

## Scope restored in this PR slice
- Local report field editing.
- Local field metadata.
- Local stub selector panel.
- Local stub lookup provider behavior.
- Local draft field update behavior.

## Explicitly not included
- production `PreUploadCheck`
- `UploadReceipt`
- backend create-report
- approved backend endpoint/source for `businessObjectKey`
- auth/session/device binding
- direct upload runtime
- download runtime
- production reports engine
- report validation
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
- This slice is Android-local field editing/selector UX only.

## Validation plan
- `dotnet test .\tests\Unit\App.Mobile.Android.Foundation.Tests\App.Mobile.Android.Foundation.Tests.csproj -c Debug`
- `dotnet build .\src\App.Mobile.Android\App.Mobile.Android.csproj -f net10.0-android -m:1`
- `dotnet format whitespace .\AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- `git diff --check`

## Physical Android runtime check
- Result: passed.
- Runtime status: report04 phone ok.
- Confirmed:
  - report draft fields render by sections.
  - text/number/toggle fields can be edited locally.
  - selector fields open local stub selector.
  - selected stub values update local draft fields.
  - media attachments still work.
  - local report queue action still works.
  - no fake `businessObjectKey` is shown.
  - production `PreUploadCheck` remains blocked.

## Manual steps pending
- none for REPORT-04.

## Next code step
- REPORT-05 local validation summary, or wait for backend concrete `businessObjectKey` source.
