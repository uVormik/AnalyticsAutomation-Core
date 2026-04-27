# S2-51 Desktop SignIn Runtime Enablement Prompt

Use this prompt for a future runtime implementation chat/Codex session that enables the first narrow desktop SignIn runtime slice on top of the S2-50 auth/session boundaries.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Task:
Implement S2-51 Desktop SignIn Runtime Enablement.

Branch/scope:
- Work only under a dedicated S2-51 runtime branch.
- You may modify only `src/App.Desktop` and `tests/Unit/App.Desktop.Tests`.
- Do not change solution/project files unless the task is explicitly updated to approve that.
- Do not change `App.Api`, contracts, migrations, workflows, deploy scripts, server runtime, `App.Web`, `App.Mobile.Android`, `App.UI.Shared`, `App.Worker`, `BuildingBlocks.Contracts`, or `Modules`.

Sync-readiness first:
1. Verify the active repository root and current working directory.
2. Run `git branch --show-current`.
3. Run `git status --short --untracked-files=all`.
4. Run `git fetch origin --prune`.
5. Run `git switch main`.
6. Run `git pull --ff-only origin main`.
7. Read TEAM COORDINATION LOG issue #89 if available.
8. Review the latest relevant coordination log entries before coding.
9. Return to the dedicated S2-51 runtime branch only after sync-readiness is complete.

Required source review before coding:
- `00_START_HERE_README.txt`
- `01_MASTER_PLAN_AND_ARCHITECTURE.txt`
- `02_SHARED_RULES_AND_PROTOCOLS.txt`
- `04_GITHUB_WORKFLOW_AND_INFRA_RULES.txt`
- `docs/task-cards/S2-48_desktop-application-skeleton-task-card.txt`
- `docs/handoffs/S2_48_DESKTOP_APPLICATION_SKELETON_PROMPT.md`
- `docs/task-cards/S2-49A_desktop-skeleton-launch-fix.txt`
- `docs/task-cards/S2-50_desktop-auth-session-boundary-task-card.txt`
- `docs/handoffs/S2_50_DESKTOP_AUTH_SESSION_BOUNDARY_PROMPT.md`
- `docs/task-cards/S2-51_desktop-signin-runtime-enablement-task-card.txt`
- `docs/handoffs/S2_51_DESKTOP_SIGNIN_RUNTIME_ENABLEMENT_PROMPT.md`
- `src/App.Desktop/**`
- `tests/Unit/App.Desktop.Tests/**`

Must confirm before coding:
- PR #115 / S2-50 desktop auth session boundary runtime is merged into main.
- App.Desktop has `IDesktopAuthClient`, `HttpDesktopAuthClient`, `IDesktopSessionState`, `DesktopSessionState`, `IDesktopSessionStore`, and `DisabledDesktopSessionStore`.
- DesktopCompositionRoot still keeps SignIn safe by default when the control-plane API base address is not configured.
- Existing tests cover redaction, disabled persistence, session transitions, and composition registration.

Approved technology:
WPF standalone desktop shell + BlazorWebView / App.UI.Shared RCL reuse where useful.

Architecture boundaries to preserve:
- The primary desktop client must remain a standalone desktop application.
- App.Web / Browser / PWA are not the primary desktop client.
- Do not use App.Web as a desktop auth surface.
- App.Api remains the control plane.
- Video bytes must not go through App.Api.
- Keep API/contracts/migrations/workflows/deploy/server runtime unchanged.
- Preserve direct client <-> site media architecture for later upload tasks.

Goal:
Enable only a narrow, safe desktop SignIn slice. The slice should allow a desktop user to enter credentials, call the existing control-plane sign-in endpoint through the existing desktop auth client boundary, and update the existing in-memory desktop session state on success.

Required S2-51 runtime scope:
- Add a narrow App.Desktop auth options/config boundary for the App.Api base address.
- Keep credentials out-of-band and user-entered.
- Wire `HttpDesktopAuthClient` only when a valid API base address is configured.
- Preserve safe default behavior when API base address is missing or invalid.
- Add minimal SignIn UI/view-model/component wiring inside App.Desktop only.
- Use existing `IDesktopAuthClient` / `HttpDesktopAuthClient` only against the already existing backend sign-in endpoint.
- On successful SignIn, call the existing `IDesktopSessionState.SetSignedInAsync`.
- Keep `DisabledDesktopSessionStore`; do not add plaintext persistence or real Windows secure storage.
- Add focused tests for options/config behavior, composition-root auth wiring, successful session update, failure/unavailable states, and redaction.

