# S2 Platform AuthZ / CI Hardening Handoff

Status: Ready for team consumption
Owner: Coder 1 / Platform Owner
Scope: S2-01, S2-02, S2-03
Production deploy: Not included and not allowed

## Current main

Main is expected to be at:

- 5516e9d S2-03 CI Actions Node 24 Readiness

## Completed increments

### S2-01 Platform AuthZ Hardening

Delivered:

- documented S2-01 authz hardening task card;
- documented endpoint authz coverage map;
- added opaque bearer authentication pipeline skeleton;
- added App.Api host integration test skeleton;
- protected first business endpoint:
  - GET /api/group-tree/nodes now requires authorization;
- kept /health/live anonymous;
- added tests:
  - /health/live allows anonymous;
  - /api/group-tree/nodes rejects anonymous.

Contracts:

- no shared DTO changes;
- no route changes;
- no response payload shape changes;
- no database migration.

Client impact:

- clients calling GET /api/group-tree/nodes must send a valid bearer access token.
- health/live remains anonymous.

### S2-02 CI Integration Test Failure Propagation Hardening

Delivered:

- fixed false-green CI behavior in unit/integration test loops;
- CI now fails when any discovered test project fails;
- App.Api host integration tests are isolated from external PostgreSQL and auth bootstrap;
- opaque bearer handler no longer resolves PlatformDbContext for anonymous requests.

Contracts:

- no shared DTO changes;
- no API route changes;
- no migration.

CI impact:

- integration test discovery under tests/Integration is expected to run real projects;
- failed dotnet test now fails the CI job.

### S2-03 CI Actions Node 24 Readiness

Delivered:

- updated GitHub Actions checkout from actions/checkout@v4 to actions/checkout@v5;
- removed Node.js 20 action runtime warning source from checkout usage;
- kept CI behavior unchanged:
  - restore;
  - format;
  - build;
  - unit-tests;
  - integration-tests.

Contracts:

- no runtime code change;
- no API change;
- no migration.

## Current known deferred items

Still deferred from S1/S2 platform hardening:

- full endpoint-level authz coverage for all business endpoints;
- role/policy mapping beyond the first protected endpoint;
- real site provider beyond stub-compatible baseline;
- ffprobe/ffmpeg/Chromaprint deep media payload;
- worker-to-incident fan-out from deep result;
- final mobile offline cache policy;
- real stage host rehearsal with actual release folders;
- production deploy.

## Local repository note

A local Korobochka-related commit was removed from main and preserved in a local safety branch:

- safety/local-korobochka-main-072645f

Do not merge or push that work without a separate task card and explicit approval.
Deploy workflow changes are infra/release-sensitive and must go through PR/CI.

## Team guidance

Coder 2 / Web:

- continue using canonical repo uVormik/AnalyticsAutomation-Core;
- expect /api/group-tree/nodes to require bearer auth;
- health/live remains anonymous for smoke checks.

Coder 3 / Android:

- continue using canonical repo uVormik/AnalyticsAutomation-Core;
- mobile API compatibility remains preserved;
- any call to /api/group-tree/nodes must use the active session access token once auth integration is wired.

## Validation evidence

Recent completed PRs:

- PR #51: S2-01 Platform AuthZ Hardening
- PR #52: S2-02 CI Integration Test Failure Propagation Hardening
- PR #53: S2-03 CI Actions Node 24 Readiness

Manual workflow_dispatch CI on main after S2-03 completed successfully.

## Rollback

Each S2 increment can be rolled back by reverting the corresponding PR merge commit.
No database rollback is required for S2-01/S2-02/S2-03.
Production deploy is not part of this handoff.
