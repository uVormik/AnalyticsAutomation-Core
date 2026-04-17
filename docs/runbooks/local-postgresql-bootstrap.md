# Local PostgreSQL bootstrap

## Purpose
Bring the local PostgreSQL foundation to a working state for AnalyticsAutomation-Core.

## Preconditions
- PostgreSQL installed and service running
- psql available in PATH
- dotnet-ef available as global tool
- local password files created by the platform owner bootstrap step

## Password file locations
- postgres superuser password:
  - %LOCALAPPDATA%\AnalyticsAutomation-Core\local-dev\postgres-superuser-password.txt
- app role password:
  - %LOCALAPPDATA%\AnalyticsAutomation-Core\local-dev\postgres-app-password.txt

## Apply migrations
Run from repository root:

dotnet ef database update `
  --project src/BuildingBlocks/Infrastructure/BuildingBlocks.Infrastructure.csproj `
  --startup-project src/App.Api/App.Api.csproj `
  --context PlatformDbContext

## Verify bootstrap marker
Use psql and query:

select code, created_at_utc
from app.database_bootstrap_markers
order by code;

## Verify migration history
Use psql and query:

select ""MigrationId""
from app.__ef_migrations_history
order by ""MigrationId"";

## Local default database identity
- database: analytics_automation_dev
- user: analytics_automation_app_dev

## Notes
- secrets are not stored in the repository
- local file based password resolution is a developer convenience only
- stage/prod must use protected runtime configuration