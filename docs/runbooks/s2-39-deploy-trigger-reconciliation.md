# S2-39 Deploy Trigger Reconciliation

## Purpose

This note records the confirmed mismatch between the expected Korobochka deploy trigger model and the actual current-main workflow behavior. It is docs-only and does not change workflow code or execute deploy.

## Actual current state

- `.github/workflows/deploy-korobochka.yml` on current `main` contains only `workflow_dispatch`.
- `b377e59` did not receive a deploy run immediately after merge to `main`.
- An explicit manual dispatch was then executed successfully.
- Korobochka version afterward matched `b377e59`.
- The deploy workflow therefore behaves as manual-only today.

## Source inputs behind the mismatch

- `.github/workflows/deploy-korobochka.yml` shows a manual-only trigger model on current `main`.
- `docs/ops/chatgpt-project-instructions.txt` still describes the server-change flow as `branch -> PR -> merge to main -> GitHub Actions deploy on Korobochka`.
- `docs/ops/korobochka-chatgpt-context.md` explicitly says auto-deploy on `push` to `main` is not enabled, but it also contains a generic `merge -> deploy` flow description.

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

## Desired decision for the next implementation step

The next implementation PR must choose exactly one canonical deploy trigger model.

### Option A: restore push-to-main deploy trigger

- Add `push` on `main` back to `.github/workflows/deploy-korobochka.yml`.
- Keep manual `workflow_dispatch`.
- Do not add deploy from `pull_request`.

### Option B: formalize manual-only deploy policy

- Keep `.github/workflows/deploy-korobochka.yml` manual-only.
- Update all operational docs/context so merge to `main` is never described as an automatic deploy trigger.

## Recommendation

- Prefer one canonical deploy trigger model and document it consistently in both workflow YAML and operational docs.
- Do not leave a split state where operators expect post-merge deploy but the workflow is manual-only.
- Until the decision is implemented, assume manual dispatch is required after merge.

## Decision criteria

- Release safety.
- Operational predictability.
- Not deploying untrusted pull requests.
- Consistency with docs/context.
- Minimal ambiguity for maintainers and future task execution.

## Security guardrail

- Do not run the Korobochka self-hosted runner on untrusted pull requests.
- Do not enable deploy from `pull_request` as part of the reconciliation fix.

## Exact next implementation step

Open one narrow infra PR that does exactly one of the following:

- Option A: change only `.github/workflows/deploy-korobochka.yml` and directly related operational docs to restore `push` to `main` while keeping `workflow_dispatch`.
- Option B: keep the workflow YAML unchanged and update only the operational docs/context so the canonical policy is manual-only.

No runtime API, contracts, migrations, or deploy execution should be part of that step.

## Non-goals of this note

- No workflow behavior change in this PR.
- No deploy execution in this PR.
- No runtime code, contracts, migrations, or secrets handling changes.
