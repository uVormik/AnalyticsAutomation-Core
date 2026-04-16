# Shared contracts v0

## Status
- Stage: Sprint 0 / S0-05
- Purpose: frozen baseline before deep web/mobile integration
- Change policy: additive-first only
- Breaking changes require separate coordination and explicit PR note

## Scope
This baseline freezes the shared DTO and enum surface for:
- auth/session
- user summary
- group tree
- device registration
- upload precheck
- upload receipt
- download intent
- download receipt
- pending sync
- duplicate/fraud incident summary
- common error contract

## Namespaces included
- BuildingBlocks.Contracts.Common
- BuildingBlocks.Contracts.Auth
- BuildingBlocks.Contracts.Groups
- BuildingBlocks.Contracts.Devices
- BuildingBlocks.Contracts.VideoUpload
- BuildingBlocks.Contracts.VideoDownload
- BuildingBlocks.Contracts.VideoSync
- BuildingBlocks.Contracts.Incidents

## Notes
- DTOs are intentionally minimal and versionable.
- Unknown future fields must be added additively.
- Nullable fields in download/auth contracts are intentional until deeper integration hardens.
- Duplicate and fraud statuses are frozen by enum names first, while internal scoring can evolve later.

## Frozen enums
- DevicePlatform
- UploadPrecheckDecision
- DuplicateMatchKind
- PendingSyncItemType
- PendingSyncItemStatus
- IncidentType
- IncidentStatus
- IncidentSeverity

## Consumer expectation
- App.Api owns these contracts.
- App.Web and App.Mobile.Android may reference these contracts as baseline.
- App.Worker uses the same enums/DTOs when queue or receipt flow needs shared payload shapes.

## Next coordination point
After this baseline is merged:
- coder 2 can bind shell/web flows to stable DTO names
- coder 3 can bind mobile shell/offline flow to the same DTO names
- deeper auth/site integration may extend the payloads only additively