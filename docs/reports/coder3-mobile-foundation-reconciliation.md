# Coder 3 mobile foundation reconciliation

## Canonical App.Mobile.Android current state
The canonical repository contains the Android media/outbox/report foundation through REPORT-05:

- IMPORT-01..07 media/outbox foundation;
- BACKEND-00 backend readiness checkpoint;
- BACKEND-01 unresolved business-object binding guard;
- REPORT-00..03 local report-first baseline;
- REPORT-04 local field editing and selector prototype;
- REPORT-05 local validation and ready-to-queue gating;
- REPORT-05 Russian localization fixes.

## Current local UX slice
MOB-CANON-REPORT-06 is the current Android-local restart-resilience slice.

REPORT-06 means:
- local FPV report drafts are persisted as JSON metadata snapshots;
- edited field values are restored after app restart;
- attachment metadata is restored after app restart;
- restored attachments are clearly metadata-only;
- local validation and queue gating still run after restore.

REPORT-06 does not mean:
- backend save;
- backend validation;
- create-report API;
- media byte persistence;
- file copy;
- stream persistence;
- SQLite/local DB;
- production report flow.

## BusinessObject and backend boundary
- TEAM COORDINATION LOG #89 records the Android businessObjectKey decision:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298
- Android must obtain businessObjectKey from a backend-controlled report/business-object binding source before PreUploadCheck.
- The concrete backend source/endpoint/contract remains undocumented.
- Local report draft id is not businessObjectKey.
- JSON snapshot metadata is not businessObjectKey.
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
