# S2-53 Operator Admin Password Recovery Prompt

Use this prompt for a future runtime implementation chat/Codex session that adds a safe operator-controlled admin password recovery or additional-admin maintenance command.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Task:
Implement S2-53 Operator Admin Password Recovery.

Working repository:
Use the active dedicated S2-53 runtime worktree provided by the operator.

Branch/scope:
- Work only under a dedicated S2-53 runtime branch.
- Inspect the existing code first and choose the smallest safe implementation.
- Prefer extending `tools/App.Maintenance` rather than App.Api.
- Expected writable paths:
  - `tools/App.Maintenance/**`
  - `tests/Integration/App.Maintenance/**`
- Treat these docs as source context, not runtime write scope:
  - `docs/task-cards/S2-53_operator-admin-password-recovery-task-card.txt`
  - `docs/handoffs/S2_53_OPERATOR_ADMIN_PASSWORD_RECOVERY_PROMPT.md`
- Do not change App.Api unless a later separate task explicitly approves an API route.
- Do not change App.Desktop except through a later separate task.
- Do not change App.Web, App.Mobile.Android, App.UI.Shared, App.Worker, BuildingBlocks.Contracts, deploy scripts, workflows, or unrelated modules.
- Do not add DB migrations unless a separate additive-first migration task is created and approved.

Sync-readiness first:
1. Verify the active repository root and current working directory.
2. Run `git branch --show-current`.
3. Run `git status --short --untracked-files=all`.
4. Run `git fetch origin --prune`.
5. Run `git switch main`.
6. Run `git pull --ff-only origin main`.
7. Read TEAM COORDINATION LOG issue #89 with comments.
8. Review the latest relevant coordination log entries, including at least:
   - PR #117 S2-51 Desktop SignIn Runtime Enablement Task Card
   - PR #118 S2-51 desktop signin runtime enablement
   - PR #119 S2-52 identity bootstrap first admin task card
   - PR #120 S2-52 identity bootstrap first admin runtime
   - any later merge entries
9. Return to the dedicated S2-53 runtime branch only after sync-readiness is complete.
10. If `origin/main` changed in a way that makes the current worktree stale, stop and report the blocker.

Required source review before coding:
- `00_START_HERE_README.txt`
- `01_MASTER_PLAN_AND_ARCHITECTURE.txt`
- `02_SHARED_RULES_AND_PROTOCOLS.txt`
- `04_GITHUB_WORKFLOW_AND_INFRA_RULES.txt`
- `docs/task-cards/S2-51_desktop-signin-runtime-enablement-task-card.txt`
- `docs/handoffs/S2_51_DESKTOP_SIGNIN_RUNTIME_ENABLEMENT_PROMPT.md`
- `docs/task-cards/S2-52_identity-bootstrap-first-admin-task-card.txt`
- `docs/handoffs/S2_52_IDENTITY_BOOTSTRAP_FIRST_ADMIN_PROMPT.md`
- `docs/task-cards/S2-53_operator-admin-password-recovery-task-card.txt`
- `docs/handoffs/S2_53_OPERATOR_ADMIN_PASSWORD_RECOVERY_PROMPT.md`
- `tools/App.Maintenance/**`
- `tests/Integration/App.Maintenance/**`
- `src/Modules/Auth/**` read-only as needed to confirm password hashing/auth ownership
- `src/BuildingBlocks/Infrastructure/Persistence/**` read-only as needed to confirm entities, roles, group-admin assignments, and audit records
- `src/App.Api/**` read-only only if needed to confirm the existing sign-in contract and audit pattern

Must confirm before coding:
- S2-51 desktop SignIn runtime is merged and deployed.
- S2-52 first-admin bootstrap runtime is merged and deployed.
- Manual bootstrap on Korobochka returned `status: skipped_existing_admin` for `incident-routing-admin`.
- That result means an active root/platform admin already exists and S2-52 correctly refused to overwrite it.
- Manual desktop SignIn still needs a known valid password.
- Do not assume the password is known.
- Do not ask for passwords, tokens, connection strings, or secrets.
- Open desktop self-registration is forbidden.
- Manual DB edits are forbidden.

Goal:
Add the smallest safe operator-controlled maintenance path that lets an operator on Korobochka either:
- reset or set the password for an existing interactive root/platform admin; or
- create an additional interactive root/platform admin if policy chooses that route.

This is an operator maintenance task, not public registration and not account-management UI.

Preferred implementation shape:
- Extend `tools/App.Maintenance`.
- Add an explicit command such as:
  - `identity-admin reset-password`
  - `identity-admin recover-password`
  - `identity-bootstrap set-admin-password`
  - `identity-admin create-root-admin` if policy chooses the additional-admin route
- The exact command name may be chosen during implementation, but it must be explicit, operator-only, and impossible to confuse with public self-registration.
- Require an explicit enable flag such as `AA_ADMIN_PASSWORD_RECOVERY_ENABLED=true`.
- Read the login from an operator-local environment variable or secure interactive prompt.
- Read the new password from an operator-local environment variable or secure interactive prompt.
- Do not accept passwords as command-line arguments.
- Never print the password or password hash.
- Never print access tokens, refresh tokens, or Authorization headers.
- Redact secret-like values in logs/errors/command output.
- Use existing `IPasswordHasher<AuthUser>` and existing auth persistence conventions.
- Use existing auth/group admin structures for `platform_owner` and root group assignment policy.
- Use existing audit infrastructure where available.
- Use transactions for database writes and ensure partial failures do not leave inconsistent state.

