#!/usr/bin/env bash
set -euo pipefail
BASE_URL="${BASE_URL:-http://127.0.0.1:5000}"
API_SERVICE="${API_SERVICE:-analytics-api}"
WORKER_SERVICE="${WORKER_SERVICE:-analytics-worker}"
echo "SMOKE_CHECK=systemd_api"
systemctl is-active --quiet "$API_SERVICE"
echo "SMOKE_CHECK=systemd_worker"
systemctl is-active --quiet "$WORKER_SERVICE"
echo "SMOKE_CHECK=health_live"
curl -fsS "$BASE_URL/health/live" >/dev/null
echo "SMOKE_CHECK=health_ready"
curl -fsS "$BASE_URL/health/ready" >/dev/null
echo "SMOKE_CHECK=nginx_config"
sudo nginx -t >/dev/null
echo "SMOKE_LOGS=api_tail"
journalctl -u "$API_SERVICE" -n 40 --no-pager
echo "SMOKE_LOGS=worker_tail"
journalctl -u "$WORKER_SERVICE" -n 40 --no-pager
echo "STAGE_SMOKE_OK"
