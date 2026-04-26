# Coder 3 Report-First Mobile Plan

## Why this correction exists
- Previous import slices focused on media and outbox foundation.
- The primary mobile flow is report or flight draft creation first.
- Video is attached to a report, not the top-level user object.
- The Android `businessObjectKey` decision is recorded in TEAM COORDINATION LOG #89:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298

## Correct mobile object model direction
Local-only foundation names, not shared DTO/contracts:
- `MobileReportDraft`
- `MobileReportDraftStatus`
- `MobileReportAttachment`
- `MobileReportAttachmentKind`
- `MobileReportDraftFieldValue`
- `IMobileReportDraftStore`
- `InMemoryMobileReportDraftStore`
- `StubMobileReportLookupProvider`

Direction:
- `MobileReportDraft` is the parent.
- Video, photo, and log files are attachments.
- Selected-media cache feeds attachment creation.
- A local outbox item may reference `reportDraftId` and `attachmentId` as local metadata.
- `businessObjectKey` must later come from a backend-controlled report/business-object binding source, not local Android state.

## Existing foundation reinterpretation
- `AndroidNativeMediaService` = attachment source provider.
- `IMobileSelectedMediaStore` = temporary selected attachment cache.
- `IMobileOutboxService` = local pending attachment or report action queue.
- duplicate-precheck = local attachment duplicate warning only.
- snapshot stores = local draft and attachment restart-resilience foundation.
- repair/rebind = restored attachment local-file access repair.

## Baseline UX direction
- The first report screen is a local `Полеты` / report draft list.
- The create action exposes local FPV draft creation.
- Media blocks live inside the report draft.
- Duplicate same-video attachments are blocked locally.
- The draft can be placed into the local outbox.

## What stays mobile-local for now
- report draft shell
- local draft state
- local lookup stub provider as a non-production seam
- report form shell sections
- attachment blocks
- Android media binding
- outbox and draft repair and restart behavior

## What must wait for coder 1 / backend owner
- concrete report/business-object binding source
- `businessObjectKey` source
- report server id or remote id semantics
- validation ownership
- lookup or reference catalog endpoints
- catalog versioning, freshness, and offline rules
- exact upload binding between report and `UploadReceipt`
- auth, session, device, and group scope

## What must wait for coder 2
- promotion of generic form controls to `App.UI.Shared`
- promotion of lookup selector UI to `App.UI.Shared`
- shared visual language for forms
- shared status and error components
- web compatibility of report form components

## What is not allowed
- no hardcoded final lookup dictionaries
- no backend contract invention
- no shared DTO/contracts changes
- no `App.UI.Shared` expansion
- no production upload flow from report draft
- no `PreUploadCheck`
- no `UploadReceipt`
- no fake `businessObjectKey`
- no local draft id as `businessObjectKey`
- no lookup/filter/profile UX in this baseline
- no direct incident, fraud, or worker logic in mobile

## Current baseline replay
- `MOB-CANON-REPORT-00` aligns the local report-first direction.
- `MOB-CANON-REPORT-01` adds the local FPV report draft shell.
- `MOB-CANON-REPORT-02` moves media attachment UX into `ReportDraft`.
- `MOB-CANON-REPORT-03` makes report attachments duplicate-safe and queueable.
- Local queue action is not backend report save and does not invent `businessObjectKey`.
