# S2-31 Incident Routing Admin Assignment Provisioning

Status: Implemented
Owner: Coder 1 / Platform Owner
Module: App.Maintenance / GroupTree / Incidents / Korobochka / Ops
Type: maintenance tool + runbook
Goal: safely provision a dedicated root incident-routing admin that is distinct from `integration-web-android`.
Contracts: no shared DTO or public API route changes.
Migration: none.
Security: no passwords, password hashes, access tokens, or refresh tokens in repo, chat, docs, screenshots, or logs.
Production deploy: not included.

## Scope and guardrails

- use a repo-tracked maintenance command instead of ad-hoc production SQL as the primary path;
- create or update a dedicated login-capable routing admin for incident routing smoke and baseline ops;
- keep the account distinct from uploader `integration-web-android`;
- use the normal password hasher and normal auth model;
- do not use `DevelopmentBootstrap`;
- do not add hidden auth bypasses;
- do not print the password, password hash, `accessToken`, or `refreshToken`.

## Fixed provisioning target

The maintenance command provisions one dedicated account with these fixed values:

- login: `incident-routing-admin`;
- display name: `Incident Routing Admin`;
- role: `platform_owner`;
- current group node: `root`;
- root admin assignment: ensured in `app.group_admin_assignments`;
- active: `true`.

`platform_owner` remains the approved role because it already exists, is seeded, and matches the current maintenance/auth model.

## Required environment variable

The command requires exactly one secret input:

- `AA_INCIDENT_ROUTING_ADMIN_PASSWORD`

Set it only in the local PowerShell session on Korobochka.
Do not save it in repo files, `appsettings`, `.env`, terminal transcripts, screenshots, PR comments, or chat.

## Command usage

Open PowerShell in the repository root on Korobochka:

```powershell
Set-Location C:\Codex\AnalyticsAutomation-Core
```

Read the password without echoing it:

```powershell
$securePassword = Read-Host 'Incident routing admin password' -AsSecureString
$env:AA_INCIDENT_ROUTING_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $securePassword).Password
```

Run the maintenance command:

```powershell
dotnet run --project tools\App.Maintenance\App.Maintenance.csproj -- incident-routing-admin upsert
```

Expected safe output:

```text
login: incident-routing-admin
status: created
role: platform_owner
role-link: created
group-node: root
assignment: created
```

or on repeat execution:

```text
login: incident-routing-admin
status: updated
role: platform_owner
role-link: exists
group-node: root
assignment: exists
```

Clear the temporary environment variable after the run:

```powershell
Remove-Item Env:AA_INCIDENT_ROUTING_ADMIN_PASSWORD
```

## Verification

Required verification is additive and secret-safe:

1. Confirm the command completed with the safe output above and printed no password, password hash, `accessToken`, or `refreshToken`.
2. Re-run the existing readonly evidence path and confirm `app.group_admin_assignments` now contains one row for root + `incident-routing-admin`.
3. Re-run routing preview for the `root` node with uploader `integration-web-android` and confirm `resolvedAdminUserIds` is not empty.
4. Re-run the duplicate incident runtime smoke from S2-28 and confirm:
   - duplicate candidate is still created;
   - duplicate incident is now created;
   - duplicate incident assignment targets the dedicated routing admin;
   - worker logs remain sanitized.

The routing-preview and smoke verification must not print passwords or token payloads.
If sign-in is used to obtain temporary user context during verification, keep the response in process memory only and do not write the full response object to the terminal.

## No-secret rules

- never echo `$env:AA_INCIDENT_ROUTING_ADMIN_PASSWORD`;
- never print the raw `Invoke-RestMethod` sign-in response;
- never paste `accessToken` or `refreshToken` into chat, docs, or screenshots;
- never print password hashes from `app.auth_users`;
- never commit secret files or local transcripts.

## Idempotency and repeatability

The command is safe to re-run.
It will:

- create the user if missing, otherwise update the existing user;
- keep `incident-routing-admin` active and pinned to `root`;
- overwrite the password hash from `AA_INCIDENT_ROUTING_ADMIN_PASSWORD`;
- ensure the `platform_owner` role link exists exactly once;
- ensure the root assignment exists exactly once.

## Rollback

Preferred rollback is operational and additive-first:

1. if only password rotation is needed, re-run `incident-routing-admin upsert` with a new out-of-band password;
2. if the routing baseline must be revoked, disable the dedicated user and remove the root assignment through the approved operator procedure for Korobochka;
3. re-run readonly verification and confirm routing preview for uploader `integration-web-android` returns no resolved admins again if full rollback is intended.

This PR intentionally does not add a destructive maintenance command.
No migration rollback is required because the schema is unchanged.

## Out of scope

- public API route changes;
- shared DTO or contract changes;
- migrations;
- deploy workflow changes;
- `App.Web`, `App.Mobile.Android`, or `App.UI.Shared` changes;
- SSH or direct host automation from this repository.
