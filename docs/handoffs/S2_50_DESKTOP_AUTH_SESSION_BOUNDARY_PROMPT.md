# S2-50 Desktop Auth/Session Boundary Prompt

Use this prompt for a future runtime implementation chat/Codex session that implements the first narrow desktop auth/session boundary slice.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Task:
Implement S2-50 Desktop Auth/Session Boundary.

Branch/scope:
- Work only under a dedicated S2-50 runtime branch.
- You may modify only `src/App.Desktop` and `tests/Unit/App.Desktop.Tests`.
- Do not change solution/project files unless the task is explicitly updated to approve that.
- Do not change `App.Api`, contracts, migrations, workflows, deploy scripts, server runtime, `App.Web`, `App.Mobile.Android`, `App.UI.Shared`, `App.Worker`, `BuildingBlocks.Contracts`, or `Modules`.

Sync-readiness first:
1. Verify the active repository root and current working directory.
2. Run `git branch --show-current`.
3. Run `git status --short --untracked-files=all`.
4. Run `git rev-parse origin/main`.
5. Follow the repo sync-readiness rules in `02_SHARED_RULES_AND_PROTOCOLS.txt`.
6. Check TEAM COORDINATION LOG issue #89 if available.
7. Review the latest relevant coordination log entries before coding.

Required source review before coding:
- `01_MASTER_PLAN_AND_ARCHITECTURE.txt`
- `02_SHARED_RULES_AND_PROTOCOLS.txt`
- `docs/task-cards/S2-48_desktop-application-skeleton-task-card.txt`
- `docs/handoffs/S2_48_DESKTOP_APPLICATION_SKELETON_PROMPT.md`
- `docs/task-cards/S2-49A_desktop-skeleton-launch-fix.txt`
- `docs/task-cards/S2-50_desktop-auth-session-boundary-task-card.txt`
- `docs/handoffs/S2_50_DESKTOP_AUTH_SESSION_BOUNDARY_PROMPT.md`
- `src/App.Desktop`
- `tests/Unit/App.Desktop.Tests`

Must review S2-48 and S2-49A:
- S2-48 created the WPF standalone App.Desktop skeleton, BlazorWebView/App.UI.Shared reuse boundary, placeholder service boundaries, and App.Desktop unit tests.
- S2-49A fixed App.Desktop launch by using the Windows-versioned TFM.
- Preserve those boundaries unless the S2-50 auth/session slice needs a narrow in-place refinement.

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
Implement only the first narrow desktop auth/session boundary slice. Prepare a secure foundation for later SignIn, GroupTree, PreUploadCheck, and UploadReceipt work.

Required S2-50 runtime scope:
- Define the desktop auth/session state model.
- Define the secure token/session storage abstraction.
- Add `IDesktopSessionState`.
- Add `IDesktopSessionStore` or an equivalent secure storage abstraction.
- Add `IDesktopAuthClient` boundary for an existing sign-in endpoint only if existing backend auth endpoint/contracts already support it without changes.
- Add a session status enum/value object if useful.
- Add an auth error/result model if needed.
- Add placeholder/in-memory or no-op implementation only if this runtime task explicitly approves it.
- Integrate only enough with the App.Desktop composition root to prove the boundaries compile.
- Add focused tests for session state transitions and storage abstraction behavior.

Security requirements:
- Do not log tokens.
- Do not log raw credentials.
- Do not hardcode passwords.
- Do not commit secrets.
- Do not persist tokens in plaintext.
- Do not store tokens in local config files, plaintext JSON, environment examples, or test fixtures.
- If a Windows secure storage mechanism is not approved yet, use an explicit placeholder plus TODO and keep persistence disabled.
- Keep auth/session error messages safe; do not include token values, passwords, or raw auth payloads.

S2-50 must not implement full upload flow:
- No full SignIn UI unless explicitly approved by an updated task.
- No upload UI.
- No group tree UI.
- No PreUploadCheck UI.
- No site upload.
- No UploadReceipt submission.
- No offline queue.
- No duplicate/fraud/admin review UI.
- No real refresh-token strategy unless existing contracts clearly support it.
- No App.Api changes.
- No shared contract changes.
- No DB migrations.
- No deploy.

Implementation guidance:
- Prefer small, testable auth/session services behind interfaces.
- Keep UI minimal or unchanged; no full sign-in screen unless explicitly approved.
- Preserve the existing S2-48 placeholder upload boundaries and deferred upload behavior.
- If existing auth endpoint/contracts are unclear, keep `IDesktopAuthClient` as a boundary only and report the blocker instead of inventing contracts.
- If secure Windows token storage is unclear, keep persistence disabled with an explicit placeholder and tests that prove it does not persist plaintext.
- Keep composition root changes minimal.

Validation expectations:
- `dotnet restore AnalyticsAutomation-Core.sln`
- `dotnet build src/App.Desktop/App.Desktop.csproj`
- `dotnet build AnalyticsAutomation-Core.sln`
- `dotnet test tests/Unit/App.Desktop.Tests/App.Desktop.Tests.csproj`
- `dotnet format whitespace AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- `git diff --check`
- App.Desktop launch smoke if the App.Desktop composition root changes.
- Confirm no App.Api/contracts/migrations/workflows/deploy/server runtime changes.
- Confirm no App.Web or App.Mobile.Android changes.
- Confirm no full upload flow.
- Confirm no token logging and no plaintext token persistence.
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
- Any blockers, especially missing approved secure storage mechanism or missing existing backend auth endpoint/contract.
- Next step after the S2-50 runtime PR.

Stop conditions:
- If the required auth/session slice cannot be done without changing App.Api/contracts/migrations/workflows/deploy/server runtime, stop and report the blocker.
- If a real sign-in endpoint/contract is not already available, do not invent one; keep the auth client as a boundary and report the blocker.
- If secure Windows token storage is not approved, do not implement plaintext persistence; keep persistence disabled.
- If implementing a full SignIn UI or upload flow becomes necessary, stop; that is outside S2-50.
