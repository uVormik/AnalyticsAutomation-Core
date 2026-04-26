# Coder 3 Report-First Mobile Plan

## Current replay state
- REPORT-00..03 local report-first baseline is in main from PR #109.
- REPORT-04 is the current local field editing and selector prototype slice.
- Production backend report flow is not implemented.

## Source-of-truth boundary
- TEAM COORDINATION LOG #89 records that Android must obtain `businessObjectKey` from a backend-controlled report/business-object binding source before production `PreUploadCheck`.
- Concrete backend source/endpoint/contract remains undocumented.
- Local draft ids, file hashes, group ids, titles, and UI-only selections are not `businessObjectKey`.

## Local-only plan
- Keep local report draft UX replay separate from backend integration.
- Keep field editing in Android-local in-memory state for REPORT-04.
- Keep selector values as local stub options only until approved lookup/catalog source is documented.
- Keep production `PreUploadCheck`, `UploadReceipt`, direct upload, and backend save blocked.

## Next slices
- REPORT-05 can add local validation summary and ready-to-queue gating.
- REPORT-06 persistence and REPORT-07 create-from-last remain out of this slice.
- Backend runtime waits for a concrete backend-owned business object/report binding source.
