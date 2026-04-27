# Coder 3 canonical mobile import report

## Current step
- MOB-CANON-REPORT-07 local create-from-last FPV draft action.

## Main baseline
- Base main SHA: aae9392
- IMPORT-01..07 are in main.
- BACKEND-00 readiness is in main.
- BACKEND-01 unresolved business-object binding guard is in main.
- REPORT-00..03 local report baseline is in main.
- REPORT-04 local field editing and selector prototype is in main.
- REPORT-05 local validation and ready-to-queue gating is in main.
- REPORT-05 Russian localization fixes are preserved.
- REPORT-06 local report draft snapshot persistence is in main.
- PR #113 manual replay is recorded in TEAM COORDINATION LOG:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4324921544
- TEAM COORDINATION LOG #89 records the Android businessObjectKey decision:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298

## Current slice
This PR slice adds an Android-local create-from-last helper for FPV report drafts after local snapshot persistence is in main.

What changes:
- local "Создать из последнего" action;
- copied draft receives a new DraftId;
- latest local draft field values are copied;
- attachments are not copied;
- queued-local state is not copied;
- copied draft is persisted through the local snapshot store.

## Explicitly not included
- production PreUploadCheck;
- UploadReceipt;
- backend create-report;
- approved backend endpoint/source for businessObjectKey;
- backend validation rules;
- App.UI.Shared changes;
- shared contracts changes;
- backend/runtime integration.

## BusinessObject decision status
- Production direction is Option 1: Android must obtain businessObjectKey from a backend-controlled report/business-object binding source before PreUploadCheck.
- Concrete backend source/endpoint/contract is still not documented.
- Local report draft ids are not businessObjectKey.
- Local copied draft ids are not businessObjectKey.
- Production PreUploadCheck runtime remains blocked.

## Validation plan
- dotnet test .\tests\Unit\App.Mobile.Android.Foundation.Tests\App.Mobile.Android.Foundation.Tests.csproj -c Debug
- dotnet build .\src\App.Mobile.Android\App.Mobile.Android.csproj -f net10.0-android -m:1
- dotnet format whitespace .\AnalyticsAutomation-Core.sln --verify-no-changes --no-restore
- git diff --check

## Manual steps pending
- none for REPORT-07.

## REPORT-07 physical Android runtime check
- Result: passed.
- Runtime status: report07 phone ok.
- Confirmed:
  - "Создать из последнего" works when at least one draft exists;
  - no-draft warning/disabled state works;
  - copied draft has new DraftId;
  - copied draft copies field values;
  - copied draft does not copy attachments;
  - copied draft status is Draft;
  - copied draft can be edited, receive new video, validate, and queue locally;
  - no fake businessObjectKey is shown;
  - production PreUploadCheck remains blocked.

## REPORT-06 physical Android runtime check
- Result: passed.
- Runtime status: report06 phone ok.

## Next code step
- UX polishing, or wait for backend concrete businessObjectKey source.
