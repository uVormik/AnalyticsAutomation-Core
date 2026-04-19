#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${BASE_URL:-http://127.0.0.1:5000}"
systemctl is-active --quiet analytics-api
systemctl is-active --quiet analytics-worker
curl -fsS "$BASE_URL/health/live" >/dev/null
curl -fsS "$BASE_URL/health/ready" >/dev/null
journalctl -u analytics-api -n 40 --no-pager
journalctl -u analytics-worker -n 40 --no-pager
printf "%s\n" "STAGE_SMOKE_OK"
