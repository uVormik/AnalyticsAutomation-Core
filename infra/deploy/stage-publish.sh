#!/usr/bin/env bash
set -euo pipefail
APP_ROOT="${APP_ROOT:-/opt/analytics-automation}"
SRC_ROOT="${SRC_ROOT:-$(pwd)}"
RELEASE_ID="${RELEASE_ID:-$(date -u +%Y%m%d%H%M%S)}"
RELEASE_DIR="$APP_ROOT/releases/$RELEASE_ID"
mkdir -p "$RELEASE_DIR/api" "$RELEASE_DIR/worker"
dotnet publish "$SRC_ROOT/src/App.Api/App.Api.csproj" -c Release -o "$RELEASE_DIR/api" --nologo
dotnet publish "$SRC_ROOT/src/App.Worker/App.Worker.csproj" -c Release -o "$RELEASE_DIR/worker" --nologo
ln -sfn "$RELEASE_DIR" "$APP_ROOT/current"
printf "%s\n" "STAGE_RELEASE_DIR=$RELEASE_DIR"
