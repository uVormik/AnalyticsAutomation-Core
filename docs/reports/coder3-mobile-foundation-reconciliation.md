# Coder 3 mobile foundation reconciliation

## Canonical App.Mobile.Android current state
The canonical repository contains the Android media/outbox/report foundation through REPORT-06:

- IMPORT-01..07 media/outbox foundation;
- BACKEND-00 backend readiness checkpoint;
- BACKEND-01 unresolved business-object binding guard;
- REPORT-00..03 local report-first baseline;
- REPORT-04 local field editing and selector prototype;
- REPORT-05 local validation and ready-to-queue gating;
- REPORT-05 Russian localization fixes;
- REPORT-06 local report draft snapshot persistence.

## Current local UX slice
MOB-CANON-REPORT-07 is the current Android-local report helper slice.

REPORT-07 means:
- users can create a new local FPV draft from the latest local draft;
- copied drafts receive a new DraftId;
- copied drafts copy field values only;
- copied drafts do not copy attachments;
- copied drafts do not copy queued-local state;
- copied drafts persist through the existing local snapshot store.

REPORT-07 does not mean:
- backend save;
- backend validation;
- create-report API;
- backend-side create-from-last;
- production report flow.

## BusinessObject and backend boundary
- TEAM COORDINATION LOG #89 records the Android businessObjectKey decision:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298
- Android must obtain businessObjectKey from a backend-controlled report/business-object binding source before PreUploadCheck.
- The concrete backend source/endpoint/contract remains undocumented.
- Local report draft id is not businessObjectKey.
- Copied local report draft id is not businessObjectKey.
- Production PreUploadCheck remains blocked.

## Scope guard
- no App.UI.Shared changes;
- no App.Api changes;
- no shared DTO/contracts changes;
- no DB migrations;
- no backend runtime integration;
- no invented create-report API;
- no fake businessObjectKey;
- no local draft id as businessObjectKey;
- no production PreUploadCheck;
- no UploadReceipt.

Backend adapter runtime should wait for the approved concrete Android source of businessObjectKey / report draft / business object binding before any production PreUploadCheck runtime.
