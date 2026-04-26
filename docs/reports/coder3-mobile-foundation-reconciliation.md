# Coder 3 Mobile Foundation Reconciliation

## Purpose
Track the controlled mobile foundation replay into the canonical repository without importing broad local-only work.

## Canonical App.Mobile.Android current state
The canonical repository contains the complete IMPORT-01..07 Android media/outbox foundation chain in `main`:
- Shell, navigation, Russian visible baseline, and local shell-state stubs are present from MOB-CANON-IMPORT-01 / PR #67.
- Android native video picker and native camera capture baseline are present from MOB-CANON-IMPORT-02 / PR #95.
- Local selected-media descriptor/cache and the selected local media card are present from MOB-CANON-IMPORT-02 / PR #95.
- Local in-memory outbox foundation, queue rendering, and retry/remove local actions are present from MOB-CANON-IMPORT-03 / PR #96.
- Selected-media to outbox draft handoff and media-linked queue details are present from MOB-CANON-IMPORT-04 / PR #97.
- Local duplicate-precheck and duplicate-aware handoff behavior are present from MOB-CANON-IMPORT-05 / PR #98.
- Restart-resilience JSON metadata snapshots are present from MOB-CANON-IMPORT-06 / PR #99.
- Local repair/rebind is present from MOB-CANON-IMPORT-07 / PR #100.
- BACKEND-01 unresolved business-object binding guard is present from PR #108.
- `App.UI.Shared` was intentionally unchanged by IMPORT-02..07 and BACKEND-01.

## Current local UX slice
`MOB-CANON-REPORT-00-03` replays the local report-first baseline as an Android-local UX slice.

Physical Android runtime check is passed for REPORT-00-03:
- `Полеты` route/list works.
- create FPV draft works.
- report draft opens.
- media can be selected/captured inside draft.
- duplicate same-video attachment is blocked.
- draft can be placed into local queue.
- no fake `businessObjectKey` is shown.
- production `PreUploadCheck` remains blocked.

Report-first baseline means:
- local FPV report draft shell
- report route/list and create draft action
- media selection/capture inside report draft
- duplicate-safe attachment behavior inside draft
- local report draft queue action
- Queue rendering for local report draft pending items
- no backend save
- no sync
- no upload
- no production reports engine
- no lookup/filter/profile UX

## BusinessObject and backend boundary
- TEAM COORDINATION LOG #89 records the Android `businessObjectKey` decision:
  https://github.com/uVormik/AnalyticsAutomation-Core/issues/89#issuecomment-4321720298
- Production direction is Option 1.
- Android must obtain `businessObjectKey` from a backend-controlled report/business-object binding source before `PreUploadCheck`.
- The concrete backend source/endpoint/contract remains undocumented.
- Backend integration remains blocked pending that concrete backend source/endpoint/contract.
- Local report draft state is not backend-recognized binding.
- Local report draft id is not `businessObjectKey`.
- Local queue action is not backend report save.

## Gap matrix
| Capability | Exists in canonical main | Current slice changes | Later slice needed | Notes |
| --- | --- | --- | --- | --- |
| MAUI shell | yes | no | no | Present from PR #67 |
| mobile navigation/menu | yes | report route/list baseline | later polish possible | Report-first entry point remains Android-local |
| native video picker | yes | used inside draft | no for baseline | Present from PR #95 |
| native camera capture | yes | used inside draft | no for baseline | Device capability still applies |
| selected media cache | yes | feeds report attachment | later persistence possible | Temporary Android-local state |
| local outbox foundation | yes | report draft queue action | later backend sync | No upload/runtime integration |
| selected media to outbox handoff | yes | reinterpreted under report draft | later production binding | Attachment flow only |
| local duplicate-precheck | yes | duplicate-safe draft attachment | later backend dedupe | Local-only warning/guard |
| restart snapshots | yes | no new persistence claim | later policy | Existing metadata snapshots only |
| local repair/rebind | yes | remains intact | later polish possible | Existing repair flow retained |
| BACKEND-01 blocker guard | yes | remains intact | backend contract needed | No fake `businessObjectKey` |
| local report draft shell | no | yes | later UX slices | Local-only baseline |
| production report flow | no | no | yes | Requires backend source/contract |
| production PreUploadCheck | no | no | yes | Blocked pending backend source |

## Recommended reconciliation strategy
Continue with small PR-ready slices from fresh `main`.

This slice is allowed because the source-of-truth decision permits local report/business-object UX replay and unresolved seams while production `PreUploadCheck` remains blocked.

Backend adapter runtime should wait for the approved concrete Android source of `businessObjectKey` / report draft / business object binding before any production `PreUploadCheck` runtime.

## Not allowed
- no backend changes
- no shared DTO/contracts changes
- no App.UI.Shared changes
- no App.Api changes
- no Modules changes
- no DB migrations
- no workflows/deploy changes
- no invented create-report API
- no fake `businessObjectKey`
- no local draft id as `businessObjectKey`
- no production `PreUploadCheck`
- no `UploadReceipt`
- no direct upload runtime
- no desktop runtime changes
