# MOB-CANON-BACKEND-01 Android Business Object Binding Blocker

## Blocker summary
Android upload control-plane work still lacks an approved source for `businessObjectKey` / report draft / business object binding before `PreUploadCheck`.

## Approved source search result
No approved Android source was found in the reviewed repo docs, task cards, handoffs, ops docs, root architecture docs, or TEAM COORDINATION LOG issue #89 latest entries.

Documents confirm `businessObjectKey` is required for upload metadata, and some smoke/support docs show non-secret test values, but those do not approve a production Android source.

## Runtime scope
This slice adds only an Android-local unresolved guard:
- local intent state
- explicit unresolved binding snapshot
- PreUploadCheck eligibility gate that blocks production precheck while no approved key exists
- Upload page blocker card

No runtime backend integration is added.
No `PreUploadCheck` request is constructed or sent.
No `UploadReceipt` flow is added.
No fake `businessObjectKey` is generated.

## Coordination question
What is the approved Android source for businessObjectKey before PreUploadCheck?

Expected answer category:
1. draft/create-select endpoint first
2. existing catalog/entity endpoint
3. temporary approved non-production stub
4. another approved source
