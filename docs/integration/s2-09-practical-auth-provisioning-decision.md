# S2-09 Practical Auth Provisioning Decision

Status: Approved direction for follow-up
Owner: Coder 1 / Platform Owner
Module: App.Api / Auth / Korobochka / Client Integration
Type: decision / task card
Goal: choose a safe practical auth provisioning path for Coder 2 and Coder 3 on Korobochka.
Decision: use Option B server-side maintenance command/runbook; do not use DevelopmentBootstrap.
Contracts: none
Migration: none
Feature flag: none for docs step
Offline behavior: no change
Security: no passwords/tokens in repo/chat/logs
Rollback: revert docs PR
Production deploy: not included

## Context

`main` is currently at `383125b` (`S2-08 Practical Client Auth Access`).

S2-08 confirmed that Coder 2 Web and Coder 3 Android need a practical and safe way to obtain an `accessToken` on Korobochka for shared client integration.

The audit outcome is that `DevelopmentAuthBootstrapService` is not suitable as shared Korobochka integration access because:

- it is development-only;
- a default or static bootstrap password must not become a shared integration credential;
- the password must not be published in the repo, ChatGPT, PRs, or logs.

## Scope and guardrails

- docs-only decision for practical auth provisioning;
- no production code changes;
- no DTO, contract, or route changes;
- no migration;
- no package changes;
- no secrets added to the repo;
- no change to offline behavior.

## Decision

Approved direction: Option B.

Use a server-side maintenance command or operational runbook to create or rotate a dedicated integration account for Korobochka.

The password must be supplied outside Git and outside ChatGPT, and it must stay rotatable.

`DevelopmentAuthBootstrapService` is explicitly not the approved shared integration path for Korobochka.

## Operational requirements

- create or reset a dedicated integration account only for shared client integration use;
- pass the password out-of-band;
- keep the credential out of repo content, PR text, ChatGPT messages, and logs;
- allow password rotation without contract or route changes;
- verify access through the existing auth flow only.

## Follow-up

S2-10 should implement or document the approved maintenance command/runbook.

It must:

1. create or reset a dedicated integration account on Korobochka;
2. accept a password supplied out-of-band;
3. support password rotation;
4. verify sign-in through `POST /api/auth/sign-in`;
5. verify `GET /api/group-tree/nodes` with `Authorization: Bearer <accessToken>`.

## Rollback

Rollback is docs-only: revert the docs PR.

## Notes

- Contracts: none.
- Migration: none.
- Feature flag: none for this docs step.
- Production deploy is not included.
