# S2-47 Desktop UI Technology Owner Decision

This handoff records the owner-approved desktop UI technology decision. It is a docs-only owner decision record and does not create runtime code, projects, packages, solution entries, deploy changes, or workflow changes.

## Exact Owner Decision

Selected technology:

WPF standalone desktop shell + BlazorWebView / App.UI.Shared RCL reuse where useful.

The primary desktop client may proceed in a future task as a standalone WPF desktop application. App.Desktop-style naming is allowed, but actual project creation remains a later implementation task.

BlazorWebView is allowed only as an embedded native desktop UI composition mechanism for selected App.UI.Shared Razor components where useful.

App.Web / Browser / PWA remain non-primary and must not be used as the primary desktop client form.

## Approved Scope

- Record the owner decision in repo-side docs.
- Create the S2-47 task card.
- Create this S2-47 handoff/decision record.
- Keep changes inside docs/task-cards/ and docs/handoffs/.
- No deploy is required for S2-47.

## Non-Approved Options

- .NET MAUI Desktop as first option.
- Pure WPF-only as mandatory direction.
- Pure WinUI 3 as default.
- Avalonia.
- Uno Platform.
- Electron/Tauri as default.
- Browser/PWA/App.Web as primary desktop.

## Implementation Guardrails

- The primary executable must be a standalone WPF desktop application.
- App.Web is not the primary desktop client.
- Browser/PWA/App.Web must not become the primary desktop client through a wrapper shell.
- BlazorWebView may be used only for embedded native desktop UI composition of selected App.UI.Shared Razor components where useful.
- BlazorWebView must not be used as App.Web-in-a-shell.
- WPF shell responsibilities are windowing, native file picker, native desktop lifecycle, secure token/session storage boundary, hosting upload orchestration services, and hosting BlazorWebView where useful.
- Upload orchestration, hashing, precheck, direct-site adapter, and upload receipt should live in testable services where possible.
- Video bytes must not go through App.Api.
- Server remains the control plane.
- Runtime implementation must preserve:
  SignIn -> GroupTree -> file select -> SHA-256 -> businessObjectKey -> PreUploadCheck -> direct site upload boundary -> UploadReceipt
- Runtime implementation cannot change API/contracts/migrations/workflows/deploy/server runtime without separate explicit approval.
- WebView2/runtime prerequisite, packaging/update story, secure storage design, and test strategy must be explicitly handled in the implementation task.

## Next Allowed Task

S2-48 Desktop Application Skeleton Task Card.

S2-48 may create a runtime project only after reviewing the S2-47 decision.

S2-48 must not implement the full upload flow at once unless explicitly scoped.

Recommended first runtime slice:

Solution/project skeleton + dependency boundaries + no real upload behavior yet.

## Deferred Work

- Actual App.Desktop or equivalent project creation.
- Package/NuGet decisions.
- Solution/project file changes.
- WebView2 prerequisite handling.
- Packaging/update policy.
- Secure token/session storage implementation details.
- Test strategy for WPF shell, hosted services, adapter boundaries, and BlazorWebView composition.
- Real upload behavior and provider integration.

## Validation Expectations For S2-47

- git diff --name-only
- git diff --check
- dotnet format whitespace AnalyticsAutomation-Core.sln --verify-no-changes --no-restore
- Confirm no runtime/API/contracts/migrations/workflows/deploy changes.
- Confirm App.Web and App.Mobile.Android are unchanged.
- Confirm App.Desktop/project/solution files were not created.

## PR Notes

S2-47 does not require deploy.

S2-47 should be described as docs-only and as an owner-approved decision record.

Runtime desktop implementation remains a separate future task.
