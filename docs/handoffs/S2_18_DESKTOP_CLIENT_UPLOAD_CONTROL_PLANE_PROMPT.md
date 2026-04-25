# S2-18 Desktop Client Upload Control Plane Prompt

Use this prompt for a future implementation chat/Codex session that builds the primary desktop upload flow.

## Ready Prompt

You are working in `uVormik/AnalyticsAutomation-Core`.

Task:
Implement the primary desktop upload control-plane flow as a standalone launchable desktop application.

Important architecture boundary:
- The primary desktop client must be a standalone desktop application.
- App.Web is not the primary desktop client.
- App.Web may remain a non-primary web/admin/diagnostic/support surface.
- Browser/PWA/App.Web cannot be used as the primary desktop client without a new explicit architecture decision.
- S2-18-WEB is stale for primary desktop-client implementation.
- S2-18-ANDROID remains broadly aligned with the mobile baseline.

Before coding:
1. Verify the active repository root, current branch, and clean working tree.
2. Search the repo docs for an approved desktop UI technology decision/task card.
3. If no approved desktop UI technology decision exists, stop implementation work and create only a narrow desktop technology decision/task card. Do not implement UI code and do not select a concrete UI technology yourself.
4. If an approved desktop UI technology decision exists, follow it exactly and keep changes scoped to the approved desktop-client surface.

Required flow:
Implement or reuse this upload control-plane flow:

SignIn -> GroupTree -> file select -> SHA-256 -> businessObjectKey -> PreUploadCheck -> direct site upload boundary -> UploadReceipt

Flow requirements:
- Authenticate through the existing server control-plane auth surface.
- Load the group tree through the server control plane.
- Let the user select a local video file in the standalone desktop app.
- Compute SHA-256 client-side before upload.
- Produce or collect `businessObjectKey` before `PreUploadCheck`.
- Run `PreUploadCheck` before site upload when the server is online.
- Upload video bytes directly from the client to the site boundary.
- Submit `UploadReceipt` only after the direct site upload boundary succeeds or a task-approved stub reports success.

Hard boundaries:
- Video bytes must not go through App.Api.
- App.Api must remain the control plane, not a media byte proxy.
- Do not change public API routes, DTOs, shared contracts, migrations, workflows, deploy scripts, or server runtime unless a separate explicit task approves it.
- Do not use App.Web as the primary desktop-client implementation.
- Do not add offline behavior beyond limited/deferred placeholders unless a specific task card approves offline scope.

Site provider boundary:
- The real site provider may be stubbed behind a desktop-client adapter seam.
- A stub must make the direct-site boundary explicit.
- A stub must not send bytes through App.Api.
- Keep replacement by a real provider straightforward and local to the adapter boundary.

Security:
- Do not commit credentials.
- Do not print passwords, access tokens, refresh tokens, authorization headers, or secret-bearing response bodies.
- Redact token-like values in logs, UI diagnostics, tests, screenshots, and PR notes.

Validation expectations:
- Run targeted format/build/test commands for the affected desktop-client and shared projects.
- Run `git diff --check`.
- Verify changed files stay inside the approved desktop-client/docs/test scope.
- Confirm no API/contracts/migrations/workflows/deploy changes unless explicitly approved.
- Confirm App.Web remains non-primary and is not used as the desktop implementation surface.
- Include a sanitized manual smoke path for SignIn, GroupTree, file select, SHA-256, PreUploadCheck, direct-site boundary, and UploadReceipt.

PR metadata expectations:
- State the approved desktop UI technology decision/task card used.
- State whether the site provider is real or stubbed behind an adapter seam.
- State that video bytes do not pass through App.Api.
- State that server remains the control plane.
- State contracts/migrations/workflows/deploy impact.
- State App.Web/App.Mobile.Android impact.
- Include validation commands and results.
- Include any deferred offline or provider work as follow-up tasks.

Stop condition:
If the approved desktop UI technology decision is missing, do not implement desktop UI/runtime code. Create the narrow technology decision/task card and report that implementation is blocked until that decision is approved.
