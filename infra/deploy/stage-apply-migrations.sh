#!/usr/bin/env bash
set -euo pipefail
ENV_FILE="${ENV_FILE:-/etc/analytics-automation/stage.api.env}"
SRC_ROOT="${SRC_ROOT:-$(pwd)}"
set -a
. "$ENV_FILE"
set +a
dotnet ef database update \
  --project "$SRC_ROOT/src/BuildingBlocks/Infrastructure/BuildingBlocks.Infrastructure.csproj" \
  --startup-project "$SRC_ROOT/src/App.Api/App.Api.csproj" \
  --context PlatformDbContext
