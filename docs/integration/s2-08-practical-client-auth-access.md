# S2-08 Practical Client Auth Access

Status: Draft
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / Korobochka / Client Integration
Type: integration readiness / operational handoff

## Goal

Define a safe practical way for Coder 2 Web and Coder 3 Android to obtain an access token on Korobochka for authenticated client integration.

This task follows S2-07 Client Auth API Handoff.

## Context

S2-07 documented the client auth flow:

1. call `POST /api/auth/sign-in`;
2. read `accessToken`;
3. send `Authorization: Bearer <accessToken>`;
4. call `GET /api/group-tree/nodes`;
5. expect `200 OK` when authenticated and `401 Unauthorized` when anonymous.

S2-06 added App.Api integration smoke coverage for this path.

The remaining practical gap is not the contract itself.
The practical gap is how Web and Android developers obtain a safe development account/token for shared Korobochka integration without putting secrets into Git, ChatGPT, logs, or PRs.

## What changes

- backend:
  - document the practical auth access gap;
  - select a safe implementation path for test/dev account provisioning.
- web:
  - no code change in this task.
- mobile:
  - no code change in this task.
- worker:
  - no behavior change.
- db:
  - no migration in this task card.
- audit:
  - no schema change in this task card.
- flags:
  - no new application feature flag in this docs step.
- events:
  - no new events.

## Contracts

No shared DTO changes.
No API route changes.
No response payload shape changes.
No enum/status changes.

## Migration

Not needed for this task card.

## Offline behavior

No offline behavior change.

## Security

Hard requirements:

- do not commit passwords;
- do not paste passwords into ChatGPT;
- do not commit runner tokens;
- do not commit private SSH keys;
- do not commit `/opt/v1-pyton/secrets`;
- do not log `accessToken` or `refreshToken`;
- do not store tokens in plain logs;
- do not create hidden bypass auth for clients.

## Candidate implementation options

### Option A: manual admin-created dev account

Create or reset a dedicated development account directly on Korobochka through an approved operational command or controlled admin path.

Pros:
- no public seed password;
- no test-only endpoint;
- safest for shared LAN integration.

Cons:
- requires a documented operational runbook;
- requires a controlled way to rotate/reset the password.

### Option B: temporary bootstrap command / runbook

Add or document a server-side maintenance command that can create a dev integration user using a password supplied outside Git.

Pros:
- repeatable;
- no password in repo;
- easier to rotate.

Cons:
- needs careful scope and audit;
- must not become a production backdoor.

### Option C: development bootstrap flag

Use existing development bootstrap only if it is explicitly safe for Korobochka integration and does not expose a known password in repo/docs.

Pros:
- may already exist.
Cons:
- risky if password is static or public;
- must not be enabled accidentally in production-like environments.

## Preferred direction

Preferred initial direction: Option A or B.

Do not publish shared credentials in repo or chat.
If a password is needed, it must be exchanged outside Git/ChatGPT and be rotatable.

## Observability

The selected follow-up implementation must allow Coder 1 to verify:

- health endpoint works;
- sign-in succeeds for the integration account;
- returned token can call `GET /api/group-tree/nodes`;
- anonymous request remains `401 Unauthorized`;
- no token values are printed.

## Rollback

Docs-only task card rollback: revert this PR.

Implementation rollback depends on selected follow-up:
- remove/reset dev account;
- rotate password;
- revert maintenance command if one is added.

## Definition of Done

- practical auth access gap is documented;
- selected implementation direction is approved;
- follow-up task is created for either:
  - secure dev integration account runbook;
  - or safe server-side maintenance command;
- Coder 2 and Coder 3 receive instructions without secrets;
- PR states:
  - module;
  - contracts: none;
  - migration: none;
  - feature flag: none;
  - rollback;
  - production deploy: not included.