# S2-39 Deploy Trigger Reconciliation

## Purpose

This note records the confirmed mismatch between the expected Korobochka deploy trigger model and the actual current-main workflow behavior. It is docs-only and does not change workflow code or execute deploy.

## S2-42 outcome

S2-42 selected Option B: formalize the manual-only Korobochka deploy policy.

Canonical policy after S2-42:

- merge to `main` fixes code in the repository and runs the repo CI/coordination flow;
- Korobochka deploy is not triggered automatically by merge or push to `main`;
- Korobochka deploy is executed manually through GitHub Actions `workflow_dispatch` in `.github/workflows/deploy-korobochka.yml`;
- after a manual deploy, operators must verify health, version, and smoke checks;
- push-to-main auto-deploy remains disabled until a separate PR and explicit owner approval.

No workflow YAML change is part of S2-42.

## Actual current state

- `.github/workflows/deploy-korobochka.yml` on current `main` contains only `workflow_dispatch`.
- `b377e59` did not receive a deploy run immediately after merge to `main`.
- An explicit manual dispatch was then executed successfully.
- Korobochka version afterward matched `b377e59`.
- The deploy workflow therefore behaves as manual-only today.

## Source inputs behind the original S2-39 mismatch

- `.github/workflows/deploy-korobochka.yml` shows a manual-only trigger model on current `main`.
- At the time of S2-39, `docs/ops/chatgpt-project-instructions.txt` still described the server-change flow as `branch -> PR -> merge to main -> GitHub Actions deploy on Korobochka`.
- At the time of S2-39, `docs/ops/korobochka-chatgpt-context.md` explicitly said auto-deploy on `push` to `main` was not enabled, but it also contained a generic `merge -> deploy` flow description.
- S2-42 updates the active docs/context wording to manual-only.

## Confirmed mismatch

Expected trigger model:

- deploy on `push` to `main`;
- deploy on manual `workflow_dispatch`.

Actual trigger model:

- deploy on manual `workflow_dispatch` only;
- merge to `main` alone does not create a deploy run.

Operational evidence:

- no deploy run was found for `b377e59` until explicit dispatch;
- explicit dispatch succeeded;
- server version then became `b377e59`.

## Decision model

The reconciliation decision is now made by S2-42. Option B is canonical.

### Historical Option A: restore push-to-main deploy trigger

- Add `push` on `main` back to `.github/workflows/deploy-korobochka.yml`.
- Keep manual `workflow_dispatch`.
- Do not add deploy from `pull_request`.

### Selected Option B: formalize manual-only deploy policy

- Keep `.github/workflows/deploy-korobochka.yml` manual-only.
- Update all operational docs/context so merge to `main` is never described as an automatic deploy trigger.

## Recommendation

- Keep one canonical deploy trigger model and document it consistently in operational docs.
- Do not leave a split state where operators expect post-merge deploy but the workflow is manual-only.
- Manual dispatch is required after merge when a Korobochka deploy is needed.

## Decision criteria

- Release safety.
- Operational predictability.
- Not deploying untrusted pull requests.
- Consistency with docs/context.
- Minimal ambiguity for maintainers and future task execution.

## Security guardrail

- Do not run the Korobochka self-hosted runner on untrusted pull requests.
- Do not enable deploy from `pull_request` as part of the reconciliation fix.

## S2-42 implementation step

Open one narrow docs/process PR that keeps the workflow YAML unchanged and updates only the operational docs/context so the canonical policy is manual-only.

No runtime API, contracts, migrations, or deploy execution should be part of that step.

## Non-goals of this note

- No workflow behavior change in this PR.
- No deploy execution in this PR.
- No runtime code, contracts, migrations, or secrets handling changes.
