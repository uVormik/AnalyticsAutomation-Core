# S0-14 backend freeze summary

## Status
- Sprint 0 backend freeze point is reached.
- This summary is valid after S0-05, S0-08, S0-09, S0-10, S0-11 and S0-13.

## Frozen shared contracts baseline
- Common API envelopes and error contract baseline
- Auth/session DTO baseline
- Group tree DTO baseline
- Device registration DTO baseline
- Video upload / download / sync / incidents DTO baseline v0
- WebsiteIntegration DTO baseline

## Frozen backend endpoints
- GET /health/live
- GET /health/ready
- GET /api/system/version
- GET /api/system/platform-foundation
- GET /api/system/observability
- GET /api/system/site-gateway
- POST /api/system/site-gateway/reconcile-preview
- POST /api/auth/sign-in
- POST /api/auth/refresh
- GET /api/group-tree/nodes
- GET /api/group-tree/routing-preview
- POST /api/devices/register

## Frozen feature flags
- Modules.Auth.DevelopmentBootstrapEnabled
- Modules.Devices.DeviceRegistrationEnabled

## Known internal/runtime baseline
- ValidateOnStart modular options
- correlation id middleware and audit foundation
- known internal event baseline currently surfaced in system diagnostics:
  - DeviceRegistrationUpsertedInternalEvent

## Frozen site stub expectations
- provider = Stub
- mode = Development
- externalVideoId format = site-video-<guid>
- storageKey format = videos/<externalVideoId>.mp4
- known statuses:
  - uploaded
  - available
  - deleted

## Known stubs / not production-like yet
- no PreUploadCheck runtime endpoint yet
- no UploadReceipt runtime endpoint yet
- no CreateDownloadIntent runtime endpoint yet
- no DownloadReceipt runtime endpoint yet
- no real sync runtime path yet
- no duplicate/fraud runtime incidents API for UI binding yet
- website integration is stub-only, not a live site provider

## What may still change
- Sprint 1 runtime endpoints
- duplicate/fraud payload details
- site gateway real provider implementation
- download control plane payload expansion
All such changes must stay additive-first and be coordinated through coder 1.

## Coordination rule
- coder 2 and coder 3 may rely on this baseline for shell/deep integration preparation
- they must not invent new shared DTO or statuses without coordination
- production-like upload binding starts only after Sprint 1 backend handoff