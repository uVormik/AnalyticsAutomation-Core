# S2-04 Korobochka Deploy Workflow Activation

Status: Draft
Owner: Coder 1 / Platform Owner
Module: GitHub Actions / Deploy / Korobochka
Type: infra / deployment workflow hardening

## Goal

Introduce a clean, reviewed Korobochka deploy workflow from current main without merging the stale remote branch `origin/chore/korobochka-auto-deploy`.

## Context

Audit found an existing remote branch:

- `origin/chore/korobochka-auto-deploy`

That branch is stale and includes unrelated old S2-02 changes.
It must not be merged as-is.

The only intended unique artifact is:

- `.github/workflows/deploy-korobochka.yml`

The workflow content uses:

- `workflow_dispatch`
- `push` to `main`
- self-hosted runner labels
- `korobochka`
- `linux`
- `sudo /usr/local/bin/v1-deploy`

No secrets, passwords, tokens, private keys, or env files are expected in the workflow.

## What changes

- backend: no runtime code change.
- web: no change.
- mobile: no change.
- worker: no runtime code change.
- db: no migration.
- ci/deploy: planned follow-up PR may add `.github/workflows/deploy-korobochka.yml` from a clean branch.
- docs: this task card documents scope, risk, rollback, and decision boundary.

## Contracts

No shared DTO changes.
No API route changes.
No response payload shape changes.
No enum/status changes.

## Migration

No database migration.

## Feature flag

No application feature flag.

Deploy activation is controlled by GitHub Actions workflow triggers and branch protection.

## Events

No internal application events.

## Offline behavior

No offline behavior change.

## Security

Requirements:

- do not commit secrets;
- do not commit runner token;
- do not commit private SSH keys;
- do not commit `/opt/v1-pyton/secrets`;
- workflow must run only on trusted `main` / manual dispatch, not on untrusted pull_request execution;
- use self-hosted runner labels to target Korobochka only;
- use existing `sudo /usr/local/bin/v1-deploy` entrypoint.

## Observability

Deploy workflow must show:

- runner identity check;
- deploy command execution;
- deploy result;
- post-deploy health/smoke check in the same workflow or a follow-up mandatory step.

Minimum smoke target:

- `/health/ready`
- `/api/system/version`

## Rollback

Rollback by reverting the deploy workflow PR.

Operational rollback remains the existing Korobochka rollback mechanism through `v1-deploy` / release symlink / documented runbook.

No database rollback is introduced by this task card.

## Decision boundary

This task card does not add the deploy workflow yet.
This task card must not trigger deployment.

The follow-up implementation PR must explicitly state whether `push` to `main` deployment is enabled immediately or whether the first version is `workflow_dispatch` only.

## Definition of Done

- stale remote branch is not merged;
- clean branch from current `main` is used for implementation;
- workflow file contains no secrets;
- workflow uses the Korobochka self-hosted runner labels;
- workflow behavior is documented in PR;
- PR passes CI;
- production/stage deployment behavior is explicitly approved before merge if `push: main` trigger is enabled.
