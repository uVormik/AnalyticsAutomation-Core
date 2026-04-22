# C2 S2-18 Web Upload Control Plane Handoff

Status: local increment ready for PR readiness.

Branch:
feature/web-upload-control-plane-s2-18

Owner:
Coder 2 / Web

## Goal

Implement Web/PWA upload control plane baseline:

sign-in -> group tree -> pre-upload check -> local direct-site stub boundary -> upload receipt.

## What changed

- Added web-only upload control plane client adapter baseline:
  - sign-in endpoint constant
  - refresh endpoint constant
  - group tree nodes endpoint constant
  - routing preview endpoint constant
  - in-memory session store
  - sanitized session shape
  - token/body redaction helper
  - HttpUploadControlPlaneApi
- Made HttpVideoUploadApi session-aware:
  - PreUploadCheck adds Authorization header when an in-memory session exists.
  - UploadReceipt adds Authorization header when an in-memory session exists.
  - error body text is redacted before surfacing.
- Extended /upload UI:
  - sign-in form
  - sanitized session summary
  - group tree load
  - group node selection
  - pre-upload check
  - local direct-site stub boundary
  - upload receipt boundary
- Added explicit local/client direct-site stub:
  - does not call App.Api
  - does not send video bytes through App.Api
  - produces deterministic external video id / storage key / site status for UploadReceipt boundary
- Added App.Web unit tests for:
  - control plane endpoint constants
  - redaction
  - in-memory session store
  - session factory
  - bearer header behavior in HttpVideoUploadApi
  - local direct-site stub behavior

## What did not change

- No backend route changes.
- No App.Api changes.
- No shared DTO / BuildingBlocks.Contracts changes.
- No database migration.
- No Android code.
- No deploy workflow changes.
- No SSH or server-side operation.
- No production direct-site provider implementation.
- No video byte proxy through App.Api.

## Security and redaction

Password is not stored in source code or docs.

Token values must not be logged or pasted into GitHub / ChatGPT / screenshots.

The UI displays only sanitized session metadata:
- user id
- display name
- access token available: true/false
- refresh token available: true/false

Error bodies are sanitized with UploadControlPlaneErrorRedactor before being surfaced.

## Manual smoke notes

Live smoke was not executed by this local code step.

When owner approves live smoke, use only local browser interaction and do not paste secrets into logs.

Suggested sanitized smoke path:

1. Run App.Web locally.
2. Open /upload.
3. Enter integration login manually.
4. Enter password manually from out-of-band source.
5. Submit sign-in.
6. Confirm session summary appears and no token values are displayed.
7. Load group tree.
8. Select a group node and confirm GroupNodeId is copied into the upload form.
9. Fill local metadata for pre-upload check:
   - user id
   - device id
   - group node id
   - business object key
   - file name
   - size bytes
   - byte sha256
   - content type
   - captured at UTC
10. Run PreUploadCheck.
11. Confirm the backend decision is displayed.
12. Run local direct-site upload stub boundary.
13. Confirm the message explicitly says no video bytes were sent through App.Api.
14. Submit UploadReceipt.
15. Submit UploadReceipt again without clearing result if idempotency behavior needs manual confirmation.
16. Do not paste token values or password into notes.

## Expected PR scope

Allowed changed areas:
- docs/task-cards/C2-S2-18_web-upload-control-plane-integration.txt
- docs/handoffs/C2_S2_18_WEB_UPLOAD_CONTROL_PLANE_HANDOFF.md
- src/App.Web/**
- tests/Unit/App.Web.Tests/**

Disallowed changed areas:
- src/App.Api/**
- src/Modules/**
- src/BuildingBlocks/Contracts/**
- src/App.Mobile.Android/**
- .github/workflows/**
- infra/**
- DB migrations

## Local validation

Expected commands for this Web-only branch on a machine without MAUI workload:

dotnet restore src/App.Web/App.Web.csproj
dotnet restore tests/Unit/App.Web.Tests/App.Web.Tests.csproj
dotnet format whitespace src/App.Web/App.Web.csproj --verify-no-changes --no-restore
dotnet format whitespace tests/Unit/App.Web.Tests/App.Web.Tests.csproj --verify-no-changes --no-restore
dotnet build src/App.Web/App.Web.csproj
dotnet build tests/Unit/App.Web.Tests/App.Web.Tests.csproj
dotnet test tests/Unit/App.Web.Tests/App.Web.Tests.csproj --no-build
git diff --check

Solution-wide restore/format is intentionally not required on this local machine because App.Mobile.Android requires the maui-android workload and Android is out of scope for S2-18 Web.