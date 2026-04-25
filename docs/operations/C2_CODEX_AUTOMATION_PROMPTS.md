# C2 Codex Automation Prompts

This document standardizes repeatable CODEX -> PowerShell operations for AnalyticsAutomation-Core.

Canonical local repo:
D:\nero project\ChatGpt\Repository\AnalyticsAutomation-Core

Canonical GitHub repo:
https://github.com/uVormik/AnalyticsAutomation-Core.git

## Global execution rules

Every executable prompt must be one single copy-paste block for CODEX -> PowerShell.

Required constraints:
- do not access C:
- do not use C:\Users, C:\Codex, $env:USERPROFILE as a real path, or $env:TEMP on C
- all temp/audit/cache paths stay under D:\nero project\ChatGpt\Repository
- work only in D:\nero project\ChatGpt\Repository\AnalyticsAutomation-Core
- never push to main
- never force push
- never change another owner zone without explicit scope
- never print password/accessToken/refreshToken values in output, logs, screenshots, GitHub, or ChatGPT

## Standard result blocks

Every Codex step must return exactly these two blocks:

=== C2_STEPXX_RESULT_BEGIN ===
Step:
Status:
RepoRoot:
CurrentBranch:
OriginUrl:
WorkingTreeCleanBefore:
WorkingTreeCleanAfter:
FirstFailingReason:
NextGate:
=== C2_STEPXX_RESULT_END ===

=== C2_STEPXX_DETAIL_BEGIN ===
GitStatusBefore:
GitStatusAfter:
ChangedFiles:
UnexpectedChangedFiles:
Logs:
=== C2_STEPXX_DETAIL_END ===

Common statuses:
READY
READY_PUSHED
READY_CI_GREEN_WEB_SCOPE_CLEAN
PENDING
CHECKS_FAILED
WEB_SCOPE_ANNOTATIONS_PRESENT
NOT READY

## Branch and task-card bootstrap

Use when starting a bounded task.

Required behavior:
- start from main
- fetch origin
- fast-forward main
- create feature branch
- create task card
- commit task card
- no production code changes
- no push

Required gates:
- origin URL matches canonical repo
- working tree clean before and after
- final branch is feature branch
- changed files are docs/task-cards only

## Implementation step

Use for bounded code changes.

Desktop client prompt replacement:
- For primary desktop-client upload control-plane work, use `docs/handoffs/S2_18_DESKTOP_CLIENT_UPLOAD_CONTROL_PLANE_PROMPT.md`.
- `docs/task-cards/C2-S2-18_web-upload-control-plane-integration.txt` and `docs/handoffs/C2_S2_18_WEB_UPLOAD_CONTROL_PLANE_HANDOFF.md` are stale for primary desktop-client implementation after PR #101.
- App.Web remains available only as a non-primary web/admin/diagnostic/support surface unless a new explicit architecture decision says otherwise.

Required behavior:
- verify current feature branch
- verify clean working tree
- write only allowed files
- run targeted format/build/test
- run git diff --check
- check changed files against allowed scope
- commit
- do not push unless the step explicitly says push

Scope guards:
- no src/App.Api unless task scope says so
- no src/Modules unless task scope says so
- no src/BuildingBlocks/Contracts without explicit contract approval
- no src/App.Mobile.Android unless mobile scope
- no .github/workflows unless infra scope
- no infra unless infra scope

## PR readiness

Use before push/open PR.

Required behavior:
- fetch origin
- compute merge-base with origin/main
- run targeted restore/build/test only for affected projects
- avoid solution-wide restore when Android MAUI workload is not required for the task
- run format verify for affected projects
- run git diff --check
- verify changed file scope
- verify no backend/contracts/App.Api/Android/deploy changes for web-only PRs
- prepare PR body in D-only audit directory

