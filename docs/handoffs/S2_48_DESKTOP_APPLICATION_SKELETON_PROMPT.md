# S2-48 Desktop Application Skeleton Prompt

Use this prompt for a future runtime implementation chat/Codex session that creates the first standalone desktop application skeleton.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Task:
Implement S2-48 Desktop Application Skeleton.

Branch/scope:
- Work only under a dedicated S2-48 runtime branch.
- You may create an `App.Desktop` WPF project only under that future S2-48 runtime branch.
- Keep changes scoped to the desktop skeleton, tests, and required solution/project wiring for that skeleton.

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
- `docs/task-cards/desktop-client-form-architecture-update.txt`
- `docs/task-cards/C2-S2-18_desktop-client-upload-control-plane-integration.txt`
- `docs/handoffs/S2_18_DESKTOP_CLIENT_UPLOAD_CONTROL_PLANE_PROMPT.md`
- `docs/task-cards/S2-46_desktop-ui-technology-decision.txt`
- `docs/handoffs/S2_46_DESKTOP_UI_TECHNOLOGY_DECISION_PROMPT.md`
- `docs/task-cards/S2-47_desktop-ui-technology-owner-decision.txt`
- `docs/handoffs/S2_47_DESKTOP_UI_TECHNOLOGY_OWNER_DECISION.md`
- `docs/task-cards/S2-48_desktop-application-skeleton-task-card.txt`
- `docs/handoffs/S2_48_DESKTOP_APPLICATION_SKELETON_PROMPT.md`

Must review S2-47 owner decision:
- Approved technology is WPF standalone desktop shell + BlazorWebView / App.UI.Shared RCL reuse where useful.
- BlazorWebView is allowed only as embedded native desktop UI composition for selected App.UI.Shared Razor components where useful.
- BlazorWebView must not be used as App.Web-in-a-shell.
- App.Web / Browser / PWA are not the primary desktop client.

Goal:
Create a minimal standalone desktop application skeleton and dependency boundaries only. Do not implement full upload behavior in S2-48.

Future S2-48 runtime implementation scope:
- Add `App.Desktop` WPF project.
- Add the required solution entry.
- Add minimal app startup/window.
- Add dependency boundaries/interfaces only.
- Add no full upload behavior.
- Add no backend API changes.
- Add no migrations.
- Add no deploy workflow changes.

Required skeleton boundaries:
- Desktop shell / window lifecycle.
- Desktop auth/session boundary.
- Secure token/session storage abstraction.
- File picker abstraction.
- Local file metadata / SHA-256 hashing service interface.
- Control-plane API client boundary.
- Direct site upload adapter boundary.
- Upload orchestration placeholder service boundary.
- BlazorWebView host boundary for selected App.UI.Shared components.

Architecture boundaries to preserve:
- The primary desktop client must be a standalone desktop application.
- Do not use App.Web as the primary desktop implementation.
- Do not use Browser/PWA/App.Web as the primary desktop client.
- Do not route video bytes through App.Api.
- Preserve direct client <-> site media architecture.
- App.Api remains the control plane.
- Keep API/contracts/migrations unchanged.
- Keep deploy workflows and deploy scripts unchanged.
- Keep server runtime unchanged unless a separate approved task explicitly changes it.

Upload flow to preserve for later tasks:
SignIn -> GroupTree -> file select -> SHA-256 -> businessObjectKey -> PreUploadCheck -> direct site upload boundary -> UploadReceipt

S2-48 must not implement full upload flow:
- No full SignIn UI.
- No full upload UI.
- No real site provider implementation.
- No offline queue implementation.
- No duplicate/fraud UI.
- No admin review UI.
- No App.Api changes.
- No shared contracts changes.
- No DB migrations.
- No deploy.

Suggested implementation shape:
- Keep WPF startup/window minimal.
- Put desktop runtime dependencies behind interfaces.
- Keep placeholder implementations explicit and easy to replace.
- Prefer testable services for hashing, control-plane client, direct-site adapter, and upload orchestration placeholder.
- If adding BlazorWebView, host only selected App.UI.Shared components through a bounded desktop host; do not load App.Web.
- If WebView2/package prerequisites are required, document them clearly in PR notes and keep package changes limited to the desktop project only.

Validation expectations:
- `dotnet restore AnalyticsAutomation-Core.sln`
- `dotnet build AnalyticsAutomation-Core.sln` or a targeted desktop project build if solution-wide build is not appropriate.
- `dotnet format whitespace AnalyticsAutomation-Core.sln --verify-no-changes --no-restore`
- `git diff --check`
- Unit tests for boundaries if test projects or testable boundary implementations are added.
- Confirm no App.Api, App.Worker, App.Web, App.Mobile.Android, App.UI.Shared runtime behavior changes unless explicitly approved.
- Confirm no BuildingBlocks.Contracts/shared DTO changes.
- Confirm no DB migrations.
- Confirm no workflow/deploy script changes.
- Confirm video bytes do not route through App.Api.
- No deploy.

Required final output:
- Exact files changed.
- Exact checks run and result of each check.
- Whether `App.Desktop` was created.
- Whether solution/project files changed and why.
- Confirmation that full upload orchestration is still not implemented.
- Confirmation that App.Web is not used as primary desktop.
- Confirmation that video bytes do not go through App.Api.
- Confirmation that API/contracts/migrations/workflows/deploy/server runtime remain unchanged.
- Any limitations or follow-up tasks, especially packaging/update policy, secure storage implementation details, WebView2 prerequisite, real site provider, and full upload orchestration.

PR metadata expectations:
- State that S2-47 approved WPF standalone desktop shell + BlazorWebView / App.UI.Shared RCL reuse where useful.
- State that S2-48 is skeleton-only.
- State that full upload orchestration is deferred.
- State whether package additions were limited to the desktop project.
- State contracts/migrations/workflows/deploy impact.
- State App.Web/App.Mobile.Android impact.
- Include validation commands and results.

Stop conditions:
- If S2-47 owner decision is missing or superseded, stop and ask for an updated decision.
- If the required skeleton cannot be created without changing API/contracts/migrations/workflows/deploy/server runtime, stop and report the blocker.
- If App.Web would be needed as the primary desktop surface, stop; that violates the approved architecture.