Security requirements:
- No hardcoded credentials.
- No token, password, or Authorization logging.
- No raw exception message in UI-safe errors if it may contain secrets.
- No raw response body in UI-safe errors if it may contain secrets.
- No plaintext token persistence.
- No secrets in config examples.
- Credentials must remain user-entered and out-of-band.
- Default runtime behavior must stay safe if API base address is not configured.
- Passwords must not be kept in long-lived state after a sign-in attempt.
- UI/log/test diagnostics must use safe result codes/messages and redaction assertions.

S2-51 must not implement:
- Upload flow.
- GroupTree UI.
- PreUploadCheck UI.
- Site upload.
- UploadReceipt.
- Offline queue.
- Duplicate/fraud/admin review UI.
- App.Api changes.
- Shared contract changes.
- DB migrations.
- Deploy workflow/scripts.
- App.Web as desktop auth surface.
- App.Mobile.Android changes.
- App.Worker changes.
- App.UI.Shared changes unless a later approved task explicitly scopes shared UI.
- BuildingBlocks.Contracts changes.
- Modules changes.
- Real Windows secure storage.
- Plaintext local token storage.
- Full upload orchestration.

Implementation guidance:
- Prefer small, testable App.Desktop services behind interfaces.
- Keep the SignIn UI minimal and local to App.Desktop.
- Keep UI-safe errors based on `DesktopAuthResult` status/code/message rather than raw exceptions or response bodies.
- Treat missing/invalid API base address as a safe unavailable state.
- Do not invent backend auth endpoints or DTOs.
- Do not add secrets to appsettings, environment examples, test fixtures, or repo files.
- Keep composition-root changes narrow and covered by tests.
- Preserve S2-48/S2-50 placeholder upload boundaries and deferred upload behavior.

Validation expectations:
- `git diff --name-only`
- `git diff --check`
- `dotnet restore AnalyticsAutomation-Core.sln`
- `dotnet build src/App.Desktop/App.Desktop.csproj`
- `dotnet build AnalyticsAutomation-Core.sln`
- `dotnet test tests/Unit/App.Desktop.Tests/App.Desktop.Tests.csproj`
- `dotnet format whitespace AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- App.Desktop launch smoke if DesktopShell, composition root, or startup behavior changes.
- Confirm no App.Api/contracts/migrations/workflows/deploy/server runtime changes.
- Confirm no App.Web or App.Mobile.Android changes.
- Confirm no App.UI.Shared changes unless explicitly approved by a later task.
- Confirm no full upload flow.
- Confirm no GroupTree, PreUploadCheck, site upload, UploadReceipt, offline queue, duplicate/fraud/admin UI.
- Confirm no token logging and no plaintext token persistence.
- Confirm safe behavior when API base address is missing.
- No deploy.

Required final output:
- Exact files changed.
- Exact checks run and result of each check.
- Whether App.Desktop runtime files changed.
- Whether App.Desktop tests changed.
- Confirmation that solution/project files stayed unchanged unless explicitly approved.
- Confirmation that App.Api/contracts/migrations/workflows/deploy/server runtime stayed unchanged.
- Confirmation that App.Web and App.Mobile.Android stayed unchanged.
- Confirmation that full upload orchestration is still not implemented.
- Confirmation that no tokens are logged and no tokens are persisted in plaintext.
- Confirmation that missing API base address remains safe.
- Any blockers, especially missing existing backend auth endpoint compatibility or missing approved secure storage mechanism.
- Next step after the S2-51 runtime PR.

Stop conditions:
- If the required SignIn slice cannot be done without changing App.Api/contracts/migrations/workflows/deploy/server runtime, stop and report the blocker.
- If the existing backend sign-in endpoint/contract is missing or incompatible, do not invent one; stop and report the blocker.
- If secure Windows token storage appears necessary, stop; real secure storage is outside S2-51.
- If plaintext token persistence appears necessary, stop; that violates the security guardrails.
- If implementing upload, GroupTree, PreUploadCheck, UploadReceipt, offline queue, duplicate/fraud/admin UI, or full upload orchestration becomes necessary, stop; that is outside S2-51.
