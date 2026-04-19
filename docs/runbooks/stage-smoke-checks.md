# Stage smoke checks v1
Purpose: verify that a deployed stage release is alive before handoff.
Scope: App.Api, App.Worker, Nginx, health endpoints and basic logs.
Command:
BASE_URL=http://127.0.0.1:5000 bash infra/deploy/stage-smoke.sh
Checks:
1. analytics-api systemd service is active.
2. analytics-worker systemd service is active.
3. GET /health/live returns success.
4. GET /health/ready returns success.
5. nginx -t returns success.
6. journalctl tail for API and Worker is readable.
Failure triage:
- API inactive: check journalctl -u analytics-api -n 120 --no-pager.
- Worker inactive: check journalctl -u analytics-worker -n 120 --no-pager.
- ready failed: check PostgreSQL connection string and migrations.
- nginx failed: run sudo nginx -t and inspect site config.
Done marker: STAGE_SMOKE_OK.