Required behavior:
- The command defaults to disabled.
- The command fails without the explicit enable flag.
- The command fails safely when login is missing.
- The command fails safely when password is missing or interactive input is cancelled.
- Reset-password mode fails safely when the target account does not exist.
- Reset-password mode validates the target account is interactive, not a service/integration account.
- Reset-password mode validates the target account is an active root/platform admin unless the approved policy says otherwise.
- Additional-admin mode, if implemented, creates or updates only an interactive root/platform admin and must be explicit about that policy route.
- All write operations are transactional where supported.
- Audit records/events capture attempt/result, target login or safe identifier, mode, and outcome without secrets.
- Command output reports safe status values only.
- The resulting admin can authenticate through the existing `/api/auth/sign-in` path after operator deploy/smoke.

Forbidden behavior:
- No public self-registration.
- No App.Desktop user creation.
- No account-management UI.
- No manual DB edits.
- No public API route unless a separate task explicitly approves it before coding.
- No hidden API route inside S2-53.
- No shared DTO/contract changes unless a separate task explicitly approves them.
- No command-line password arguments.
- No password, password hash, access token, refresh token, Authorization header, connection string, or secret in logs, exceptions, stdout/stderr, tests, docs, reports, PR body, or coordination comments.
- No plaintext credential persistence.
- No changes to App.Web, App.Mobile.Android, App.UI.Shared, App.Worker, workflows, deploy scripts, or unrelated runtime.
- No DB migration by default.

Feature flag / options / events:
- Use an operator-local environment gate and/or existing feature flag pattern.
- The gate must default to disabled.
- Recommended gate name: `AA_ADMIN_PASSWORD_RECOVERY_ENABLED`.
- Record observability/audit outcomes.
- No public API route is required.
- If an API route is proposed, stop and create a separate task. Do not hide it inside S2-53.

Migrations:
- Default expectation: no migration.
- If implementation discovers a schema gap, stop and create a separate additive-first migration task.
- Do not add migrations inside S2-53 without explicit separate approval.

Offline behavior:
- N/A for client offline mode.
- The operator maintenance command runs online against the Korobochka database.

Rollback:
- Future runtime rollback should revert the maintenance command PR.
- A password already changed by operator action cannot be blindly rolled back without another operator action.
- The PR must say that reversing a password change requires a new approved operator action.

Required tests:
- success path for resetting an existing interactive root/platform admin password;
- success path for additional interactive root/platform admin creation if that policy route is implemented;
- disabled enable flag;
- missing login;
- missing password or cancelled secure prompt;
- nonexistent account for reset-password mode;
- service/integration account rejection;
- non-root/non-platform-admin rejection if reset-password is limited to root/platform admins;
- root/platform-admin role and group-admin assignment validation;
- audit record/event for attempt and result without secrets;
- no password, password hash, access token, refresh token, Authorization, or connection string logging;
- idempotent behavior;
- transaction safety when a write fails after partial validation;
- secret-safe command output and error output.

Validation expectations:
- `git diff --name-only`
- `git diff --check`
- `dotnet restore AnalyticsAutomation-Core.sln -nologo`
- `dotnet format whitespace AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- `dotnet build tools/App.Maintenance/App.Maintenance.csproj -nologo -v minimal`
- `dotnet test tests/Integration/App.Maintenance/App.Maintenance.Tests.csproj -nologo -v minimal`
- `dotnet test AnalyticsAutomation-Core.sln -nologo -v minimal` if feasible
- Confirm no App.Api change unless separately approved.
- Confirm no public registration.
- Confirm no direct DB edits.
- Confirm no contracts changes.
- Confirm no migrations.
- Confirm no deploy/workflow changes.
- Confirm no App.Desktop, App.Web, App.Mobile.Android, App.UI.Shared, or App.Worker changes unless separately approved.
- Confirm no passwords/tokens/Authorization/connection strings were printed, logged, committed, or included in PR/report text.
- No deploy in PR.
- After merge/deploy, live smoke only through explicit operator-approved step.

PR body requirements:
- Coder
- Task ID
- Module
- Scope
- Changed areas
- Base main SHA
- Coordination log entries reviewed
- Handoff/task cards reviewed
- Contracts
- Migrations
- Feature flags
- API impact
- Web impact
- Desktop impact
- Android impact
- Offline behavior
- Rollback
- Validation
- Handoff docs
- Next step
- NO-GO / Deferred

Required final output:
- Exact files changed.
- Exact checks run and result of each check.
- Selected implementation command name and policy mode.
- Whether App.Maintenance changed.
- Whether App.Api changed.
- Whether Modules/Auth changed.
- Whether BuildingBlocks.Contracts changed.
- Whether migrations changed.
- Whether feature flags/options changed.
- Whether audit/security events are written.
- Confirmation this is not public self-registration.
- Confirmation App.Desktop does not create users.
- Confirmation there were no direct DB edits.
- Confirmation no secrets/passwords/tokens/Authorization/connection strings were added to files, logs, PR body, report, or coordination comments.
- Manual smoke result or blocker.
- Rollback instructions.
- Next step.

Stop conditions:
- If the task appears to require public self-registration, stop.
- If App.Desktop would need to create users directly, stop.
- If manual DB edits appear necessary, stop.
- If a public API route seems necessary but lacks separate explicit approval, stop.
- If password handling would require command-line password arguments, plaintext persistence, logging, or docs/PR disclosure, stop.
- If service/integration account detection cannot be made safely, stop and request a policy decision.
- If root/platform-admin policy cannot be validated from existing auth/group-admin structures, stop.
- If a schema gap appears, stop and create a separate additive-first migration task.
- If a non-additive migration appears necessary, stop.
- If the implementation would require broad RBAC/account-management redesign, stop.
