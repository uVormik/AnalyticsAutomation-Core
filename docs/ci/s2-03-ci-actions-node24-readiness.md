# S2-03 CI Actions Node 24 Readiness

Status: Draft
Owner: Coder 1 / Platform Owner
Module: GitHub Actions / CI / Platform
Type: CI maintenance / platform readiness

## Goal

Remove GitHub Actions Node.js 20 deprecation risk from the CI pipeline while keeping the current CI behavior unchanged.

## Context

After S2-02 was merged, manual workflow_dispatch CI on main completed successfully.

CI annotations reported that Node.js 20 actions are deprecated and that actions such as actions/checkout@v4 may require updates before GitHub-hosted runner defaults change.

## What changes

- backend: no runtime code change.
- web: no change.
- mobile: no change.
- worker: no runtime code change.
- db: no migration.
- ci: verify and update GitHub Actions versions only where needed.
- docs: document the maintenance scope and validation evidence.

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
No secrets change.

## Observability

CI logs must clearly show restore, format, build, unit-tests and integration-tests.
No new runtime observability hooks.

## Rollback

Rollback by reverting the PR.
No database rollback.
No shared contract rollback.
No production deploy included.

## Definition of Done

- current action versions in .github/workflows are inventoried;
- Node 24-compatible replacement versions are verified before update;
- CI behavior remains the same:
  - restore;
  - format;
  - build;
  - unit-tests;
  - integration-tests;
- PR explains:
  - module: GitHub Actions / CI;
  - contracts: none;
  - migration: none;
  - feature flag: none;
  - rollback: revert PR;
  - production deploy: not included.
