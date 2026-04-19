# S2-02 CI Integration Test Discovery Hardening

Status: Draft
Owner: Coder 1 / Platform Owner
Module: GitHub Actions / CI / Platform
Type: CI hardening

## Goal

Ensure GitHub Actions CI actually discovers and runs existing integration test projects instead of reporting a green integration-tests job with "No integration tests yet".

## Context

After S2-01 was merged to main, manual workflow_dispatch CI on main completed successfully.
However, the integration-tests job reported "No integration tests yet" even though the repository contains integration test projects under tests/Integration.

This creates a false-green CI risk.

## What changes

- backend: no runtime code change.
- web: no change.
- mobile: no change.
- worker: no runtime code change.
- db: no migration.
- ci: update integration test project discovery in GitHub Actions.
- docs: document the CI hardening scope.

## Contracts

No shared DTO changes.
No API route changes.
No response payload shape changes.
No enum/status changes.

## Migration

Not needed.

## Feature flag

Not needed.

## Events

No new events.

## Offline behavior

No offline behavior change.

## Security

No auth/authz behavior change.

## Observability

CI logs must clearly show:
- which integration test projects were discovered;
- which projects were executed;
- explicit failure if discovery is expected but no test projects are found.

## Rollback

Rollback by reverting the PR.
No database rollback.
No shared contract rollback.
No production deploy included.

## Definition of Done

- CI integration-tests job discovers existing tests/Integration/*.csproj projects.
- CI runs integration tests instead of silently printing "No integration tests yet".
- Local validation confirms the discovery command finds current integration test projects.
- PR explains:
  - module: GitHub Actions / CI;
  - contracts: none;
  - migration: none;
  - feature flag: none;
  - rollback: revert PR;
  - production deploy: not included.
## Investigation update

Manual main CI investigation showed that integration test discovery works.
The real defect is false-green behavior: `dotnet test` can fail inside the PowerShell foreach loop while the GitHub Actions step still completes successfully.

Observed case:
- `tests/Integration/App.Api/App.Api.IntegrationTests.csproj` was discovered and executed.
- Both App.Api host tests failed in CI with 500 responses.
- The integration-tests job continued to the next projects and ended as success.

S2-02 therefore hardens both:
- test failure propagation in CI loops;
- App.Api host test isolation from external database availability.
## Auth handler follow-up

PR CI also proved that App.Api host tests must not require database resolution for anonymous requests.

Auth hardening update:
- opaque bearer handler no longer constructor-injects PlatformDbContext;
- PlatformDbContext is resolved only after a non-empty Bearer token is present;
- anonymous health and anonymous authorization-challenge paths stay independent from database availability.