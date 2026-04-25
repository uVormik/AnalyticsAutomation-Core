# S2-46 Desktop UI Technology Decision Prompt

Use this prompt for a future architecture decision / desktop client technology selection chat.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Role:
Architecture decision / desktop client technology selection.

Task:
Prepare a PR-ready desktop UI technology decision artifact or update the S2-46 task card with a clear recommendation and owner decision options.

Before deciding:
1. Verify repository root, current branch, and clean working tree.
2. Read the current master plan and architecture sources:
   - `00_START_HERE_README.txt`
   - `01_MASTER_PLAN_AND_ARCHITECTURE.txt`
   - `02_SHARED_RULES_AND_PROTOCOLS.txt`
   - `03_TASK_CARD_TEMPLATE.txt`
   - `docs/task-cards/desktop-client-form-architecture-update.txt`
   - `docs/task-cards/C2-S2-18_desktop-client-upload-control-plane-integration.txt`
   - `docs/handoffs/S2_18_DESKTOP_CLIENT_UPLOAD_CONTROL_PLANE_PROMPT.md`
   - `docs/task-cards/S2-46_desktop-ui-technology-decision.txt`
3. Check TEAM COORDINATION LOG issue #89 if available.

Hard constraints:
- Do not write runtime code.
- Do not add App.Desktop, WPF, WinUI, MAUI Desktop, Avalonia, Electron, Tauri, Uno, or any other desktop project.
- Do not change the solution file or project files.
- Do not change API routes, shared contracts, migrations, workflows, deploy scripts, server runtime, or existing App.Web WIP.
- Do not select Browser/PWA/App.Web as the primary desktop client.
- App.Web remains a non-primary web/admin/diagnostic/support surface.
- The primary desktop client must be a standalone launchable desktop application.
- Explicit owner approval is required before implementation can start.

Architecture boundaries to preserve:
- Server remains the control plane.
- Video upload/download bytes remain direct client <-> site.
- App.Api must not become a media byte proxy.
- Desktop implementation must support the S2-45 upload flow after approval:
  SignIn -> GroupTree -> file select -> SHA-256 -> businessObjectKey -> PreUploadCheck -> direct site upload boundary -> UploadReceipt

Candidate families to evaluate without preselecting:
- .NET MAUI Desktop
- WPF
- WinUI 3
- Avalonia
- Uno Platform
- Electron / Tauri-style shell, explicitly non-default unless owner approves
- Other justified option

Evaluate each viable candidate against:
- Alignment with C# / .NET 10.
- Standalone launchable app/executable.
- File picker and large video file handling.
- SHA-256 hashing of local video bytes.
- Direct client -> site upload boundary.
- Offline/local queue potential.
- Secure token/session storage.
- Shared UI reuse potential with App.UI.Shared, without forcing browser/PWA.
- Testability.
- Packaging/update story.
- Maintenance risk for a small team.
- Windows-first practicality, unless owner approves broader desktop scope.

Required output:
- Recommendation.
- Owner decision options.
- Chosen technology or explicitly deferred decision.
- Rationale.
- Consequences.
- Risks.
- Rollback/revisit criteria.
- Next implementation branch/task, only after approval.
- Validation plan for the decision artifact.

Approval rule:
If owner approval is not recorded, leave runtime implementation blocked. Do not proceed to implementation planning that assumes a concrete desktop UI technology.

PR metadata expectations:
- State that this is docs-only.
- State that no concrete desktop UI technology was implemented.
- State whether the decision is approved, recommended, or deferred.
- State that server remains the control plane.
- State that direct client <-> site media flow is preserved.
- State that App.Web remains non-primary.
- State that Browser/PWA is not the primary desktop client.
- State no API/contracts/migrations/workflows/deploy/runtime impact.
- Include validation commands and results.
