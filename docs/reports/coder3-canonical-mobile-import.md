# Coder 3 canonical mobile import report

## Current step
- MOB-CANON-REPORT-06 local report draft snapshot persistence.

## Main baseline
- Base main SHA: b9fc98b
- IMPORT-01..07 are in main.
- BACKEND-00 readiness is in main.
- BACKEND-01 unresolved business-object binding guard is in main.
- REPORT-00..03 local report baseline is in main.
- REPORT-04 local field editing and selector prototype is in main.
- REPORT-05 local validation and ready-to-queue gating is in main.
- REPORT-05 Russian localization fixes are preserved.
- TEAM COORDINATION LOG #89 records the Android businessObjectKey decision:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298

## Current slice
This PR slice adds Android-local JSON metadata snapshots for FPV report drafts so local draft fields and attachment metadata survive a full app restart without backend save.

What changes:
- local report draft JSON metadata snapshots;
- restored field values;
- restored attachment metadata;
- metadata-only note for restored attachments;
- validation after restore;
- local queue action after restore.

## Explicitly not included
- production PreUploadCheck;
- UploadReceipt;
- backend create-report;
- approved backend endpoint/source for businessObjectKey;
- backend validation rules;
- media byte persistence;
- file copy;
- stream persistence;
- SQLite or local database;
- App.UI.Shared changes;
- shared contracts changes;
- backend/runtime integration.

## BusinessObject decision status
- Production direction is Option 1: Android must obtain businessObjectKey from a backend-controlled report/business-object binding source before PreUploadCheck.
- Concrete backend source/endpoint/contract is still not documented.
- Local report draft ids are not businessObjectKey.
- Local JSON snapshot ids and attachment metadata are not businessObjectKey.
- Production PreUploadCheck runtime remains blocked.

## Validation plan
- dotnet test .\tests\Unit\App.Mobile.Android.Foundation.Tests\App.Mobile.Android.Foundation.Tests.csproj -c Debug
- dotnet build .\src\App.Mobile.Android\App.Mobile.Android.csproj -f net10.0-android -m:1
- dotnet format whitespace .\AnalyticsAutomation-Core.sln --verify-no-changes --no-restore
- git diff --check

## Manual steps pending
- none for REPORT-06.

## Physical Android runtime check
- Result: passed.
- Runtime status: report06 phone ok.
- Confirmed:
  - local drafts survive full app restart;
  - edited fields survive full app restart;
  - attachment metadata survives full app restart;
  - restored attachments are metadata-only;
  - validation still works after restore;
  - local report queue action still works after restore;
  - no fake businessObjectKey is shown;
  - production PreUploadCheck remains blocked.

## Next code step
- REPORT-07 create-from-last, or wait for backend concrete businessObjectKey source.
