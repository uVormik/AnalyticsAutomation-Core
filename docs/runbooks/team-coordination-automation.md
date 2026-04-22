# Team coordination automation

## Purpose
This runbook documents the GitHub-centered coordination flow for AnalyticsAutomation-Core. GitHub is the source of truth for merged PR handoff context, while Telegram is notification-only when secrets are configured.

## Source of truth
- The fixed GitHub issue `TEAM COORDINATION LOG — AnalyticsAutomation-Core` is the canonical coordination log.
- Each merged PR appends one coordination comment to that issue.
- PR bodies must carry the metadata that the workflow extracts: contracts, migrations, feature flags, handoff docs, and next step.
- Telegram messages are optional mirrors and must always link back to GitHub.

## How each coder starts a new chat/task
1. PowerShell на ноутбуке:
   ```powershell
   git checkout main
   git pull --ff-only
   ```
2. Open GitHub Issue: `TEAM COORDINATION LOG — AnalyticsAutomation-Core`.
3. Read latest coordination comments.
4. Read referenced handoff docs.
5. Open only the relevant task card/runbook/handoff.
6. Do not rely on Telegram screenshots.
7. Start work in a short feature/docs/chore branch.

## What the merge workflow does
- It triggers on `pull_request` `closed`.
- It continues only when the PR is merged.
- It uses GitHub APIs only; it does not checkout PR code.
- It summarizes changed areas from the pull request files API.
- It posts a coordination comment into the fixed coordination issue.
- If the issue is closed, it reopens it and then comments.
- If the issue does not exist, it creates it with the canonical title and source-of-truth description.
- It marks CI/build status as `not evaluated by coordination workflow`.

## What the merge workflow does not do
- It does not run build, test, deploy, or `deploy-korobochka`.
- It does not use a self-hosted runner.
- It does not push commits, write generated logs into the repository, or modify `main`.
- It does not change runtime API contracts, migrations, or application behavior.

## PR author checklist
- Fill in the PR template completely, especially `Contracts`, `Migrations`, `Feature flags`, `Handoff docs`, and `Next step`.
- Be explicit when something is `not provided`, `no`, or `none`.
- For docs/workflow-only PRs, state that build/test execution is not applicable locally and why.
- Keep rollback instructions practical: revert the PR or disable the workflow when appropriate.

## Telegram behavior
- Telegram notification is optional and depends on `TELEGRAM_BOT_TOKEN` and `TELEGRAM_CHAT_ID` secrets.
- If secrets are absent, the workflow skips the Telegram step without failing.
- The Telegram message must stay short and must link back to the GitHub PR and the GitHub coordination issue.
- Telegram never replaces the GitHub coordination log.
