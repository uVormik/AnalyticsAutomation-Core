#!/usr/bin/env bash
set -euo pipefail
APP_ROOT="${APP_ROOT:-/opt/analytics-automation}"
API_SERVICE="${API_SERVICE:-analytics-api}"
WORKER_SERVICE="${WORKER_SERVICE:-analytics-worker}"
TARGET_RELEASE="${1:-}"
if [ -z "$TARGET_RELEASE" ]; then
  echo "Usage: stage-rollback.sh /opt/analytics-automation/releases/<release-id>"
  echo "Available releases:"
  ls -1 "$APP_ROOT/releases"
  exit 2
fi
test -d "$TARGET_RELEASE"
CURRENT_BEFORE="$(readlink -f "$APP_ROOT/current" || true)"
ln -sfn "$TARGET_RELEASE" "$APP_ROOT/current"
sudo systemctl restart "$API_SERVICE"
sudo systemctl restart "$WORKER_SERVICE"
echo "STAGE_ROLLBACK_FROM=$CURRENT_BEFORE"
echo "STAGE_ROLLBACK_TO=$TARGET_RELEASE"
echo "STAGE_ROLLBACK_DONE"