Recommended Web-only validation:
dotnet restore src/App.Web/App.Web.csproj
dotnet restore tests/Unit/App.Web.Tests/App.Web.Tests.csproj
dotnet format whitespace src/App.Web/App.Web.csproj --verify-no-changes --no-restore
dotnet format whitespace tests/Unit/App.Web.Tests/App.Web.Tests.csproj --verify-no-changes --no-restore
dotnet build src/App.Web/App.Web.csproj
dotnet build tests/Unit/App.Web.Tests/App.Web.Tests.csproj
dotnet test tests/Unit/App.Web.Tests/App.Web.Tests.csproj --no-build
git diff --check

## Push and PR body

Use after PR readiness passes.

Required behavior:
- verify branch
- verify clean working tree
- verify PR diff scope using merge-base
- push only feature branch
- no force push
- create PR body file in D:\nero project\ChatGpt\Repository\_migration-audit\...
- output manual PR URL

Manual PR URL pattern:
https://github.com/uVormik/AnalyticsAutomation-Core/compare/main...FEATURE_BRANCH_URL_ENCODED?expand=1

## CI and annotations recheck

Use after PR is opened or after every pushed fix.

Required behavior:
- verify local branch
- verify origin URL
- verify clean working tree
- fetch origin
- verify remote branch head
- verify PR head ref/base ref/head sha
- query GitHub check-runs for PR head
- query annotations
- classify blockers

Coder 2 Web blockers:
- failed checks
- pending checks
- annotations in src/App.Web
- annotations in tests/Unit/App.Web.Tests
- PR head mismatch while PR is open

Out-of-scope annotations:
- src/BuildingBlocks
- src/Modules
- src/App.Api
- src/App.UI.Shared/ExampleJsInterop.cs
- src/App.Mobile.Android

Out-of-scope annotations must be reported, not fixed in a web PR.

## Review-only validation

Use when reviewing another coder PR for a narrow ownership zone.

Required behavior:
- do not change files
- fetch PR head into remote tracking ref
- switch detached
- compute merge-base with origin/main
- inspect changed files in assigned scope only
- run targeted build if appropriate
- switch back to start branch
- final working tree clean
- output review recommendation

Review recommendations:
APPROVE
REQUEST_CHANGES
MANUAL_REVIEW_REQUIRED

## Merge verification and local main sync

Use after PR is merged.

Required behavior:
- verify PR API says merged = true
- fetch origin
- verify merge commit is ancestor of origin/main
- switch main
- fast-forward main
- verify local main equals origin/main
- verify required files are present
- run targeted build/test if task requires
- working tree clean

Do not:
- delete feature branches unless owner explicitly asks
- push to main
- force reset

## Live smoke gate

Live smoke must be a separate explicit step.

Rules:
- password is out-of-band
- do not print password
- do not print accessToken
- do not print refreshToken
- do not paste Authorization bearer values
- sanitized outputs only
- no screenshots containing secrets
- no SSH unless task explicitly allows it
- no server mutation without owner approval

Korobochka operational context:
- API root: http://192.168.1.66/
- health: /health/ready
- version: /api/system/version

Safe smoke output includes:
- HTTP status codes
- route names
- sanitized response summaries
- no secret values

## Standard reviewer-ready status message

Use after CI green and scope annotations clean:

STATUS=C2_PR_READY_FOR_REVIEW

PR:
<PR URL>

Branch:
<branch>

Latest head:
<short sha> <subject>

CI:
GREEN

Checks:
- ci / restore: success
- ci / build: success
- ci / format: success
- ci / unit-tests: success
- ci / integration-tests: success

Web scope annotations:
- src/App.Web: 0
- tests/Unit/App.Web.Tests: 0

Out-of-scope annotations:
<summary if any>

Scope:
<brief scope>

Not changed:
- no backend changes
- no App.Api changes
- no shared DTO / BuildingBlocks.Contracts changes
- no Android changes
- no deploy workflow changes
- no DB migration

Ready for code owner review / merge decision.
