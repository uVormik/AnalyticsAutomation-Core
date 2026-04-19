#!/usr/bin/env bash
set -euo pipefail
APP_ROOT="${APP_ROOT:-/opt/analytics-automation}"
TARGET_RELEASE="${1:-}"
if [ -z "$TARGET_RELEASE" ]; then
  echo "Usage: stage-rollback.sh /opt/analytics-automation/releases/<release-id>"
  echo "Available releases:"
  ls -1 "$APP_ROOT/releases"
  exit 2
fi
test -d "$TARGET_RELEASE"
ln -sfn "$TARGET_RELEASE" "$APP_ROOT/current"
sudo systemctl restart analytics-api
sudo systemctl restart analytics-worker
printf "%s\n" "STAGE_ROLLBACK_DONE=$TARGET_RELEASE"
