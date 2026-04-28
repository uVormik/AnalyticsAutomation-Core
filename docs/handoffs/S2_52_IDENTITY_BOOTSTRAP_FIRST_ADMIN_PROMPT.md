# S2-52 Identity Bootstrap / First Admin Prompt

Use this prompt for a future runtime implementation chat/Codex session that creates the first approved account/bootstrap path needed for App.Desktop SignIn smoke.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Task:
Implement S2-52 Identity Bootstrap / First Admin.

Branch/scope:
- Work only under a dedicated S2-52 runtime branch.
- Inspect the existing code first and choose the smallest safe implementation.
- Prefer `Modules.Auth` / existing auth services as the domain authority.
- Prefer existing `tools/App.Maintenance` operator tooling if it can safely support the first-account/first-admin/smoke-account need.
- If no suitable maintenance command exists, you may add a narrowly scoped operator-only maintenance command or equivalent approved bootstrap mechanism.
- Any API endpoint for bootstrap requires explicit approval before implementation. Default preference is not to expose public bootstrap over the network.
- Do not change App.Desktop unless the future task explicitly approves a minimal setup-message UX change.
- Do not change App.Web, App.Mobile.Android, App.UI.Shared, upload/download runtime, workflows, deploy scripts, or unrelated modules.

Sync-readiness first:
1. Verify the active repository root and current working directory.
2. Run `git branch --show-current`.
3. Run `git status --short --untracked-files=all`.
4. Run `git fetch origin --prune`.
5. Run `git switch main`.
6. Run `git pull --ff-only origin main`.
7. Read TEAM COORDINATION LOG issue #89 with comments.
8. Review the latest relevant coordination log entries, including at least:
   - PR #115 S2-50 desktop auth session boundary runtime
   - PR #117 S2-51 Desktop SignIn Runtime Enablement Task Card
   - PR #118 S2-51 desktop signin runtime enablement
   - any later merge entries
9. Return to the dedicated S2-52 runtime branch only after sync-readiness is complete.
10. If `origin/main` changed in a way that makes the current worktree stale, stop and report the blocker.

Required source review before coding:
- `00_START_HERE_README.txt`
- `01_MASTER_PLAN_AND_ARCHITECTURE.txt`
- `02_SHARED_RULES_AND_PROTOCOLS.txt`
- `04_GITHUB_WORKFLOW_AND_INFRA_RULES.txt`
- `docs/task-cards/S2-50_desktop-auth-session-boundary-task-card.txt`
- `docs/handoffs/S2_50_DESKTOP_AUTH_SESSION_BOUNDARY_PROMPT.md`
- `docs/task-cards/S2-51_desktop-signin-runtime-enablement-task-card.txt`
- `docs/handoffs/S2_51_DESKTOP_SIGNIN_RUNTIME_ENABLEMENT_PROMPT.md`
- `docs/task-cards/S2-52_identity-bootstrap-first-admin-task-card.txt`
- `docs/handoffs/S2_52_IDENTITY_BOOTSTRAP_FIRST_ADMIN_PROMPT.md`
- `src/App.Desktop/Services/Auth/DesktopSignInService.cs`
- `src/App.Desktop/Services/Auth/DesktopAuthOptions.cs`
- `src/App.Desktop/Services/Auth/HttpDesktopAuthClient.cs`
- `src/Modules/Auth/**`
- `src/App.Api/**`
- `tools/App.Maintenance/**`, if it exists
- `tests/**` only as needed for existing auth/bootstrap/maintenance patterns

Must confirm before coding:
- App.Desktop SignIn is enabled by S2-51 but needs a provisioned account/password.
- There is no approved open registration flow.
- App.Desktop must not create arbitrary users directly.
- `/api/auth/sign-in` is the existing sign-in contract and must remain backward-compatible.
- `Modules.Auth` owns password verification, session creation, roles/permissions mapping, and token hashing.
- Existing development-only bootstrap behavior is not production/operator bootstrap approval.
- Existing maintenance tooling must be inspected before adding anything new.

