# S2-10 Integration Account Provisioning Runbook

Status: Draft
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / Korobochka / Ops
Type: task card / runbook design
Goal: safely create or rotate a dedicated integration account for Coder 2 and Coder 3.
Decision source: S2-09 selected Option B.
Contracts: no shared DTO changes.
Migration: none expected for docs/runbook design.
Feature flag: none for docs step.
Offline behavior: no change.
Security: no secrets in repo/chat/logs.
Rollback: disable/reset account or revert runbook PR.
Production deploy: not included.

## Context

`main` already contains S2-09 practical auth provisioning decision.

S2-09 selected Option B:

- use a server-side maintenance path or operational runbook;
- create a dedicated integration account for Korobochka client integration;
- do not use `DevelopmentBootstrap` as shared Korobochka integration auth.

This S2-10 document is docs-only.
It does not change production code, shared contracts, DTOs, routes, migrations, packages, or deploy workflows.

## Goal and outcome

Provide a reviewed runbook design so Coder 1 / Platform Owner can safely provision or rotate a dedicated integration account for:

- Coder 2 Web LAN integration on Korobochka;
- Coder 3 Android LAN integration on Korobochka.

The account is for Web/Android LAN integration on Korobochka only.
It must use the normal sign-in flow.
It must not rely on hidden auth bypasses or development bootstrap shortcuts.

## Scope and guardrails

- docs-only task card and runbook design;
- no production code changes;
- no shared DTO changes;
- no route changes;
- no migration;
- no package additions;
- no production deploy;
- no secret values in repository content, chat, PR text, or logs.

## Account requirements

- purpose: shared Web/Android LAN integration on Korobochka;
- the password must be supplied out-of-band;
- the password must be rotatable;
- the password must not be printed to console output, logs, docs, or chat;
- access tokens and refresh tokens must not be printed to console output, logs, docs, or chat;
- verification must use existing auth endpoints and an authenticated API call only.

## Runbook design

### Inputs

- login name for the dedicated integration account;
- password provided out-of-band by Coder 1 / Platform Owner;
- Korobochka host access with the necessary admin or maintenance permissions.

No password is stored in Git.
No password is pasted into ChatGPT.
No private keys, runner token, access token, or refresh token are recorded in this document.

### Provisioning flow

1. Connect to Korobochka using the approved server-side operational path.
2. Create the dedicated integration account if it does not exist, or reset it if rotation is required.
3. Generate or apply the password hash server-side.
4. Ensure the raw password is entered interactively or read from a local-only secret source outside the repo.
5. Do not echo the password.
6. Do not echo generated tokens.
7. Share the login name and password with Coder 2 and Coder 3 out-of-band.

### Rotation flow

1. Choose a new password outside the repository.
2. Apply the new password through the same server-side maintenance path.
3. Do not print the new password.
4. Re-distribute the updated login name/password out-of-band if needed.
5. Re-run verification after rotation.

### Verification flow

Verification must confirm that the standard auth boundary remains intact.

1. Sign in through `POST /api/auth/sign-in` using the dedicated integration account.
2. Capture the returned bearer token only in the calling session or client memory.
3. Use the bearer token for authenticated `GET /api/group-tree/nodes`.
4. Expect authenticated `GET /api/group-tree/nodes` to return `200`.
5. Call anonymous `GET /api/group-tree/nodes` without authentication.
6. Expect anonymous `GET /api/group-tree/nodes` to remain `401`.

The verification procedure must avoid printing the password, `accessToken`, `refreshToken`, or equivalent secret values into terminal history, logs, screenshots, docs, or chat.

## Implementation options

### Option 1: Manual server-side psql or admin operation on Korobochka

Use a documented manual maintenance procedure on Korobochka.
The password hash is generated server-side and applied through a controlled admin or database operation.

Notes:

- best for immediate ops control with no new runtime surface;
- requires careful operator discipline so the raw password is not echoed or logged;
- suitable as the first concrete execution path when the runbook is followed by Platform Owner only.

### Option 2: Small maintenance command or script checked into repo

Add a narrow maintenance command or script in a later PR.
The password is supplied interactively or through a local-only secret file outside the repo.

Notes:

- reduces repeated manual steps and operator error;
- must not introduce hidden auth bypass;
- must not use `DevelopmentBootstrap`;
- must not accept secrets from committed config files;
- should preserve the same verification flow and log redaction discipline.

### Option 3: Admin UI later

An admin UI can be considered later, but not now.

Notes:

- out of scope for S2-10;
- would require separate design, review, and security approval;
- must still avoid secret exposure and bypass behavior.

## Preferred S2-10 implementation direction

Start with a repo-tracked runbook.
If repetition or operator friction justifies it, add a small maintenance command in a later PR.

Required direction:

- do not create a hidden auth bypass;
- do not use `DevelopmentBootstrap`;
- keep the normal sign-in flow as the only supported verification path;
- keep password handling out-of-band and rotatable.

## Acceptance criteria

- Coder 1 can create or reset the integration account without exposing the password;
- Coder 2 and Coder 3 receive the login name and password out-of-band;
- sign-in works on Korobochka;
- bearer call to `/api/group-tree/nodes` returns `200`;
- anonymous call to `/api/group-tree/nodes` remains `401`;
- no password or token appears in logs or docs.

## Rollback

Operational rollback options:

- disable the dedicated integration account;
- reset the integration account password again if distribution was incorrect;
- revert the runbook PR if the rollback target is documentation only.

No schema rollback is expected.
No production deploy rollback is included in this task.

## Out of scope

- changing shared contracts or DTOs;
- changing API routes;
- changing production authentication flow;
- adding migrations;
- adding packages;
- deploying to production;
- exposing passwords, tokens, private keys, or runner tokens.