Product/security decisions:
- This is not public self-registration.
- App.Desktop must not create arbitrary users directly.
- A first account / first admin / smoke account must be created through an operator-controlled server-side bootstrap path.
- Prefer existing server auth/domain model and existing maintenance tooling if present.
- If no suitable maintenance command exists, add only a narrowly scoped operator-only maintenance command or equivalent approved bootstrap mechanism.
- Any bootstrap API endpoint requires explicit approval; default preference is no public network bootstrap.
- Bootstrap must be safe to disable after use and must not become an ongoing auth bypass.
- Passwords must not be passed in chat, committed to files, printed to logs, or included in PR bodies/reports.
- Prefer interactive secret input or a protected operator-local secret mechanism over command-line password arguments.
- If a DB migration is required, it must be additive-first.
- Bootstrap/user creation must write audit/security events if existing audit/event infrastructure supports it.
- Desktop first-run UX may show a safe message such as: "No account is configured; ask an administrator or run the approved bootstrap procedure."
- No open registration UI is approved by this task.
- Future manual smoke must allow an operator to create/use an account, run App.Desktop against `http://192.168.1.66/`, sign in, and confirm no password/token leakage.

Candidate implementation shape:
- Keep `Modules.Auth` / existing auth services as the domain authority.
- Keep the App.Api sign-in contract backward-compatible.
- Reuse or extend `tools/App.Maintenance` only after confirming it is the correct operator-local seam.
- A maintenance command, admin-only endpoint, or seed/bootstrap tool may be implemented only after code inspection proves the correct seam.
- Any desktop UX change must stay minimal and must not become full account management.
- Any Korobochka usage must follow manual deploy policy. Merge to `main` must not be treated as deployment.

Implementation guidance:
- Start by identifying existing entities and services for users, roles, group nodes, group admin assignments, password hashing, audit entries, and feature flags.
- Prefer idempotent account upsert behavior where safe.
- Validate required role/group-node existence before writing a user.
- Do not print or echo passwords.
- Do not accept passwords as command-line arguments.
- Avoid putting secrets in config examples, appsettings, docs, tests, PR bodies, reports, or logs.
- Use existing password hashing.
- Use existing normalized-login conventions.
- Ensure the resulting account can authenticate through the existing `/api/auth/sign-in`.
- If bootstrap needs a first-admin role/group assignment, use existing role/group ownership rules and tests.
- If audit cannot be written from the selected mechanism, document the reason and add the smallest safe audit hook if approved by scope.
- Keep error output operationally useful but secret-safe.

Out of scope:
- Public registration.
- Desktop user creation.
- Account management UI.
- Password reset.
- Invitations.
- Full RBAC redesign.
- Upload flow.
- GroupTree UI.
- PreUploadCheck UI.
- Site upload.
- UploadReceipt.
- Offline queue.
- App.Web changes.
- App.Mobile.Android changes.
- App.UI.Shared changes.
- Workflow/deploy changes.
- Public bootstrap endpoint without explicit approval.
- Secrets in docs, tests, PR text, logs, or reports.

Validation expectations:
- `git diff --name-only`
- `git diff --check`
- Targeted tests for the chosen bootstrap path.
- Build/test commands for changed projects.
- `dotnet format whitespace AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- Confirm `/api/auth/sign-in` remains backward-compatible.
- Confirm no open registration endpoint/UI was added.
- Confirm App.Desktop does not create users.
- Confirm no passwords/tokens/Authorization values are logged, printed, committed, or added to PR/report text.
- Confirm any migration is additive-first, if a migration was approved and required.
- Manual smoke:
  - provision the approved account through the bootstrap path;
  - configure/run App.Desktop against `http://192.168.1.66/`;
  - sign in successfully;
  - verify no password/token leakage.
- No deploy unless separately requested and manually triggered under the approved Korobochka policy.

Required final output:
- Exact files changed.
- Exact checks run and result of each check.
- Selected implementation seam and why it was chosen.
- Whether App.Api changed.
- Whether Modules.Auth changed.
- Whether tools/App.Maintenance changed.
- Whether App.Desktop changed.
- Whether contracts changed.
- Whether migrations changed.
- Whether feature flags changed.
- Whether audit/security events are written or why they are deferred.
- Confirmation that this is not public self-registration.
- Confirmation that App.Desktop does not create arbitrary users.
- Confirmation that no secrets/passwords/tokens were added to files, logs, PR body, or report.
- Manual smoke result or blocker.
- Rollback instructions.
- Next step.

Stop conditions:
- If the task appears to require public self-registration, stop.
- If App.Desktop would need to create users directly, stop.
- If a public bootstrap endpoint seems necessary but lacks explicit approval, stop.
- If the correct auth/domain ownership is unclear, stop and report the blocker.
- If password handling would require command-line password arguments, plaintext persistence, logging, or docs/PR disclosure, stop.
- If a non-additive migration appears necessary, stop.
- If the implementation would require broad RBAC/account-management redesign, stop.
